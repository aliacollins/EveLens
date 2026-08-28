// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// How characters are ordered within each group on the overview (Issue #72 /
    /// Discussion #46). Applies inside every group and to ungrouped characters alike.
    /// </summary>
    public enum OverviewSortMode
    {
        /// <summary>User-defined order via drag-to-reorder. The default.</summary>
        Custom = 0,

        /// <summary>Alphabetical by character name.</summary>
        Name = 1,

        /// <summary>Highest skill points first.</summary>
        SkillPoints = 2,

        /// <summary>
        /// Who needs attention first: characters not training (paused queues) lead,
        /// then training characters by soonest current-skill completion.
        /// </summary>
        TrainingUrgency = 3
    }
}
