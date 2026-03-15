// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Formats TimeSpan values into compact human-readable strings for display.
    /// </summary>
    public static class TimeFormatHelper
    {
        /// <summary>
        /// Formats a remaining time as a compact string: "2d 4h", "3h 22m", or "45m 12s".
        /// Returns "Done" for zero or negative durations.
        /// </summary>
        public static string FormatRemaining(TimeSpan remaining)
        {
            if (remaining <= TimeSpan.Zero)
                return "Done";

            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}d {remaining.Hours}h";
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m";
            return $"{remaining.Minutes}m {remaining.Seconds}s";
        }
    }
}
