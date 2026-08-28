// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Extensions;
using Velopack.Logging;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Routes Velopack's internal log output into EveLens's own trace pipeline.
    /// </summary>
    /// <remarks>
    /// Without this, everything Velopack does — locator initialisation, which feed it
    /// read, the exact <c>Update</c>/<c>UpdateMac</c> command line it spawned, why an
    /// apply was refused — is written to a logger the app never installs and is lost.
    /// That made a failed macOS in-place update indistinguishable from one that never
    /// started: the only symptom was a version that did not change.
    ///
    /// Velopack also keeps its own rolling log next to the app (and on macOS the native
    /// updater writes <c>~/Library/Caches/velopack.log</c>), but those files live outside
    /// the app's own diagnostics. Forwarding into <see cref="ITraceService"/> puts update
    /// activity in the trace file and on the TCP diagnostic stream alongside everything
    /// else, so a user can report an update failure the same way they report any bug.
    ///
    /// The last few lines are also retained in memory so the update UI can show the user
    /// the real reason a download or apply failed instead of silently resetting a button.
    /// </remarks>
    public sealed class VelopackTraceLogger : IVelopackLogger
    {
        /// <summary>How many recent lines to retain for error reporting in the UI.</summary>
        private const int RetainedLines = 20;

        // Velopack logs its staging user id ("Loaded existing staging userId: <guid>"),
        // a GUID that persists for the lifetime of the install — a durable identifier
        // linking every pasted log back to one machine. No GUID in an updater log has
        // diagnostic value to us, so all of them are scrubbed.
        private static readonly System.Text.RegularExpressions.Regex s_guid = new(
            @"\b[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}\b",
            System.Text.RegularExpressions.RegexOptions.Compiled);

        private readonly object _sync = new();
        private readonly Queue<string> _recent = new();

        /// <summary>
        /// The most recent warning or error Velopack reported, or null if it has not
        /// complained. Surfaced in the update dialog when an update cannot be applied.
        /// </summary>
        public string? LastProblem { get; private set; }

        public void Log(VelopackLogLevel logLevel, string? message, Exception? exception)
        {
            if (string.IsNullOrWhiteSpace(message) && exception == null)
                return;

            // Velopack's Trace/Debug levels are extremely chatty (per-chunk download
            // progress); they would drown the trace file for no diagnostic gain.
            if (logLevel < VelopackLogLevel.Information)
                return;

            string text = exception == null
                ? message ?? string.Empty
                : $"{message} — {exception.GetType().Name}: {exception.Message}";

            // Velopack logs absolute paths (packages dir, bundle location), which embed
            // the OS account name. Scrub once here, at the single point of entry, so the
            // trace file, the TCP diagnostic stream, the retained tail and the failure
            // dialog all inherit the redaction.
            text = s_guid.Replace(text.RedactUserName(), "[GUID]");

            string line = $"Velopack [{logLevel}] {text}";

            lock (_sync)
            {
                _recent.Enqueue(line);
                while (_recent.Count > RetainedLines)
                    _recent.Dequeue();

                if (logLevel >= VelopackLogLevel.Warning)
                    LastProblem = text;
            }

            AppServices.TraceService?.Trace(line);
        }

        /// <summary>
        /// The retained tail of Velopack's log, newest last — the context to include in
        /// an update failure report.
        /// </summary>
        public string RecentLog()
        {
            lock (_sync)
                return string.Join(Environment.NewLine, _recent);
        }
    }
}
