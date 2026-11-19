// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// How much detail a formatted duration carries.
    /// </summary>
    public enum DurationStyle
    {
        /// <summary>"2d 4h", "3h 22m", "45m".</summary>
        Compact = 0,

        /// <summary>"2d 4h", "3h 22m", "45m 12s".</summary>
        CompactSeconds = 1,

        /// <summary>"2d 4h 10m", "3h 22m", "45m".</summary>
        Full = 2,

        /// <summary>"2d 4h 10m", "3h 22m", "45m 12s".</summary>
        FullSeconds = 3
    }
}
