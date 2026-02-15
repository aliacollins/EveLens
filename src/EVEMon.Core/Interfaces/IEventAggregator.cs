using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Publish/subscribe event aggregator for decoupled communication.
    /// Replaces direct subscription to EveMonClient's 74 static events.
    /// </summary>
    /// <remarks>
    /// Uses strong references by default to match the existing event behavior.
    /// Opt-in weak references available via <see cref="SubscribeWeak{TEvent}"/>.
    /// </remarks>
    public interface IEventAggregator
    {
        /// <summary>
        /// Subscribe to an event type with a strong reference.
        /// Dispose the returned token to unsubscribe, or call <see cref="Unsubscribe{TEvent}"/> directly.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A disposable token that unsubscribes when disposed.</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Subscribe to an event type with a weak reference.
        /// The subscription is automatically cleaned up when the subscriber is GC'd.
        /// Dispose the returned token to unsubscribe early.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A disposable token that unsubscribes when disposed.</returns>
        IDisposable SubscribeWeak<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Unsubscribe from an event type.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="handler">The handler to remove.</param>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Publish an event to all subscribers.
        /// </summary>
        /// <typeparam name="TEvent">The event type.</typeparam>
        /// <param name="eventData">The event data.</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
    }
}
