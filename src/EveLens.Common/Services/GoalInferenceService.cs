// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Models;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Infers training "chains" (goal groups) from a flat plan by analyzing the
    /// prerequisite graph. Each skill is assigned to the chain of its nearest
    /// goal skill — a goal being any skill in the plan that no other plan skill
    /// depends on (a leaf in the in-plan dependency DAG).
    /// </summary>
    public static class GoalInferenceService
    {
        /// <summary>
        /// Color palette for chain ribbons. Stable indices — same goal always
        /// gets the same color within a single computation.
        /// </summary>
        public static readonly string[] Palette = new[]
        {
            "#4A94F0", // blue
            "#E84A4A", // red
            "#AA88FA", // purple
            "#E6A632", // gold
            "#54D878", // green
            "#E08050", // orange
            "#50B8B0", // teal
            "#D85A90", // pink
        };

        /// <summary>
        /// Analyzes a flat ordered list of plan entries and assigns each to a
        /// goal chain based on the prerequisite graph.
        ///
        /// Algorithm: Find goal skills (no in-plan dependents). Walk DOWN from
        /// each goal through its prerequisite tree, claiming all prereqs.
        /// Shared prereqs go to the first goal that claims them (by goal skill ID
        /// for stability). Colors are deterministic from goal skill ID.
        /// </summary>
        public static Dictionary<int, ChainAssignment> InferChains(IReadOnlyList<PlanEntry> entries)
        {
            if (entries == null || entries.Count == 0)
                return new Dictionary<int, ChainAssignment>();

            // Index entries by skill ID
            var inPlan = new HashSet<int>();
            var entryById = new Dictionary<int, PlanEntry>();
            var depMap = new Dictionary<int, List<int>>(); // skillId → list of in-plan dependents

            foreach (var entry in entries)
            {
                int sid = entry.Skill.ID;
                inPlan.Add(sid);
                entryById[sid] = entry;
                if (!depMap.ContainsKey(sid))
                    depMap[sid] = new List<int>();
            }

            foreach (var entry in entries)
            {
                foreach (var prereq in entry.Skill.Prerequisites)
                {
                    int prereqId = prereq.Skill.ID;
                    if (inPlan.Contains(prereqId))
                        depMap[prereqId].Add(entry.Skill.ID);
                }
            }

            // Find goal skills: those with zero in-plan dependents
            var goals = new List<int>();
            foreach (var entry in entries)
            {
                int sid = entry.Skill.ID;
                if (depMap[sid].Count == 0)
                    goals.Add(sid);
            }

            if (goals.Count == 0)
                goals = entries.Select(e => e.Skill.ID).ToList();

            // Sort goals by skill ID for deterministic ordering
            goals.Sort();

            // Walk DOWN from each goal through prerequisites, claiming unclaimed skills
            var assignments = new Dictionary<int, ChainAssignment>();

            foreach (int goalId in goals)
            {
                var goalEntry = entryById[goalId];
                // Color is deterministic: based on goal skill ID, not discovery order
                int stableColorIndex = Math.Abs(goalId) % Palette.Length;
                var chainAssignment = new ChainAssignment(
                    goalId,
                    goalEntry.Skill.Name,
                    Palette[stableColorIndex],
                    stableColorIndex);

                // Claim the goal itself
                if (!assignments.ContainsKey(goalId))
                    assignments[goalId] = chainAssignment;

                // Walk prerequisites recursively, claiming unclaimed ones
                ClaimPrerequisites(goalId, chainAssignment, entryById, inPlan, assignments);
            }

            // Any unclaimed skills (shouldn't happen, but safety)
            foreach (var entry in entries)
            {
                int sid = entry.Skill.ID;
                if (!assignments.ContainsKey(sid))
                {
                    int ci = Math.Abs(sid) % Palette.Length;
                    assignments[sid] = new ChainAssignment(sid, entry.Skill.Name, Palette[ci], ci);
                }
            }

            return assignments;
        }

        /// <summary>
        /// Recursively claims all in-plan prerequisites of a skill for a chain.
        /// Shared prereqs: first goal to claim wins (deterministic since goals are sorted by ID).
        /// </summary>
        private static void ClaimPrerequisites(
            int skillId,
            ChainAssignment chain,
            Dictionary<int, PlanEntry> entryById,
            HashSet<int> inPlan,
            Dictionary<int, ChainAssignment> assignments)
        {
            if (!entryById.TryGetValue(skillId, out var entry))
                return;

            foreach (var prereq in entry.Skill.Prerequisites)
            {
                int prereqId = prereq.Skill.ID;
                if (!inPlan.Contains(prereqId))
                    continue;

                // First claimer wins — skip already claimed
                if (assignments.ContainsKey(prereqId))
                    continue;

                assignments[prereqId] = chain;

                // Recurse into this prereq's prerequisites
                ClaimPrerequisites(prereqId, chain, entryById, inPlan, assignments);
            }
        }

        /// <summary>
        /// Computes ChainPosition for each entry based on adjacent chain IDs.
        /// </summary>
        public static ChainPosition GetChainPosition(
            IReadOnlyList<ChainAssignment> assignments, int index)
        {
            if (assignments.Count == 0)
                return ChainPosition.Solo;

            var current = assignments[index];
            bool sameAbove = index > 0 && assignments[index - 1].GoalSkillId == current.GoalSkillId;
            bool sameBelow = index < assignments.Count - 1 && assignments[index + 1].GoalSkillId == current.GoalSkillId;

            if (sameAbove && sameBelow) return ChainPosition.Mid;
            if (!sameAbove && sameBelow) return ChainPosition.First;
            if (sameAbove && !sameBelow) return ChainPosition.Last;
            return ChainPosition.Solo;
        }
    }

    /// <summary>
    /// The result of chain inference for a single skill.
    /// </summary>
    public sealed class ChainAssignment
    {
        public int GoalSkillId { get; }
        public string GoalSkillName { get; }
        public string Color { get; }
        public int ColorIndex { get; }

        public ChainAssignment(int goalSkillId, string goalSkillName, string color, int colorIndex)
        {
            GoalSkillId = goalSkillId;
            GoalSkillName = goalSkillName;
            Color = color;
            ColorIndex = colorIndex;
        }
    }

    /// <summary>
    /// Position of a skill within its chain's contiguous block.
    /// Drives ribbon border-radius and connector line visibility.
    /// </summary>
    public enum ChainPosition
    {
        First,  // top of contiguous block — rounded top corners
        Mid,    // middle — no rounding
        Last,   // bottom — rounded bottom corners
        Solo    // single skill in chain — rounded all corners
    }
}
