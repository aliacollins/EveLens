// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Legacy adapter that delegates to EveMonClient.Trace().
    /// Superseded by <see cref="TraceService"/> which owns the logic directly.
    /// </summary>
    /// <remarks>
    /// <b>WARNING:</b> Do NOT register this as <c>AppServices.TraceService</c>.
    /// Since EveMonClient.Trace() now delegates back to AppServices.TraceService,
    /// using this adapter causes infinite recursion (StackOverflowException).
    /// Use <see cref="TraceService"/> instead.
    /// </remarks>
    [Obsolete("Use TraceService instead. TraceServiceAdapter causes infinite recursion when registered as AppServices.TraceService.")]
    public sealed class TraceServiceAdapter : ITraceService
    {
        /// <inheritdoc />
        public TraceLevel MinimumLevel { get; set; } = TraceLevel.Debug;

        /// <inheritdoc />
        public void Trace(string message, bool printMethod = true)
        {
            EveMonClient.Trace(message, printMethod);
        }

        /// <inheritdoc />
        public void Trace(string format, params object[] args)
        {
            EveMonClient.Trace(format, args);
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string message, bool printMethod = true)
        {
            if (level < MinimumLevel)
                return;
            EveMonClient.Trace(message, printMethod);
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string format, params object[] args)
        {
            if (level < MinimumLevel)
                return;
            EveMonClient.Trace(format, args);
        }

        /// <inheritdoc />
        public void StartLogging(string filePath)
        {
            // Delegated to EveMonClient in the legacy path
            EveMonClient.StartTraceLogging();
        }

        /// <inheritdoc />
        public void StopLogging()
        {
            // Delegated to EveMonClient in the legacy path
            EveMonClient.StopTraceLogging();
        }
    }
}
