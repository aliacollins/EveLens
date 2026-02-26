// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Microsoft.Extensions.Logging;

namespace EveLens.Common.Logging
{
    /// <summary>
    /// MEL logger provider that bridges to <see cref="System.Diagnostics.Trace.WriteLine"/>,
    /// routing all structured log entries to the existing trace file (TextWriterTraceListener).
    /// </summary>
    public sealed class TraceLoggerProvider : ILoggerProvider
    {
        /// <inheritdoc />
        public ILogger CreateLogger(string categoryName) => new TraceLogger(categoryName);

        /// <inheritdoc />
        public void Dispose()
        {
            // No resources to release — Trace.Listeners is owned by TraceService
        }

        /// <summary>
        /// Logger that writes formatted entries to System.Diagnostics.Trace.
        /// </summary>
        private sealed class TraceLogger : ILogger
        {
            private readonly string _category;

            public TraceLogger(string category)
            {
                _category = category;
            }

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
                Exception? exception, Func<TState, Exception?, string> formatter)
            {
                if (!IsEnabled(logLevel))
                    return;

                string msg = formatter(state, exception);
                if (exception != null)
                    msg += " | " + exception.Message;

                string level = logLevel switch
                {
                    LogLevel.Trace => "Trace",
                    LogLevel.Debug => "Debug",
                    LogLevel.Information => "Info",
                    LogLevel.Warning => "Warning",
                    LogLevel.Error => "Error",
                    LogLevel.Critical => "Critical",
                    _ => "Unknown"
                };

                string timeStr = $"{DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z";
                System.Diagnostics.Trace.WriteLine(
                    $"{timeStr} > [{level}] {_category} - {msg}");
            }
        }
    }
}
