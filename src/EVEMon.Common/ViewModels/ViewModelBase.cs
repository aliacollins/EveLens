using System;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels.Binding;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// Base class for all ViewModels. Inherits <see cref="ObservableObject"/> from CommunityToolkit.Mvvm
    /// for <see cref="System.ComponentModel.INotifyPropertyChanged"/> support.
    /// Implements <see cref="IDisposable"/> with automatic subscription tracking (Law #11).
    /// </summary>
    /// <remarks>
    /// Supports constructor injection of <see cref="IEventAggregator"/> and <see cref="IDispatcher"/>
    /// with fallback to <see cref="AppServices"/> for production use. Tests should inject mocks.
    /// </remarks>
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private readonly CompositeDisposable _subscriptions = new CompositeDisposable();
        private bool _disposed;

        /// <summary>
        /// Gets the event aggregator for pub/sub messaging.
        /// </summary>
        protected IEventAggregator EventAggregator { get; }

        /// <summary>
        /// Gets the dispatcher for UI thread marshaling.
        /// </summary>
        protected IDispatcher? Dispatcher { get; }

        /// <summary>
        /// Creates a new ViewModel with explicit dependencies (preferred for testing).
        /// </summary>
        /// <param name="eventAggregator">The event aggregator. Required.</param>
        /// <param name="dispatcher">The dispatcher for UI thread marshaling. Can be null in test scenarios.</param>
        protected ViewModelBase(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
        {
            EventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            Dispatcher = dispatcher;
        }

        /// <summary>
        /// Creates a new ViewModel using <see cref="AppServices"/> defaults (production path).
        /// </summary>
        protected ViewModelBase()
            : this(AppServices.EventAggregator, AppServices.Dispatcher)
        {
        }

        /// <summary>
        /// Subscribes to an event type and auto-tracks the subscription for disposal.
        /// The subscription is automatically cleaned up when this ViewModel is disposed.
        /// </summary>
        /// <typeparam name="TEvent">The event type to subscribe to.</typeparam>
        /// <param name="handler">The handler to invoke when the event is published.</param>
        protected void Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));

            var sub = EventAggregator.Subscribe(handler);
            _subscriptions.Add(sub);
        }

        /// <summary>
        /// Tracks an external <see cref="IDisposable"/> for automatic cleanup on disposal.
        /// </summary>
        /// <param name="disposable">The disposable to track.</param>
        protected void Track(IDisposable disposable)
        {
            if (disposable == null)
                throw new ArgumentNullException(nameof(disposable));

            _subscriptions.Add(disposable);
        }

        /// <summary>
        /// Sets a property value and raises PropertyChanged on the UI thread if a dispatcher is available.
        /// Falls back to normal <see cref="ObservableObject.SetProperty{T}"/> if no dispatcher.
        /// </summary>
        /// <typeparam name="T">The property type.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The property name (auto-filled by compiler).</param>
        /// <returns>True if the value changed.</returns>
        protected bool SetPropertyOnUI<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;

            if (Dispatcher != null)
            {
                Dispatcher.Post(() => OnPropertyChanged(propertyName));
            }
            else
            {
                OnPropertyChanged(propertyName);
            }

            return true;
        }

        /// <summary>
        /// Disposes all tracked subscriptions. Safe to call multiple times.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Override in subclasses for custom cleanup. Always call base.
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                _subscriptions.Dispose();
            }

            _disposed = true;
        }

        /// <summary>
        /// Gets whether this ViewModel has been disposed.
        /// </summary>
        protected bool IsDisposed => _disposed;

        // Bring EqualityComparer into scope for SetPropertyOnUI
        private static class EqualityComparer<T>
        {
            public static readonly System.Collections.Generic.EqualityComparer<T> Default
                = System.Collections.Generic.EqualityComparer<T>.Default;
        }
    }
}
