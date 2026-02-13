using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Simple event aggregator implementing publish/subscribe pattern.
    /// Uses strong references by default; opt-in weak references via <see cref="SubscribeWeak{TEvent}"/>.
    /// </summary>
    /// <remarks>
    /// ~200 LOC as specified in the PRD. Thread-safe via ConcurrentDictionary.
    /// Designed to coexist with EveMonClient's 74 static events (Strangler Fig pattern).
    /// </remarks>
    internal sealed class EventAggregator : IEventAggregator
    {
        private readonly ConcurrentDictionary<Type, List<SubscriptionBase>> _subscriptions
            = new ConcurrentDictionary<Type, List<SubscriptionBase>>();

        /// <inheritdoc />
        public void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subs = _subscriptions.GetOrAdd(typeof(TEvent), _ => new List<SubscriptionBase>());
            lock (subs)
            {
                subs.Add(new StrongSubscription<TEvent>(handler));
            }
        }

        /// <inheritdoc />
        public void SubscribeWeak<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var subs = _subscriptions.GetOrAdd(typeof(TEvent), _ => new List<SubscriptionBase>());
            lock (subs)
            {
                subs.Add(new WeakSubscription<TEvent>(handler));
            }
        }

        /// <inheritdoc />
        public void Unsubscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            if (!_subscriptions.TryGetValue(typeof(TEvent), out var subs))
                return;

            lock (subs)
            {
                for (int i = subs.Count - 1; i >= 0; i--)
                {
                    if (subs[i].Matches(handler))
                    {
                        subs.RemoveAt(i);
                        break;
                    }
                }
            }
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent eventData) where TEvent : class
        {
            if (eventData == null)
                throw new ArgumentNullException(nameof(eventData));

            if (!_subscriptions.TryGetValue(typeof(TEvent), out var subs))
                return;

            SubscriptionBase[] snapshot;
            lock (subs)
            {
                // Clean up dead weak references while we're here
                subs.RemoveAll(s => !s.IsAlive);
                snapshot = subs.ToArray();
            }

            foreach (var sub in snapshot)
            {
                sub.Invoke(eventData);
            }
        }

        #region Subscription Types

        private abstract class SubscriptionBase
        {
            public abstract bool IsAlive { get; }
            public abstract bool Matches<THandler>(THandler handler);
            public abstract void Invoke(object eventData);
        }

        private sealed class StrongSubscription<TEvent> : SubscriptionBase where TEvent : class
        {
            private readonly Action<TEvent> _handler;

            public StrongSubscription(Action<TEvent> handler)
            {
                _handler = handler;
            }

            public override bool IsAlive => true;

            public override bool Matches<THandler>(THandler handler)
            {
                return handler is Action<TEvent> typed && typed == _handler;
            }

            public override void Invoke(object eventData)
            {
                _handler((TEvent)eventData);
            }
        }

        private sealed class WeakSubscription<TEvent> : SubscriptionBase where TEvent : class
        {
            private readonly WeakReference<object> _targetRef;
            private readonly System.Reflection.MethodInfo _method;

            public WeakSubscription(Action<TEvent> handler)
            {
                if (handler.Target != null)
                {
                    _targetRef = new WeakReference<object>(handler.Target);
                    _method = handler.Method;
                }
                else
                {
                    // Static method - keep as strong reference
                    _targetRef = null;
                    _method = handler.Method;
                }
            }

            public override bool IsAlive
            {
                get
                {
                    if (_targetRef == null)
                        return true; // Static method
                    return _targetRef.TryGetTarget(out _);
                }
            }

            public override bool Matches<THandler>(THandler handler)
            {
                if (!(handler is Action<TEvent> typed))
                    return false;

                if (_targetRef == null)
                    return typed.Target == null && typed.Method == _method;

                return _targetRef.TryGetTarget(out var target)
                    && ReferenceEquals(target, typed.Target)
                    && _method == typed.Method;
            }

            public override void Invoke(object eventData)
            {
                if (_targetRef == null)
                {
                    // Static method
                    _method.Invoke(null, new[] { eventData });
                    return;
                }

                if (_targetRef.TryGetTarget(out var target))
                {
                    _method.Invoke(target, new[] { eventData });
                }
            }
        }

        #endregion
    }
}
