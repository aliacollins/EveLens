using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts UI thread marshaling and delayed execution.
    /// Replaces direct dependency on the static <c>Dispatcher</c> class in
    /// <c>EVEMon.Common.Threading</c>.
    /// </summary>
    /// <remarks>
    /// All three methods detect whether the caller is already on the UI thread.
    /// If so, <see cref="Invoke"/> and <see cref="Post"/> execute the action immediately.
    ///
    /// <see cref="Schedule"/> uses a WinForms <c>System.Windows.Forms.Timer</c> under the hood,
    /// so the callback always fires on the UI thread regardless of the calling thread.
    ///
    /// Production: <c>DispatcherService</c> in <c>EVEMon.Common/Services/DispatcherService.cs</c>
    /// (delegates to the static <c>Threading.Dispatcher</c>).
    /// Testing: Provide a stub that executes actions synchronously on the calling thread.
    /// </remarks>
    public interface IDispatcher
    {
        /// <summary>
        /// Invokes the action on the UI thread and blocks the caller until it completes.
        /// If already on the UI thread, executes immediately inline.
        /// </summary>
        /// <param name="action">The action to invoke on the UI thread.</param>
        void Invoke(Action action);

        /// <summary>
        /// Posts the action to the UI thread message queue without blocking the caller.
        /// If already on the UI thread, executes immediately inline.
        /// </summary>
        /// <param name="action">The action to invoke on the UI thread.</param>
        void Post(Action action);

        /// <summary>
        /// Schedules an action to execute on the UI thread after a specified delay.
        /// Uses a one-shot WinForms timer internally; the callback fires on the UI thread.
        /// </summary>
        /// <param name="delay">The time to wait before execution.</param>
        /// <param name="action">The action to execute after the delay.</param>
        void Schedule(TimeSpan delay, Action action);
    }
}
