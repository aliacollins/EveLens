// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Shared formatting utilities for ISK values and other numeric displays.
    /// </summary>
    public static class FormattingHelper
    {
        /// <summary>
        /// Formats an ISK value with appropriate suffix (B/M/K) for compact display.
        /// </summary>
        public static string FormatIsk(double isk)
        {
            if (Math.Abs(isk) >= 1_000_000_000)
                return $"{isk / 1_000_000_000:F1}B";
            if (Math.Abs(isk) >= 1_000_000)
                return $"{isk / 1_000_000:F1}M";
            if (Math.Abs(isk) >= 1_000)
                return $"{isk / 1_000:F1}K";
            return $"{isk:F0}";
        }
    }
}
