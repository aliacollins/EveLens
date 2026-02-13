using System;
using System.Threading;
using System.Windows.Forms;

namespace EVEMon.Common.Threading
{
    public static class Dispatcher
    {
        private static SynchronizationContext s_uiContext;
        private static System.Windows.Forms.Timer s_oneSecondTimer;

        /// <summary>
        /// Starts the dispatcher on the main thread.
        /// </summary>
        /// <param name="thread">The thread.</param>
        /// <remarks>
        /// If the method has already been called previously, this new call will silently fail.
        /// </remarks>
        internal static void Run(Thread thread)
        {
            if (s_uiContext != null)
                return;

            s_uiContext = SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext();

            s_oneSecondTimer = new System.Windows.Forms.Timer { Interval = 1000 };
            s_oneSecondTimer.Tick += OneSecondTickTimer_Tick;
            s_oneSecondTimer.Start();
        }

        /// <summary>
        /// Shutdowns the dispatcher.
        /// </summary>
        internal static void Shutdown()
        {
            if (s_oneSecondTimer == null)
                return;

            s_oneSecondTimer.Stop();
            s_oneSecondTimer.Dispose();
            s_oneSecondTimer = null;
        }

        /// <summary>
        /// Invoke the provided delegate on the underlying actor and wait for completion.
        /// </summary>
        /// <param name="action">The action to invoke</param>
        public static void Invoke(Action action)
        {
            if (s_uiContext == null || SynchronizationContext.Current == s_uiContext)
                action.Invoke();
            else
                s_uiContext.Send(_ => action.Invoke(), null);
        }

        /// <summary>
        /// Schedule an action to invoke on the actor, by specifying the time it will be executed.
        /// Uses System.Threading.Timer instead of WinForms Timer so it works correctly
        /// when called from background threads (WinForms Timer requires a message pump).
        /// </summary>
        /// <param name="time">The time at which the action will be executed.</param>
        /// <param name="action">The action to execute.</param>
        public static void Schedule(TimeSpan time, Action action)
        {
            System.Threading.Timer timer = null;
            timer = new System.Threading.Timer(_ =>
            {
                timer?.Dispose();
                Invoke(action);
            }, null, time, System.Threading.Timeout.InfiniteTimeSpan);
        }

        /// <summary>
        /// Occurs on every second, when the timer ticks.
        /// </summary>
        private static void OneSecondTickTimer_Tick(object sender, EventArgs e)
        {
            EveMonClient.UpdateOnOneSecondTick();
        }
    }
}
