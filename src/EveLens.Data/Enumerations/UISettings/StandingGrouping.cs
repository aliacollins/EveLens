// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the standings to be grouped by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum StandingGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by group")]
        Group = 1,

        [Header("Group by status")]
        Status = 2
    }
}
