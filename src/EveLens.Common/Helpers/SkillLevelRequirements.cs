// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Models;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Splits the skill points a character still needs across a list of skill levels,
    /// so that the per-level amounts add up to the total without counting any point twice.
    /// </summary>
    public static class SkillLevelRequirements
    {
        /// <summary>
        /// The skill points each listed level still needs on its own. A level counts only
        /// the points above the character's current points and above every lower listed
        /// level of the same skill. Levels may be given in any order and may repeat.
        /// </summary>
        /// <param name="character">The character whose current skill points are used.</param>
        /// <param name="levels">The skill levels to split the requirement across.</param>
        /// <exception cref="ArgumentNullException">character or levels</exception>
        public static IReadOnlyDictionary<(int SkillId, int Level), long> SkillPointsPerLevel(
            BaseCharacter character, IEnumerable<(StaticSkill Skill, int Level)> levels)
        {
            if (character == null) throw new ArgumentNullException(nameof(character));
            if (levels == null) throw new ArgumentNullException(nameof(levels));

            var result = new Dictionary<(int SkillId, int Level), long>();
            foreach (var group in levels.GroupBy(item => item.Skill))
            {
                StaticSkill skill = group.Key;
                long covered = character.GetSkillPoints(skill);
                foreach (int level in group.Select(item => item.Level).Distinct().OrderBy(level => level))
                {
                    long target = skill.GetPointsRequiredForLevel(level);
                    result[(skill.ID, level)] = Math.Max(0, target - covered);
                    covered = Math.Max(covered, target);
                }
            }
            return result;
        }
    }
}
