// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// The status of a industry job.
    /// </summary>
    /// <remarks>The integer value determines the sort order in "Group by...".</remarks>
    public enum JobState
    {
        [Header("Active jobs")]
        Active = 0,

        [Header("Delivered jobs")]
        Delivered = 1,

        [Header("Canceled jobs")]
        Canceled = 2,

        [Header("Paused jobs")]
        Paused = 3,

        [Header("Failed jobs")]
        Failed = 4
    }
}