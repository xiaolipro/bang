using System;
using System.Collections.Generic;

namespace Fake.EventBus
{
    /// <summary>
    /// 订阅管理器
    /// </summary>
    public interface ISubscriptionsManager
    {
        /// <summary>
        /// 没有订阅了
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 事件移除时
        /// </summary>
        event System.EventHandler<string>? OnEventRemoved;


        #region 添加订阅

        /// <summary>
        /// 添加动态订阅
        /// </summary>
        /// <typeparam name="THandler">处理者</typeparam>
        /// <param name="eventName">事件名称</param>
        void AddDynamicSubscription<THandler>(string eventName)
            where THandler : IDynamicEventHandler;

        /// <summary>
        /// 添加订阅
        /// </summary>
        /// <typeparam name="TEvent">事件</typeparam>
        /// <typeparam name="THandler">处理者</typeparam>
        void AddSubscription<TEvent, THandler>()
            where TEvent : Event
            where THandler : EventHandler<TEvent>;

        #endregion


        #region 移除订阅

        /// <summary>
        /// 移除动态订阅
        /// </summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="eventName"></param>
        void RemoveDynamicSubscription<THandler>(string eventName)
            where THandler : IDynamicEventHandler;

        /// <summary>
        /// 移除订阅
        /// </summary>
        /// <typeparam name="TEvent"></typeparam>
        /// <typeparam name="THandler"></typeparam>
        void RemoveSubscription<TEvent, THandler>()
            where TEvent : Event
            where THandler : EventHandler<TEvent>;

        /// <summary>
        /// 清除所有订阅
        /// </summary>
        void Clear();

        #endregion


        #region 查询订阅信息

        /// <summary>
        /// 获取订阅信息集合
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns></returns>
        IEnumerable<SubscriptionInfo> GetSubscriptionInfos(string eventName);

        /// <summary>
        /// 获取订阅信息集合
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        IEnumerable<SubscriptionInfo?> GetSubscriptionInfos<TEvent>() where TEvent : Event;


        /// <summary>
        /// 此事件是否有订阅
        /// </summary>
        /// <typeparam name="TEvent">事件类型</typeparam>
        /// <returns></returns>
        bool HasSubscriptions<TEvent>() where TEvent : Event;

        /// <summary>
        /// 此事件是否有订阅
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <returns></returns>
        bool HasSubscriptions(string eventName);


        string GetEventName<TEvent>() where TEvent : Event;

        Type? GetEventTypeByName(string eventName);

        #endregion
    }
}