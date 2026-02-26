// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the industry jobs to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum IndustryJobGrouping
    {
        [Header("Group by job state")]
        State = 0,

        [Header("Group by job state (Desc)")]
        StateDesc = 1,

        [Header("Group by ending date")]
        EndDate = 2,

        [Header("Group by ending date (Desc)")]
        EndDateDesc = 3,

        [Header("Group by installed blueprint type")]
        InstalledItemType = 4,

        [Header("Group by installed blueprint type (Desc)")]
        InstalledItemTypeDesc = 5,

        [Header("Group by output item type")]
        OutputItemType = 6,

        [Header("Group by output item type (Desc)")]
        OutputItemTypeDesc = 7,

        [Header("Group by job activity")]
        Activity = 8,

        [Header("Group by job activity (Desc)")]
        ActivityDesc = 9,

        [Header("Group by installed location")]
        Location = 10,

        [Header("Group by installed location (Desc)")]
        LocationDesc = 11
    }
}