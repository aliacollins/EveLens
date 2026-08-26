// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Overview card density (Issue #72 rework). Compact exists for pilots running
    /// dozens of characters who want more cards per screen.
    /// </summary>
    public enum OverviewDensity
    {
        /// <summary>Full cards: portrait, ship, location, status. The default.</summary>
        Comfortable = 0,

        /// <summary>Smaller cards: essentials only, ~40% more cards per screen.</summary>
        Compact = 1
    }
}
