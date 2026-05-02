// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Validates and performs skill reordering in plans while enforcing
    /// prerequisite constraints. Used by drag-and-drop and keyboard reorder.
    /// </summary>
    public static class PlanReorderService
    {
        /// <summary>
        /// Tests whether moving the skills at the given indices to the target
        /// insertion point would violate prerequisite ordering.
        /// </summary>
        /// <param name="entries">The current ordered plan entries.</param>
        /// <param name="selectedIndices">Indices of skills being moved.</param>
        /// <param name="insertBefore">The index to insert before (0 = top, Count = bottom).</param>
        /// <returns>True if the move is valid.</returns>
        public static bool CanMove(
            IReadOnlyList<PlanEntry> entries,
            IReadOnlyList<int> selectedIndices,
            int insertBefore)
        {
            if (entries.Count == 0 || selectedIndices.Count == 0)
                return false;

            var simulated = SimulateMove(entries, selectedIndices, insertBefore);
            return ValidatePrerequisiteOrder(simulated);
        }

        /// <summary>
        /// Performs the move, returning the new ordered list.
        /// Caller is responsible for validating first via CanMove.
        /// </summary>
        public static List<PlanEntry> PerformMove(
            IReadOnlyList<PlanEntry> entries,
            IReadOnlyList<int> selectedIndices,
            int insertBefore)
        {
            return SimulateMove(entries, selectedIndices, insertBefore);
        }

        /// <summary>
        /// Simulates moving selected entries to a new position.
        /// </summary>
        private static List<PlanEntry> SimulateMove(
            IReadOnlyList<PlanEntry> entries,
            IReadOnlyList<int> selectedIndices,
            int insertBefore)
        {
            var sortedIndices = selectedIndices.OrderBy(i => i).ToList();
            var moving = sortedIndices.Select(i => entries[i]).ToList();
            var rest = entries.Where((_, i) => !sortedIndices.Contains(i)).ToList();

            // Adjust insertion point for removed items above it
            int adjustedInsert = insertBefore;
            foreach (int idx in sortedIndices)
            {
                if (idx < insertBefore)
                    adjustedInsert--;
            }
            adjustedInsert = System.Math.Max(0, System.Math.Min(rest.Count, adjustedInsert));

            rest.InsertRange(adjustedInsert, moving);
            return rest;
        }

        /// <summary>
        /// Returns a human-readable reason why the move is blocked, or null if valid.
        /// </summary>
        public static string? GetBlockingReason(
            IReadOnlyList<PlanEntry> entries,
            IReadOnlyList<int> selectedIndices,
            int insertBefore)
        {
            if (entries.Count == 0 || selectedIndices.Count == 0)
                return "No skills selected";

            var simulated = SimulateMove(entries, selectedIndices, insertBefore);
            return FindPrerequisiteViolation(simulated);
        }

        /// <summary>
        /// Validates that every skill's prerequisites appear before it in the list,
        /// AND that same-skill levels are in ascending order (level II before III, etc.).
        /// </summary>
        private static bool ValidatePrerequisiteOrder(List<PlanEntry> entries)
        {
            return FindPrerequisiteViolation(entries) == null;
        }

        private static string? FindPrerequisiteViolation(List<PlanEntry> entries)
        {
            var positionOf = new Dictionary<(int skillId, long level), int>();

            for (int i = 0; i < entries.Count; i++)
            {
                var key = (entries[i].Skill.ID, entries[i].Level);
                positionOf[key] = i;
            }

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];

                if (entry.Level > 1)
                {
                    var prevLevelKey = (entry.Skill.ID, entry.Level - 1);
                    if (positionOf.TryGetValue(prevLevelKey, out int prevLevelPos))
                    {
                        if (prevLevelPos > i)
                            return $"{entry.Skill.Name} {ToRoman(entry.Level)} needs {entry.Skill.Name} {ToRoman(entry.Level - 1)} first";
                    }
                }

                foreach (var prereq in entry.Skill.Prerequisites)
                {
                    var prereqKey = (prereq.Skill.ID, prereq.Level);
                    if (positionOf.TryGetValue(prereqKey, out int prereqPos))
                    {
                        if (prereqPos > i)
                            return $"{entry.Skill.Name} {ToRoman(entry.Level)} needs {prereq.Skill.Name} {ToRoman(prereq.Level)} first";
                    }
                }
            }

            return null;
        }

        private static string ToRoman(long level) => level switch
        {
            1 => "I", 2 => "II", 3 => "III", 4 => "IV", 5 => "V",
            _ => level.ToString()
        };
    }
}
