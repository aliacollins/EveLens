using System;
using System.Windows.Forms;
using EVEMon.Common.Controls;
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
