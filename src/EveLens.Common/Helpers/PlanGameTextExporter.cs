// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Text;
using EveLens.Common.Extensions;
using EveLens.Common.Models;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Builds the game-compatible plan text ("Skill Name N" per line, numeric levels) that
    /// the EVE client's skill queue accepts when pasted.
    /// </summary>
    public static class PlanGameTextExporter
    {
        /// <summary>
        /// Exports the plan in display order (sort and group-by-priority applied), matching
        /// Save to File. The plan's order is not changed.
        /// </summary>
        public static string Export(Plan plan)
        {
            plan.ThrowIfNull(nameof(plan));

            StringBuilder builder = new StringBuilder();
            foreach (PlanEntry entry in PlanScratchpad.CreateInDisplayOrder(plan))
                builder.AppendLine($"{entry.Skill.Name} {entry.Level}");

            return builder.ToString();
        }
    }
}
