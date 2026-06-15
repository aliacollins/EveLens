// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Common.Helpers;
using EveLens.Common.Services;
using Microsoft.Extensions.Logging;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Process-wide last-resort exception backstop. Without this, any exception that escapes
    /// a background thread, an unobserved <see cref="Task"/>, or the synchronous Avalonia
    /// lifecycle (e.g. an <c>OnAttachedToVisualTree</c> override) terminates the process
    /// silently with no log — which is exactly how the non-Windows GDI+ fallback crash
    /// presented to users ("crashes a lot, leaves nothing behind").
    /// </summary>
    /// <remarks>
    /// This is a safety net, not a license to leave exceptions unhandled at their source.
    /// Handlers log through the existing <see cref="ExceptionHandler"/>/<see cref="AppServices"/>
    /// pipeline and, where the runtime allows, mark the exception observed so the app keeps
    /// running. <see cref="AppDomain.UnhandledException"/> cannot prevent termination, but it
    /// guarantees the crash is captured before exit.
    /// </remarks>
    public static class GlobalExceptionHandler
    {
        private static bool s_installed;
        private static readonly object s_lock = new();

        private static ILogger Logger =>
            AppServices.LoggerFactory.CreateLogger("GlobalException");

        /// <summary>
        /// Registers the global exception handlers. Idempotent — safe to call more than once.
        /// Call as early as possible in startup (before the UI loop begins).
        /// </summary>
        public static void Install()
        {
            lock (s_lock)
            {
                if (s_installed)
                    return;
                s_installed = true;
            }

            // Unobserved Task exceptions (fire-and-forget, ESI/scheduler background work).
            // Marking observed prevents the runtime from escalating to process termination.
            TaskScheduler.UnobservedTaskException += (_, e) =>
            {
                Log("Unobserved task exception", e.Exception);
                e.SetObserved();
            };

            // Last-resort backstop for any thread. Cannot stop termination, but ensures the
            // exception is logged before the process dies.
            AppDomain.CurrentDomain.UnhandledException += (_, e) =>
            {
                Log($"Unhandled AppDomain exception (terminating={e.IsTerminating})",
                    e.ExceptionObject as Exception);
            };
        }

        /// <summary>
        /// Logs an exception caught by the Avalonia dispatcher's unhandled-exception event.
        /// Wired from <c>App</c> after the framework is initialized. Returning normally (with
        /// the event's Handled flag set by the caller) keeps the UI thread alive.
        /// </summary>
        public static void HandleDispatcherException(Exception? ex)
        {
            Log("Unhandled UI dispatcher exception", ex);
        }

        private static void Log(string header, Exception? ex)
        {
            try
            {
                if (ex != null)
                    ExceptionHandler.LogException(ex, handled: true);

                Logger.LogError(ex, "{Header}", header);
            }
            catch
            {
                // The backstop itself must never throw.
            }
        }
    }
}
