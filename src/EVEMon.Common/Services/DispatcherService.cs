using System;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for the static <see cref="Threading.Dispatcher"/> class.
    /// Implements <see cref="IDispatcher"/> by delegating to the existing static methods.
    /// </summary>
    internal sealed class DispatcherService : IDispatcher
    {
        /// <inheritdoc />
        public void Invoke(Action action)
        {
            Threading.Dispatcher.Invoke(action);
        }

        /// <inheritdoc />
        public void Post(Action action)
        {
            Threading.Dispatcher.Post(action);
        }

        /// <inheritdoc />
        public void Schedule(TimeSpan delay, Action action)
        {
            Threading.Dispatcher.Schedule(delay, action);
        }
    }
}
