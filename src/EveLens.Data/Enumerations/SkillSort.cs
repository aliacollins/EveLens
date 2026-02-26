// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Enumeration of skill browser sorter.
    /// </summary>
    public enum SkillSort
    {
        [Description("No Sorting")]
        None = 0,

        [Description("Time to Next Level")]
        TimeToNextLevel = 1,

        [Description("Time to Max Level")]
        TimeToLevel5 = 2,

        [Description("Skill Rank")]
        Rank = 3,

        [Description("Skill Points per Hour")]
        SPPerHour = 4
    }
}