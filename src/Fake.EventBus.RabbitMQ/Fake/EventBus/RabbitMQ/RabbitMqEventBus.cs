using Fake.EventBus.Distributed;

namespace Fake.EventBus.RabbitMQ;

/// <summary>
/// 基于RabbitMessageQueue实现的事件总线
/// </summary> 
/// <remarks>
/// <para>路由模式，直连交换机，以事件名称作为routeKey</para>
/// <para>一个客户端独享一个消费者通道</para>
/// </remarks>
public class RabbitMqEventBus : IDistributedEventBus, IDynamicEventBus, IDisposable
{
    private readonly IRabbitMqChannelFactory _rabbitMqChannelFactory;
    private readonly ILogger<RabbitMqEventBus> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly ISubscriptionsManager _subscriptionsManager;
    private readonly RabbitMqEventBusOptions _eventBusOptions;

    private readonly string _brokerName; // 事件投递的交换机
    private readonly string _subscriptionQueueName; // 客户端订阅队列名称

    /// <summary>
    /// 消费者专用通道
    /// </summary>
    private IModel _consumerChannel;

    public RabbitMqEventBus(
        IRabbitMqChannelFactory rabbitMqChannelFactory,
        ILogger<RabbitMqEventBus> logger,
        IServiceScopeFactory serviceScopeFactory,
        ISubscriptionsManager subscriptionsManager,
        IOptions<RabbitMqEventBusOptions> eventBusOptions,
        IApplicationInfo applicationInfo
    )
    {
        _rabbitMqChannelFactory = rabbitMqChannelFactory;
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _subscriptionsManager = subscriptionsManager;
        _subscriptionsManager.OnEventRemoved += OnEventRemoved;

        _eventBusOptions = eventBusOptions.Value;
        _brokerName = _eventBusOptions.BrokerName;
        _subscriptionQueueName = applicationInfo.ApplicationName;
        _consumerChannel = CreateConsumerChannel();
    }


    public Task PublishAsync(EventBase @event)
    {
        var eventName = @event.GetType().Name;

        using var channel = _rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName);

        channel.ExchangeDeclare(exchange: _brokerName, ExchangeType.Direct);

        var body = JsonSerializer.SerializeToUtf8Bytes(@event, @event.GetType(), new JsonSerializerOptions
        {
            WriteIndented = true
        });

        var properties = channel.CreateBasicProperties();
        properties.DeliveryMode = 2; // Non-persistent (1) or persistent (2).

        _logger.LogDebug("发布事件到RabbitMQ: {Event}", @event.ToString());
        channel.BasicPublish(exchange: _brokerName, routingKey: eventName, mandatory: true,
            basicProperties: properties, body: body);

        return Task.CompletedTask;
    }

    public void Subscribe<TEvent, THandler>() where TEvent : EventBase
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventName<TEvent>();
        _logger.LogDebug("{EventHandler}订阅了事件{EventName}", typeof(THandler).GetName(), eventName);

        DoRabbitMqSubscription(eventName);
        _subscriptionsManager.AddSubscription<TEvent, THandler>();
        StartBasicConsume();
    }

    public void SubscribeDynamic<THandler>(string eventName) where THandler : IDynamicEventHandler
    {
        _logger.LogDebug("{EventHandler}订阅了动态事件{EventName}", typeof(THandler).GetName(), eventName);

        DoRabbitMqSubscription(eventName);
        _subscriptionsManager.AddDynamicSubscription<THandler>(eventName);
        StartBasicConsume();
    }

    public void Unsubscribe<TEvent, THandler>() where TEvent : EventBase
        where THandler : IEventHandler<TEvent>
    {
        var eventName = _subscriptionsManager.GetEventName<TEvent>();

        _logger.LogDebug("{EventHandler}取消了对事件{EventName}的订阅", typeof(THandler).GetName(), eventName);

        _subscriptionsManager.RemoveSubscription<TEvent, THandler>();

        DoRabbitMqUnSubscription(eventName);
    }

    public void UnsubscribeDynamic<THandler>(string eventName) where THandler : IDynamicEventHandler
    {
        _logger.LogDebug("{EventHandler}取消了对动态事件{EventName}的订阅", typeof(THandler).GetName(), eventName);

        _subscriptionsManager.RemoveDynamicSubscription<THandler>(eventName);

        DoRabbitMqUnSubscription(eventName);
    }

    public void Dispose()
    {
        _consumerChannel.Dispose();

        _subscriptionsManager.Clear();
    }

    #region private methods

    /// <summary>
    /// 去RabbitMQ订阅
    /// </summary>
    /// <param name="eventName"></param>
    private void DoRabbitMqSubscription(string eventName)
    {
        // 一个事件一个消费监听
        if (_subscriptionsManager.HasSubscriptions(eventName)) return;

        _rabbitMqChannelFactory.KeepAlive(_eventBusOptions.ConnectionName);

        _consumerChannel.QueueBind(queue: _subscriptionQueueName, exchange: _brokerName, routingKey: eventName);
    }

    /// <summary>
    /// 去RabbitMQ取消订阅
    /// </summary>
    /// <param name="eventName"></param>
    /// <exception cref="NotImplementedException"></exception>
    private void DoRabbitMqUnSubscription(string eventName)
    {
        _rabbitMqChannelFactory.KeepAlive(_eventBusOptions.ConnectionName);

        _consumerChannel.QueueUnbind(queue: _subscriptionQueueName, exchange: _brokerName, routingKey: eventName);
    }

    private void OnEventRemoved(object sender, string eventName)
    {
        using var channel = _rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName);
        // 解绑
        channel.QueueUnbind(queue: _subscriptionQueueName, exchange: _brokerName, routingKey: eventName);

        if (_subscriptionsManager.IsEmpty)
        {
            Dispose();
        }
    }

    private async Task OnReceived(object sender, BasicDeliverEventArgs eventArgs)
    {
        string eventName = eventArgs.RoutingKey;
        string message = Encoding.UTF8.GetString(eventArgs.Body.ToArray());
        try
        {
            await ProcessingEventAsync(eventName, message);

            _consumerChannel.BasicAck(eventArgs.DeliveryTag, multiple: false); // 手动确认
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "----- 处理消息时发生异常：{Message}", message);
            _logger.LogException(ex);

            _consumerChannel.BasicNack(eventArgs.DeliveryTag, multiple: false, requeue: true); // 重入队列
        }
    }

    /// <summary>
    /// 处理消息
    /// </summary>
    /// <param name="eventName"></param>
    /// <param name="message"></param>
    /// <returns></returns>
    private async Task ProcessingEventAsync(string eventName, string message)
    {
        // 空订阅
        if (!_subscriptionsManager.HasSubscriptions(eventName))
        {
            _logger.LogWarning("{EventName}没有任何订阅者", eventName);
            return;
        }

        using var scope = _serviceScopeFactory.CreateScope();
        var subscriptionInfos = _subscriptionsManager.GetSubscriptionInfos(eventName);
        // 广播
        foreach (var subscriptionInfo in subscriptionInfos)
        {
            _logger.LogDebug("正在处理RabbitMQ事件: {EventName}", eventName);

            // 处理动态集成事件
            if (subscriptionInfo.IsDynamic)
            {
                var handler = scope.ServiceProvider.GetRequiredService(subscriptionInfo.HandlerType)
                    .To<IDynamicEventHandler>();
                Debug.Assert(handler != null, nameof(handler) + " != null");
                await handler!.Handle(message);
            }
            else // 处理集成事件
            {
                var eventType = subscriptionInfo.EventType!;
                var handler = scope.ServiceProvider.GetRequiredService(subscriptionInfo.HandlerType);

                var handle = typeof(IEventHandler<>)
                    .MakeGenericType(eventType)
                    .GetMethod(nameof(IEventHandler<EventBase>.HandleAsync));

                var integrationEvent = JsonSerializer.Deserialize(message, eventType,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                // see：https://stackoverflow.com/questions/22645024/when-would-i-use-task-yield
                await Task.Yield();
                handle?.Invoke(handler, new[] { integrationEvent, CancellationToken.None });
            }
        }
    }

    /// <summary>
    /// 创建消费者通道
    /// </summary>
    /// <returns></returns>
    private IModel CreateConsumerChannel()
    {
        _logger.LogDebug("创建RabbitMQ消费者通道");

        _rabbitMqChannelFactory.KeepAlive(_eventBusOptions.ConnectionName);

        var arguments = new Dictionary<string, object>();

        /*
         * The message is negatively acknowledged by a consumer using basic.reject or basic.nack with requeue parameter set to false.
         * The message expires due to per-message TTL; or
         * The message is dropped because its queue exceeded a length limit
         */
        if (_eventBusOptions.EnableDlx)
        {
            string dlxExchangeName = "DLX." + _brokerName;
            string dlxQueueName = "DLX." + _subscriptionQueueName;
            string dlxRouteKey = dlxQueueName;

            _logger.LogDebug("创建RabbitMQ死信交换DLX");
            using (var deadLetterChannel = _rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName))
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

        var consumerChannel = _rabbitMqChannelFactory.CreateChannel(_eventBusOptions.ConnectionName);
        // 声明直连交换机
        consumerChannel.ExchangeDeclare(exchange: _brokerName, type: ExchangeType.Direct);
        // 声明队列
        consumerChannel.QueueDeclare(queue: _subscriptionQueueName, durable: true, exclusive: false,
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
            _logger.LogWarning(args.Exception, "消费者通道发生异常，正在重新创建");

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
        _logger.LogDebug("开启RabbitMQ消费通道的基础消费");

        if (_consumerChannel.IsClosed)
        {
            _logger.LogError("无法启动基础消费，RabbitMQ消费通道是关闭的");
            return;
        }

        // 创建异步消费者
        var consumer = new AsyncEventingBasicConsumer(_consumerChannel);
        consumer.Received += OnReceived;

        // 手动ack
        _consumerChannel.BasicConsume(queue: _subscriptionQueueName, autoAck: false, consumer: consumer);
    }

    #endregion
}