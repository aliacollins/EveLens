// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Publishes and subscribes to typed events across the application.
    /// Replaces the old 74 static events on <c>EveLensClient</c> with a decoupled pub/sub pattern
    /// (Strangler Fig migration).
    /// </summary>
    /// <remarks>
    /// Thread-safe: uses <c>ConcurrentDictionary</c> for event-type buckets and per-type <c>lock</c>
    /// on the subscription list. Dead weak references are cleaned lazily during <see cref="Publish{TEvent}"/>.
    ///
    /// Strong references are the default to match old static event semantics (subscribers stay alive).
    /// Use <see cref="SubscribeWeak{TEvent}"/> for UI components that should auto-unsubscribe on GC.
    ///
    /// Handler exceptions are caught and written to <c>Debug.WriteLine</c>; a failing handler
    /// does not prevent subsequent handlers from executing.
    ///
    /// Production: <c>EventAggregator</c> in <c>EveLens.Infrastructure/Services/EventAggregator.cs</c>
    /// (also aliased from <c>EveLens.Common/Services/</c> via <c>AppServices.EventAggregator</c>).
    /// Testing: <c>new EventAggregator()</c> directly -- no mocking needed; it is a simple in-memory
    /// implementation with no external dependencies.
    /// </remarks>
    public interface IEventAggregator
    {
        /// <summary>
        /// Subscribes to an event type with a strong reference, keeping the subscriber alive
        /// as long as the subscription exists.
        /// Dispose the returned token to unsubscribe, or call <see cref="Unsubscribe{TEvent}"/> directly.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to (must be a reference type).</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A disposable token that removes the subscription when disposed.</returns>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Subscribes to an event type with a weak reference, allowing the subscriber to be
        /// garbage-collected without explicit unsubscription.
        /// Dead weak references are cleaned during <see cref="Publish{TEvent}"/>.
        /// Dispose the returned token to unsubscribe early if needed.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to (must be a reference type).</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        /// <returns>A disposable token that removes the subscription when disposed.</returns>
        IDisposable SubscribeWeak<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Explicitly removes a handler from the subscription list for the given event type.
        /// Safe to call if the handler is not subscribed (no-op in that case).
        /// </summary>
        /// <typeparam name="TEvent">The event type to unsubscribe from.</typeparam>
        /// <param name="handler">The handler to remove.</param>
        void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// Publishes an event to all current subscribers of <typeparamref name="TEvent"/>.
        /// Invokes handlers synchronously in subscription order. Dead weak references are
        /// pruned before invocation. Exceptions in individual handlers are caught and logged.
        /// </summary>
        /// <typeparam name="TEvent">The event type to publish.</typeparam>
        /// <param name="eventData">The event payload (must not be null).</param>
        void Publish<TEvent>(TEvent eventData) where TEvent : class;
    }
}
