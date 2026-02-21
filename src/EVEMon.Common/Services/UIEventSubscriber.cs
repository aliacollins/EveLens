// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using System.Windows.Forms;
using EVEMon.Common.Controls;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Threading;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Extension methods for subscribing to EventAggregator events with UI thread marshaling.
    /// Uses <see cref="Dispatcher.Post(Action)"/> for safe cross-thread invocation.
    /// </summary>
    public static class UIEventSubscriber
    {
        /// <summary>
        /// Subscribe to an event with automatic UI thread marshaling via <see cref="Dispatcher"/>.
        /// Returns <see cref="IDisposable"/> for cleanup in the control's Dispose method.
        /// Returns a no-op disposable if the control is in design mode.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <param name="aggregator">The event aggregator.</param>
        /// <param name="control">The WinForms control that owns this subscription.</param>
        /// <param name="handler">The handler to invoke on the UI thread.</param>
        /// <returns>An <see cref="IDisposable"/> that unsubscribes when disposed.</returns>
        public static IDisposable SubscribeOnUI<TEvent>(
            this IEventAggregator aggregator,
            Control control,
            Action<TEvent> handler) where TEvent : class
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Don't subscribe in Visual Studio designer
            if (control.IsDesignModeHosted())
                return NoOpDisposable.Instance;

            // Wrap handler with UI thread marshaling via Dispatcher.Post
            Action<TEvent> wrapper = e =>
            {
                if (control.IsDisposed)
                    return;

                Dispatcher.Post(() =>
                {
                    if (!control.IsDisposed)
                        handler(e);
                });
            };

            aggregator.Subscribe(wrapper);

            return new UnsubscribeDisposable<TEvent>(aggregator, wrapper);
        }

        /// <summary>
        /// Subscribe to a character-scoped event, filtered to only fire for a specific character.
        /// Uses late-binding <see cref="Func{T}"/> accessor to support mutable character references.
        /// Filter runs BEFORE UI thread dispatch to avoid unnecessary marshaling.
        /// </summary>
        /// <typeparam name="TEvent">The event type (must derive from <see cref="CharacterEventBase"/>).</typeparam>
        /// <param name="aggregator">The event aggregator.</param>
        /// <param name="control">The WinForms control that owns this subscription.</param>
        /// <param name="characterAccessor">A function returning the current character to filter on.</param>
        /// <param name="handler">The handler to invoke on the UI thread.</param>
        /// <returns>An <see cref="IDisposable"/> that unsubscribes when disposed.</returns>
        public static IDisposable SubscribeOnUIForCharacter<TEvent>(
            this IEventAggregator aggregator,
            Control control,
            Func<Character> characterAccessor,
            Action<TEvent> handler) where TEvent : CharacterEventBase
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (characterAccessor == null)
                throw new ArgumentNullException(nameof(characterAccessor));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Don't subscribe in Visual Studio designer
            if (control.IsDesignModeHosted())
                return NoOpDisposable.Instance;

            Action<TEvent> wrapper = e =>
            {
                if (control.IsDisposed)
                    return;

                // Filter BEFORE marshaling to UI thread
                var character = characterAccessor();
                if (character == null || e.Character != character)
                    return;

                Dispatcher.Post(() =>
                {
                    if (!control.IsDisposed)
                        handler(e);
                });
            };

            aggregator.Subscribe(wrapper);

            return new UnsubscribeDisposable<TEvent>(aggregator, wrapper);
        }

        /// <summary>
        /// Subscribe to a batch event, filtered to only fire when the batch contains the
        /// specified character. Filter runs BEFORE UI thread dispatch.
        /// </summary>
        /// <typeparam name="TEvent">The batch event type.</typeparam>
        /// <param name="aggregator">The event aggregator.</param>
        /// <param name="control">The WinForms control that owns this subscription.</param>
        /// <param name="characterAccessor">A function returning the current character to filter on.</param>
        /// <param name="handler">The handler to invoke on the UI thread.</param>
        /// <returns>An <see cref="IDisposable"/> that unsubscribes when disposed.</returns>
        public static IDisposable SubscribeOnUIForCharacterBatch<TEvent>(
            this IEventAggregator aggregator,
            Control control,
            Func<Character> characterAccessor,
            Action<TEvent> handler) where TEvent : class
        {
            if (aggregator == null)
                throw new ArgumentNullException(nameof(aggregator));
            if (control == null)
                throw new ArgumentNullException(nameof(control));
            if (characterAccessor == null)
                throw new ArgumentNullException(nameof(characterAccessor));
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            // Don't subscribe in Visual Studio designer
            if (control.IsDesignModeHosted())
                return NoOpDisposable.Instance;

            Action<TEvent> wrapper = e =>
            {
                if (control.IsDisposed)
                    return;

                var character = characterAccessor();
                if (character == null)
                    return;

                // Check if batch contains this character
                bool contains = false;
                if (e is CharactersBatchUpdatedEvent batch)
                    contains = batch.Characters.Contains(character);
                else if (e is SkillQueuesBatchUpdatedEvent sqBatch)
                    contains = sqBatch.Characters.Contains(character);

                if (!contains)
                    return;

                Dispatcher.Post(() =>
                {
                    if (!control.IsDisposed)
                        handler(e);
                });
            };

            aggregator.Subscribe(wrapper);

            return new UnsubscribeDisposable<TEvent>(aggregator, wrapper);
        }

        /// <summary>
        /// Disposable that calls <see cref="IEventAggregator.Unsubscribe{TEvent}"/> on dispose.
        /// </summary>
        private sealed class UnsubscribeDisposable<TEvent> : IDisposable where TEvent : class
        {
            private IEventAggregator _aggregator;
            private Action<TEvent> _handler;

            public UnsubscribeDisposable(IEventAggregator aggregator, Action<TEvent> handler)
            {
                _aggregator = aggregator;
                _handler = handler;
            }

            public void Dispose()
            {
                var agg = _aggregator;
                var h = _handler;
                _aggregator = null;
                _handler = null;

                agg?.Unsubscribe(h);
            }
        }

        /// <summary>
        /// No-op disposable returned when in design mode.
        /// </summary>
        private sealed class NoOpDisposable : IDisposable
        {
            public static readonly NoOpDisposable Instance = new();
            public void Dispose() { }
        }
    }
}
