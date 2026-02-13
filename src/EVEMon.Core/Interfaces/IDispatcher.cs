using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts UI thread marshaling and delayed execution.
    /// Replaces direct dependency on the static <c>Dispatcher</c> class.
    /// </summary>
    public interface IDispatcher
    {
        /// <summary>
        /// Invoke the provided delegate on the UI thread and wait for completion.
        /// If already on the UI thread, executes immediately.
        /// </summary>
        /// <param name="action">The action to invoke.</param>
        void Invoke(Action action);

        /// <summary>
        /// Post the provided delegate to the UI thread without blocking the caller.
        /// If already on the UI thread, executes immediately.
        /// </summary>
        /// <param name="action">The action to invoke on the UI thread.</param>
        void Post(Action action);

        /// <summary>
        /// Schedule an action to invoke on the UI thread after a specified delay.
        /// </summary>
        /// <param name="delay">The delay before execution.</param>
        /// <param name="action">The action to execute.</param>
        void Schedule(TimeSpan delay, Action action);
    }
}
