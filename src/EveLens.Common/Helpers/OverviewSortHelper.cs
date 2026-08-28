// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Models;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Orders characters within overview groups (Issue #72 rework). Lives in the
    /// Common layer so the ordering rules are testable without the UI.
    /// </summary>
    public static class OverviewSortHelper
    {
        /// <summary>
        /// Returns the characters ordered by <paramref name="mode"/>. Custom returns
        /// the input order unchanged — the caller owns manual ordering.
        /// </summary>
        public static List<Character> Sort(IEnumerable<Character> characters, OverviewSortMode mode)
        {
            var list = characters.ToList();
            return mode switch
            {
                OverviewSortMode.Name => list
                    .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),

                OverviewSortMode.SkillPoints => list
                    .OrderByDescending(c => c.SkillPoints)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),

                // Who needs attention first: paused/idle queues lead (they bleed
                // training time right now), then training characters by soonest
                // current-skill completion.
                OverviewSortMode.TrainingUrgency => list
                    .OrderBy(c => c.IsTraining ? 1 : 0)
                    .ThenBy(c => c.IsTraining
                        ? c.CurrentlyTrainingSkill?.EndTime ?? DateTime.MaxValue
                        : DateTime.MinValue)
                    .ThenBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList(),

                _ => list
            };
        }
    }
}
