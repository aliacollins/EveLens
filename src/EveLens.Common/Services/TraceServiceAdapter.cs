// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Legacy adapter that delegates to EveLensClient.Trace().
    /// Superseded by <see cref="TraceService"/> which owns the logic directly.
    /// </summary>
    /// <remarks>
    /// <b>WARNING:</b> Do NOT register this as <c>AppServices.TraceService</c>.
    /// Since EveLensClient.Trace() now delegates back to AppServices.TraceService,
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
            EveLensClient.Trace(message, printMethod);
        }

        /// <inheritdoc />
        public void Trace(string format, params object[] args)
        {
            EveLensClient.Trace(format, args);
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string message, bool printMethod = true)
        {
            if (level < MinimumLevel)
                return;
            EveLensClient.Trace(message, printMethod);
        }

        /// <inheritdoc />
        public void Trace(TraceLevel level, string format, params object[] args)
        {
            if (level < MinimumLevel)
                return;
            EveLensClient.Trace(format, args);
        }

        /// <inheritdoc />
        public void StartLogging(string filePath)
        {
            // Delegated to EveLensClient in the legacy path
            EveLensClient.StartTraceLogging();
        }

        /// <inheritdoc />
        public void StopLogging()
        {
            // Delegated to EveLensClient in the legacy path
            EveLensClient.StopTraceLogging();
        }
    }
}
