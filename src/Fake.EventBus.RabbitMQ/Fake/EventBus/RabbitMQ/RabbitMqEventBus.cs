using System.Net.Sockets;
using Fake.EventBus.Distributed;
using Microsoft.Extensions.Hosting;
using OpenTelemetry;
using OpenTelemetry.Context.Propagation;
using Polly;
using Polly.Retry;
using RabbitMQ.Client.Exceptions;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 基于RabbitMessageQueue实现的事件总线
/// </summary> 
/// <remarks>
/// <para>路由模式，直连交换机，以事件名称作为routeKey</para>
/// <para>一个客户端独享一个消费者通道</para>
/// </remarks>
public class RabbitMqEventBus(
    IRabbitMqConnectionPool rabbitMqConnectionPool,
    ILogger<RabbitMqEventBus> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<EventBusSubscriptionOptions> subscriptionOptions,
    IOptions<RabbitMqEventBusOptions> eventBusOptions,
    IApplicationInfo applicationInfo
) : IDistributedEventBus, IDisposable, IHostedService
{
    private readonly RabbitMqEventBusOptions _eventBusOptions = eventBusOptions.Value;
    private readonly EventBusSubscriptionOptions _subscriptionOptions = subscriptionOptions.Value;

    private readonly ResiliencePipeline _pipeline = CreateResiliencePipeline(eventBusOptions.Value.RetryCount);
    private readonly ActivitySource _activitySource = new("Fake.EventBus.RabbitMQ");
    private readonly TextMapPropagator _propagator = Propagators.DefaultTextMapPropagator;

    private readonly IConnection _connection = rabbitMqConnectionPool.Get(eventBusOptions.Value.ConnectionName);
    private IModel? _consumerChannel; // 消费者专用通道

    private readonly string _exchangeName = eventBusOptions.Value.ExchangeName; // 事件投递的交换机

    private readonly string _queueName = (eventBusOptions.Value.QueueName ?? applicationInfo.ApplicationName)
        .TrimEnd('.') + ".Queue"; // 客户端订阅队列名称

    public Task PublishAsync(IntegrationEvent @event)
    {
        var routingKey = @event.GetType().Name;

        logger.LogDebug("Creating RabbitMQ channel for publishing event: {EventId} ({EventName})", @event.Id,
            routingKey);

        using var channel = _connection.CreateModel();

        var body = SerializeMessage(@event);

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).

        _pipeline.Execute(chan =>
            {
                // Start an activity with a name following the semantic convention of the OpenTelemetry messaging specification.
                // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
                using var activity = _activitySource.StartActivity($"{routingKey} publish", ActivityKind.Client);

                // Depending on Sampling (and whether a listener is registered or not), the activity above may not be created.
                // If it is created, then propagate its context. If it is not created, the propagate the Current context, if any.
                ActivityContext contextToInject = default;
                if (activity != null)
                {
                    contextToInject = activity.Context;
                }
                else if (Activity.Current != null)
                {
                    contextToInject = Activity.Current.Context;
                }
                
                _propagator.Inject(new PropagationContext(contextToInject, Baggage.Current), properties,
                    static (props, key, value) =>
                        {
                            props.Headers ??= new Dictionary<string, object>();
                            props.Headers[key] = value;
                        });
        
                SetActivityContext(activity, routingKey, "publish");

                try
                {
                    logger.LogDebug("Publishing event to RabbitMQ: {EventId} ({EventName})", @event.Id, routingKey);
                    chan.BasicPublish(exchange: _exchangeName, routingKey: routingKey, mandatory: true,
                        basicProperties: properties, body: body);
                }
                catch (Exception ex)
                {
                    activity.SetExceptionTags(ex);
                    throw;
                }
            }, channel);

        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _ = Task.Factory.StartNew(() =>
            {
                _consumerChannel = CreateConsumerChannel();
                foreach (var (eventName, _) in _subscriptionOptions.EventTypes)
                {
                    _consumerChannel.QueueBind(
                        queue: _queueName,
                        exchange: _exchangeName,
                        routingKey: eventName);
                }

                StartBasicConsume(_consumerChannel);
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Default);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    protected virtual IntegrationEvent DeserializeMessage(string message, Type eventType)
    {
        var res = JsonSerializer.Deserialize(message, eventType, _subscriptionOptions.JsonSerializerOptions) as
            IntegrationEvent;

        return res ?? throw new InvalidOperationException("Failed to deserialize message");
    }

    protected virtual byte[] SerializeMessage(IntegrationEvent @event)
    {
        return JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(),
            _subscriptionOptions.JsonSerializerOptions);
    }

    public void Dispose()
    {
        _consumerChannel?.Dispose();
    }


    #region private methods

    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task ProcessingEventAsync(string eventName, string message)
    {
        logger.LogDebug("Processing RabbitMQ event: {EventName}", eventName);

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        if (!_subscriptionOptions.EventTypes.TryGetValue(eventName, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        // deserialize th event
        var @event = DeserializeMessage(message, eventType);

        // 广播
        foreach (var handler in scope.ServiceProvider.GetKeyedServices<IEventHandler>(eventType))
        {
            await handler.HandleAsync(@event);
        }
    }

    /// <summary>
    /// 创建消费者通道
    /// </summary>
    /// <returns></returns>
    private IModel CreateConsumerChannel()
    {
        logger.LogDebug("Creating rabbitmq eventbus consumer channel");

        // tips: can't use using, because the channel will be disposed in the callback exception
        var channel = rabbitMqConnectionPool.Get(_eventBusOptions.ConnectionName).CreateModel();

        var arguments = new Dictionary<string, object>();

        /*
         * The message is negatively acknowledged by a consumer using basic.reject or basic.nack with requeue parameter set to false.
         * The message expires due to per-message TTL; or
         * The message is dropped because its queue exceeded a length limit
         */
        if (_eventBusOptions.EnableDlx)
        {
            string dlxExchangeName = "DLX." + _exchangeName;
            string dlxQueueName = "DLX." + _queueName;
            string dlxRouteKey = dlxQueueName;

            logger.LogDebug("Binding DLX exchange: {DlxExchangeName}, queue: {DlxQueueName}, routeKey: {DlxRouteKey}",
                dlxExchangeName, dlxQueueName, dlxRouteKey);

            channel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct);
            channel.QueueDeclare(dlxQueueName, durable: true, exclusive: false, autoDelete: false);
            channel.QueueBind(dlxQueueName, dlxExchangeName, dlxRouteKey);

            arguments.Add("x-dead-letter-exchange", dlxExchangeName);
            arguments.Add("x-dead-letter-routing-key", dlxRouteKey);

            if (_eventBusOptions.MessageTtl > 0)
            {
                arguments.Add("x-message-ttl", _eventBusOptions.MessageTtl);
            }

            if (_eventBusOptions.QueueMaxLength > 0)
            {
                arguments.Add("x-max-length", _eventBusOptions.QueueMaxLength);
            }
        }

        channel.ExchangeDeclare(exchange: _exchangeName, type: ExchangeType.Direct);
        channel.QueueDeclare(queue: _queueName, durable: true, exclusive: false, autoDelete: false,
            arguments: arguments);

        /*
         * 消费者限流机制，防止开启客户端时，服务被巨量数据冲宕机
         * 限流情况不能设置自动签收，一定要手动签收
         * prefetchSize，消息体大小，如果设置为0，表示对消息本身的大小不限制
         * prefetchCount，告诉RabbitMQ不要一次性给消费者推送大于N条消息
         * global，是否将设置应用于整个通道，false表示只应用于当前消费者
         */
        channel.BasicQos(_eventBusOptions.PrefetchSize, _eventBusOptions.PrefetchCount, false);

        // 当通道调用的回调中发生异常时发出信号
        channel.CallbackException += (_, args) =>
            {
                logger.LogWarning(args.Exception, "Consumer channel callback exception");

                // 销毁原有通道，重新创建
                channel.Dispose();
                channel = CreateConsumerChannel();
                // 使得新的消费者通道依然能够正常的消费消息
                StartBasicConsume(channel);
            };

        return channel;
    }

    /// <summary>
    /// 启动基本内容类消费
    /// </summary>
    /// <param name="channel"></param>
    private void StartBasicConsume(IModel channel)
    {
        logger.LogDebug("Starting rabbitmq eventbus basic consume");

        // 创建异步消费者
        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, eventArgs) =>
            {
                string eventName = eventArgs.RoutingKey;

                string message = Encoding.UTF8.GetString(eventArgs.Body.Span);
                try
                {
                    await ProcessingEventAsync(eventName, message);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Processing event: {EventName} failed, message:\n {Message}", eventName,
                        message);

                    // 即使发生异常，消息也总会被ack且不requeue，实际上，应该用死信队列来解决异常case
                    // For more information see: https://www.rabbitmq.com/dlx.html
                    channel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: false);
                }

                channel.BasicAck(eventArgs.DeliveryTag, multiple: false);
            };

        channel.BasicConsume(queue: _queueName, autoAck: false, consumer: consumer);
    }

    static ResiliencePipeline CreateResiliencePipeline(int retryCount)
    {
        // See https://www.pollydocs.org/strategies/retry.html
        var retryOptions = new RetryStrategyOptions
        {
            ShouldHandle = new PredicateBuilder().Handle<BrokerUnreachableException>().Handle<SocketException>(),
            MaxRetryAttempts = retryCount,
            DelayGenerator = context => ValueTask.FromResult(GenerateDelay(context.AttemptNumber))
        };

        return new ResiliencePipelineBuilder()
            .AddRetry(retryOptions)
            .Build();

        static TimeSpan? GenerateDelay(int attempt)
        {
            return TimeSpan.FromSeconds(Math.Pow(2, attempt));
        }
    }
    
    private static void SetActivityContext(Activity? activity, string routingKey, string operation)
    {
        if (activity is not null)
        {
            // These tags are added demonstrating the semantic conventions of the OpenTelemetry messaging specification
            // https://github.com/open-telemetry/semantic-conventions/blob/main/docs/messaging/messaging-spans.md
            activity.SetTag("messaging.system", "rabbitmq");
            activity.SetTag("messaging.destination_kind", "queue");
            activity.SetTag("messaging.operation", operation);
            activity.SetTag("messaging.destination.name", routingKey);
            activity.SetTag("messaging.rabbitmq.routing_key", routingKey);
        }
    }

    #endregion
}