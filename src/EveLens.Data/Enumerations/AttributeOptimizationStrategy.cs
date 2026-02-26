// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.SkillPlanner
{
    /// <summary>
    /// Remapping strategy.
    /// </summary>
    public enum AttributeOptimizationStrategy
    {
        /// <summary>
        /// Stratagy based on remapping points.
        /// </summary>
        RemappingPoints,

        /// <summary>
        /// Strategy based on the first year from a plan.
        /// </summary>
        OneYearPlan,

        /// <summary>
        /// Strategy based on already trained skills.
        /// </summary>
        Character,

        /// <summary>
        /// Used when the user double-click a remapping point to manually edit it.
        /// </summary>
        ManualRemappingPointEdition
    }
}