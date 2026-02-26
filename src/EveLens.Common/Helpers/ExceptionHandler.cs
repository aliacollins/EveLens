// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using EveLens.Common.Extensions;
using EveLens.Common.Services;

namespace EveLens.Common.Helpers
{
    public static class ExceptionHandler
    {
        /// <summary>
        /// Logs an exception.
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <param name="handled">if set to <c>true</c> [handled].</param>
        public static void LogException(Exception e, bool handled)
        {
            LogException(e, handled ? "Handled exception" : "Unhandled exception");
        }

        /// <summary>
        /// Logs a rethrown exception.
        /// </summary>
        /// <param name="e">The exception.</param>
        public static void LogRethrowException(Exception e)
        {
            LogException(e, "Exception caught and rethrown");
        }

        /// <summary>
        /// Logs the exception.
        /// </summary>
        /// <param name="e">The exception.</param>
        /// <param name="header">The header.</param>
        private static void LogException(Exception e, string header)
        {
            Trace.WriteLine(string.Empty);
            AppServices.TraceService?.Trace(header);
            Trace.Indent();
            Trace.WriteLine(e.ToString().RemoveProjectLocalPath());
            Trace.WriteLine(string.Empty);
            Trace.Unindent();
        }
    }
}