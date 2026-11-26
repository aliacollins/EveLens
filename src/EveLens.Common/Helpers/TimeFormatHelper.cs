// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Formats TimeSpan values into compact human-readable strings for display.
    /// </summary>
    public static class TimeFormatHelper
    {
        /// <summary>
        /// Formats a duration in the given style. A day or more shows days and hours
        /// (plus minutes in the full styles), an hour or more shows hours and minutes,
        /// and anything shorter shows minutes (plus seconds in the seconds styles).
        /// Zero and negative durations are formatted as they are.
        /// </summary>
        public static string Format(TimeSpan time, DurationStyle style)
        {
            bool full = style is DurationStyle.Full or DurationStyle.FullSeconds;
            bool seconds = style is DurationStyle.CompactSeconds or DurationStyle.FullSeconds;

            if (time.TotalDays >= 1)
            {
                return full
                    ? $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m"
                    : $"{(int)time.TotalDays}d {time.Hours}h";
            }

            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";

            return seconds
                ? $"{time.Minutes}m {time.Seconds}s"
                : $"{(int)time.TotalMinutes}m";
        }

        /// <summary>
        /// Formats a duration in the given style, or returns <paramref name="whenElapsed"/>
        /// for a zero or negative duration.
        /// </summary>
        public static string Format(TimeSpan time, DurationStyle style, string whenElapsed)
            => time <= TimeSpan.Zero ? whenElapsed : Format(time, style);

        /// <summary>
        /// Formats a remaining time as a compact string: "2d 4h", "3h 22m", or "45m 12s".
        /// Returns "Done" for zero or negative durations.
        /// </summary>
        public static string FormatRemaining(TimeSpan remaining)
            => Format(remaining, DurationStyle.CompactSeconds, "Done");
    }
}
