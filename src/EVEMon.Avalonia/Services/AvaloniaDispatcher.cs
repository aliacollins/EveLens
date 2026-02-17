using System;
using Avalonia.Threading;
using IDispatcher = EVEMon.Core.Interfaces.IDispatcher;

namespace EVEMon.Avalonia.Services
{
    /// <summary>
    /// Avalonia implementation of <see cref="IDispatcher"/>.
    /// Uses <see cref="Dispatcher.UIThread"/> for UI thread marshaling.
    /// </summary>
    internal sealed class AvaloniaDispatcher : IDispatcher
    {
        public void Invoke(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.InvokeAsync(action).Wait();
        }

        public void Post(Action action)
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                Dispatcher.UIThread.Post(action);
        }

        public void Schedule(TimeSpan delay, Action action)
        {
            DispatcherTimer.RunOnce(action, delay);
        }
    }
}
