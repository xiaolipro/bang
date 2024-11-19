using Fake.EventBus.Distributed;
using Microsoft.Extensions.Hosting;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 基于RabbitMessageQueue实现的事件总线
/// </summary> 
/// <remarks>
/// <para>路由模式，直连交换机，以事件名称作为routeKey</para>
/// <para>一个客户端独享一个消费者通道</para>
/// </remarks>
public class RabbitMqEventBus(
    IRabbitMqChannelFactory rabbitMqChannelFactory,
    ILogger<RabbitMqEventBus> logger,
    IServiceScopeFactory serviceScopeFactory,
    IOptions<RabbitMqEventBusOptions> eventBusOptions,
    IApplicationInfo applicationInfo
) : IDistributedEventBus, IDisposable, IHostedService
{
    private readonly RabbitMqEventBusOptions _eventBusOptions = eventBusOptions.Value;
    private string ExchangeName => _eventBusOptions.ExchangeName; // 事件投递的交换机

    private string QueueName =>
        _eventBusOptions.QueueName ?? applicationInfo.ApplicationName.TrimEnd('.') + ".Queue"; // 客户端订阅队列名称

    /// <summary>
    /// 消费者专用通道
    /// </summary>
    private IModel _consumerChannel = null!;

    public Task PublishAsync(Event @event)
    {
        var eventName = @event.GetType().Name;

        using var channel = rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName);

        channel.ExchangeDeclare(exchange: ExchangeName, ExchangeType.Direct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).

        logger.LogDebug("发布事件到RabbitMQ: {Event}", @event.ToString());
        channel.BasicPublish(exchange: ExchangeName, routingKey: eventName, mandatory: true,
            basicProperties: properties, body: body);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _consumerChannel.Dispose();
    }

    #region private methods

    private async Task OnReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        string eventName = eventArgs.RoutingKey;

        string message = Encoding.UTF8.GetString(eventArgs.Body.Span);
        try
        {
            await ProcessingEventAsync(eventName, message);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "处理消息时发生异常：{Message}", message);

            // todo: exception handle?
        }

        // 即使发生异常，消息也总会被ack且不requeue，实际上，应该用死信队列来解决异常case
        // For more information see: https://www.rabbitmq.com/dlx.html
        _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false);
    }

    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task ProcessingEventAsync(string eventName, string message)
    {
        logger.LogTrace("Processing RabbitMQ event: {EventName}", eventName);

        await using var scope = serviceScopeFactory.CreateAsyncScope();

        if (!_eventBusOptions.EventTypes.TryGetValue(eventName, out var eventType))
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
            return;
        }

        // deserialize th event
        var obj = JsonSerializer.Deserialize(message, eventType, _eventBusOptions.JsonSerializerOptions);
        if (obj is not Event @event)
        {
            logger.LogWarning("Unable to resolve event type for event name {EventName}", eventName);
        }

        // 广播
        var handlers = scope.ServiceProvider.GetKeyedService<IEventHandler<>>();

        foreach (var handler in handlers)
        {
            // see：https://stackoverflow.com/questions/22645024/when-would-i-use-task-yield
            // await Task.Yield();
            // -> long task
            handler.HandleAsync(@event);
        }
    }

    /// <summary>
    /// 创建消费者通道
    /// </summary>
    /// <returns></returns>
    private IModel CreateConsumerChannel()
    {
        logger.LogDebug("创建RabbitMQ消费者通道");

        rabbitMqChannelFactory.KeepAlive(_eventBusOptions.ConnectionName);

        var arguments = new Dictionary<string, object>();

        /*
         * The message is negatively acknowledged by a consumer using basic.reject or basic.nack with requeue parameter set to false.
         * The message expires due to per-message TTL; or
         * The message is dropped because its queue exceeded a length limit
         */
        if (_eventBusOptions.EnableDlx)
        {
            string dlxExchangeName = "DLX." + ExchangeName;
            string dlxQueueName = "DLX." + QueueName;
            string dlxRouteKey = dlxQueueName;

            logger.LogDebug("创建RabbitMQ死信交换DLX");
            using (var deadLetterChannel = rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName))
            {
                // 声明死信交换机
                deadLetterChannel.ExchangeDeclare(exchange: dlxExchangeName, type: ExchangeType.Direct);
                // 声明死信队列
                deadLetterChannel.QueueDeclare(dlxQueueName, durable: true, exclusive: false, autoDelete: false);
                // 绑定死信交换机和死信队列
                deadLetterChannel.QueueBind(dlxQueueName, dlxExchangeName, dlxRouteKey);
            }

            arguments.Add("x-dead-letter-exchange", dlxExchangeName); // 设置DLX
            arguments.Add("x-dead-letter-routing-key", dlxRouteKey); // DLX会根据该值去找到死信消息存放的队列

            if (_eventBusOptions.MessageTtl > 0)
            {
                arguments.Add("x-message-ttl", _eventBusOptions.MessageTtl); // 设置消息的存活时间，实现延迟队列
            }

            if (_eventBusOptions.QueueMaxLength > 0)
            {
                arguments.Add("x-max-length", _eventBusOptions.QueueMaxLength); // 设置队列最大长度，实现队列容量控制
            }
        }

        var consumerChannel = rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName);
        // 声明直连交换机
        consumerChannel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Direct);
        // 声明队列
        consumerChannel.QueueDeclare(queue: QueueName, durable: true, exclusive: false,
            autoDelete: false, arguments: arguments);

        /*
         * 消费者限流机制，防止开启客户端时，服务被巨量数据冲宕机
         * 限流情况不能设置自动签收，一定要手动签收
         * prefetchSize，消息体大小，如果设置为0，表示对消息本身的大小不限制
         * prefetchCount，告诉RabbitMQ不要一次性给消费者推送大于N条消息
         * global，是否将设置应用于整个通道，false表示只应用于当前消费者
         */
        _consumerChannel.BasicQos(_eventBusOptions.PrefetchSize, _eventBusOptions.PrefetchCount, false);

        // 当通道调用的回调中发生异常时发出信号
        consumerChannel.CallbackException += (_, args) =>
        {
            logger.LogWarning(args.Exception, "消费者通道发生异常，正在重新创建");

            // 销毁原有通道，重新创建
            _consumerChannel.Dispose();
            _consumerChannel = CreateConsumerChannel();
            // 使得新的消费者通道依然能够正常的消费消息
            StartBasicConsume();
        };

        return consumerChannel;
    }

    /// <summary>
    /// 启动基本内容类消费
    /// </summary>
    private void StartBasicConsume()
    {
        logger.LogDebug("开启RabbitMQ消费通道的基础消费");

        if (_consumerChannel.IsClosed)
        {
            logger.LogError("无法启动基础消费，RabbitMQ消费通道是关闭的");
            return;
        }

        // 创建异步消费者
        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += OnReceived;

        // 手动ack
        _consumerChannel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
    }

    #endregion

    public Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            logger.LogInformation("Starting RabbitMqEventBus on a background thread");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}