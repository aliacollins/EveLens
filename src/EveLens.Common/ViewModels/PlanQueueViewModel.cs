// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Services;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// Manages the flat ordered list of plan queue items with chain inference,
    /// time bar normalization, and reorder operations.
    /// </summary>
    public sealed class PlanQueueManager
    {
        private Plan? _plan;
        private Character? _character;

        public List<PlanQueueItem> Items { get; private set; } = new();
        public Dictionary<int, ChainAssignment> Chains { get; private set; } = new();

        public Plan? Plan => _plan;

        public void Initialize(Plan? plan, Character? character)
        {
            _plan = plan;
            _character = character;
            Rebuild();
        }

        /// <summary>
        /// Full rebuild from the plan's current entries.
        /// </summary>
        public void Rebuild()
        {
            if (_plan == null)
            {
                Items = new List<PlanQueueItem>();
                Chains = new Dictionary<int, ChainAssignment>();
                return;
            }

            // Ensure training times are computed
            _plan.UpdateStatistics();

            // Build item VMs from plan entries
            var entries = _plan.ToList();
            Items = entries.Select(e => new PlanQueueItem(e, _character)).ToList();

            // Infer chains
            Chains = GoalInferenceService.InferChains(entries);

            // Apply chain assignments
            for (int i = 0; i < Items.Count; i++)
            {
                var item = Items[i];
                if (Chains.TryGetValue(item.Skill.ID, out var chain))
                {
                    item.ChainGoalId = chain.GoalSkillId;
                    item.ChainGoalName = chain.GoalSkillName;
                    item.ChainColor = chain.Color;
                    item.IsGoal = chain.GoalSkillId == item.Skill.ID;
                }
            }

            // Compute chain positions from adjacency
            var assignmentList = Items
                .Select(item => Chains.GetValueOrDefault(item.Skill.ID))
                .Where(a => a != null)
                .ToList()!;

            for (int i = 0; i < Items.Count; i++)
            {
                Items[i].ChainPosition = GoalInferenceService.GetChainPosition(
                    assignmentList!, i);
            }

            // Normalize time bars
            RecomputeTimeBars();
        }

        /// <summary>
        /// Recomputes time bar percentages and severity after a reorder.
        /// </summary>
        private void RecomputeTimeBars()
        {
            if (Items.Count == 0) return;

            double maxHours = Items.Max(i => i.TrainingTime.TotalHours);
            if (maxHours <= 0) maxHours = 1;

            foreach (var item in Items)
            {
                double hours = item.TrainingTime.TotalHours;
                item.TimeBarPercent = Math.Max(5, (hours / maxHours) * 100);
                item.TimeSeverity = hours switch
                {
                    > 720 => TimeSeverity.Massive,  // 30+ days
                    > 168 => TimeSeverity.Long,      // 7+ days
                    > 24  => TimeSeverity.Medium,    // 1+ day
                    _     => TimeSeverity.Quick
                };
            }
        }

        /// <summary>
        /// Tests whether a drag move is valid.
        /// </summary>
        public bool CanMove(IReadOnlyList<int> selectedIndices, int insertBefore)
        {
            if (_plan == null) return false;
            return PlanReorderService.CanMove(_plan.ToList(), selectedIndices, insertBefore);
        }

        /// <summary>
        /// Performs a drag move and rebuilds.
        /// Returns the new indices of the moved items.
        /// </summary>
        public List<int> PerformMove(IReadOnlyList<int> selectedIndices, int insertBefore)
        {
            if (_plan == null) return new List<int>();

            var entries = _plan.ToList();

            // Track by (skillId, level) pair — NOT just skillId, since multiple
            // levels of the same skill have the same ID
            var movedKeys = new HashSet<(int skillId, long level)>();
            foreach (int idx in selectedIndices)
            {
                if (idx >= 0 && idx < entries.Count)
                    movedKeys.Add((entries[idx].Skill.ID, entries[idx].Level));
            }

            var newOrder = PlanReorderService.PerformMove(entries, selectedIndices, insertBefore);

            // Apply the new order to the plan
            _plan.RebuildPlanFrom(newOrder);

            // Read back the ACTUAL order after plan enforcement
            var actualOrder = _plan.ToList();
            var newIndices = new List<int>();
            for (int i = 0; i < actualOrder.Count; i++)
            {
                var key = (actualOrder[i].Skill.ID, actualOrder[i].Level);
                if (movedKeys.Contains(key))
                    newIndices.Add(i);
            }

            Rebuild();
            return newIndices;
        }

        /// <summary>
        /// Gets distinct chains in plan order for the legend.
        /// </summary>
        public List<ChainAssignment> GetOrderedChains()
        {
            var seen = new HashSet<int>();
            var result = new List<ChainAssignment>();
            foreach (var item in Items)
            {
                if (seen.Add(item.ChainGoalId) && Chains.TryGetValue(item.Skill.ID, out var chain))
                {
                    if (!result.Any(r => r.GoalSkillId == chain.GoalSkillId))
                        result.Add(chain);
                }
            }
            return result;
        }
    }
}
