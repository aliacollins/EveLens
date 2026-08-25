// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Models;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Plans WHERE neural remap points should go in a skill plan and computes the optimized
    /// attribute spread for each resulting segment. Pure computation — mutates nothing; the
    /// Optimize Attributes window applies the returned proposal atomically on user confirm.
    /// This is the "auto-insert remaps at attribute-focus boundaries" capability old EVEMon
    /// had, requested in Issue #71.
    /// </summary>
    public static class RemapPlanningService
    {
        /// <summary>A proposed remap: insert before this plan entry with these attributes.</summary>
        public sealed class ProposedRemap
        {
            public StaticSkill Skill { get; init; } = null!;
            public int Level { get; init; }
            public Dictionary<EveAttribute, long> Attributes { get; init; } = new();
            public TimeSpan SegmentDuration { get; init; }
            public string SegmentLabel { get; init; } = string.Empty;
        }

        /// <summary>The full proposal: remap placements plus the before/after plan durations.</summary>
        public sealed class RemapProposal
        {
            public List<ProposedRemap> Remaps { get; } = new();

            /// <summary>Plan duration exactly as the plan editor computes it: current
            /// attributes, implants, and any ALREADY-APPLIED remap points honored.</summary>
            public TimeSpan CurrentDuration { get; init; }

            /// <summary>Whether <see cref="CurrentDuration"/> already benefits from remap
            /// points applied to the plan — shown to the user so the comparison is honest.</summary>
            public bool CurrentIncludesRemaps { get; init; }

            public TimeSpan OptimizedDuration { get; init; }

            /// <summary>
            /// True when the character's live base attributes exceed what any legal
            /// remap can produce (17×5 + 14 assignable = 99 total): an attribute
            /// booster — the "genius boost" accelerator — is active. Every proposal
            /// then loses to "current", and without this flag that read as the
            /// optimizer giving bad values rather than the booster expiring.
            /// </summary>
            public bool CurrentLikelyBoosted { get; init; }
            public TimeSpan TimeSaved => CurrentDuration - OptimizedDuration;
        }

        /// <summary>
        /// Proposes remap placements at attribute-focus boundaries. The plan is scanned in
        /// order; a boundary is where the dominant primary attribute of the upcoming window
        /// of training time changes. Each segment must be at least
        /// <paramref name="minSegmentDays"/> long — a remap is locked for a year in EVE, so
        /// splitting a 20-day block is never worth one.
        /// </summary>
        /// <param name="plan">The plan to analyze (not modified).</param>
        /// <param name="maxRemaps">Maximum remap points to place (the character's available
        /// remaps; typically 1-2).</param>
        /// <param name="minSegmentDays">Minimum days of training per segment.</param>
        /// <param name="cloneOverride">Optional what-if clone state: analyze as Alpha or
        /// Omega regardless of the character's actual status. Null follows the character.</param>
        public static RemapProposal ProposeAtAttributeBoundaries(
            BasePlan plan, int maxRemaps, double minSegmentDays = 30,
            AccountStatusMode? cloneOverride = null)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));

            var entries = plan.Where(e => e.Skill != null).ToList();
            var proposal = BuildSegments(plan, entries, maxRemaps, minSegmentDays, cloneOverride);
            return proposal;
        }

        private static RemapProposal BuildSegments(BasePlan plan, List<PlanEntry> entries,
            int maxRemaps, double minSegmentDays, AccountStatusMode? cloneOverride)
        {
            // Baseline: EXACTLY what the plan editor's total shows — implants applied AND
            // existing remap points honored. Training the raw entries without remaps gave a
            // "current" the user wasn't actually at (466d in the window vs 398d in the panel),
            // which read as broken math (Issue #71 follow-up).
            var baseScratchpad = plan.ChosenImplantSet != null
                ? new CharacterScratchpad(plan.Character.After(plan.ChosenImplantSet))
                : new CharacterScratchpad(plan.Character);

            // What-if clone state: scratchpads inherit AccountStatusSettings on copy, so
            // setting it on the base propagates through every scratchpad derived below.
            // Analysis only — the character's real clone state is never touched.
            if (cloneOverride.HasValue)
                baseScratchpad.AccountStatusSettings = cloneOverride.Value;
            var currentScratch = new CharacterScratchpad(baseScratchpad);
            currentScratch.TrainEntries(entries, applyRemappingPoints: true);
            TimeSpan currentDuration = currentScratch.TrainingTime;

            // Find attribute-focus boundaries: split where the (primary, secondary)
            // PAIR of consecutive entries changes AND the accumulated segment is long
            // enough. The pair, not the primary alone: Memory/Perception →
            // Memory/Intelligence is a real remap boundary (the secondary carries half
            // the weight), and keying on the primary made a plan like
            // [Mem/Per, Mem/Int…, Per/Wil…] read as one Memory block with its only
            // split far too late — "it always suggests remapping at the first skill"
            // (issue #122).
            var segments = new List<List<PlanEntry>>();
            var segment = new List<PlanEntry>();
            TimeSpan segmentTime = TimeSpan.Zero;
            (EveAttribute, EveAttribute) segmentKey = (EveAttribute.None, EveAttribute.None);

            foreach (var entry in entries)
            {
                var key = (entry.Skill.PrimaryAttribute, entry.Skill.SecondaryAttribute);
                bool boundary = segmentKey.Item1 != EveAttribute.None
                    && key != segmentKey
                    && segmentTime.TotalDays >= minSegmentDays;

                if (boundary)
                {
                    segments.Add(segment);
                    segment = new List<PlanEntry>();
                    segmentTime = TimeSpan.Zero;
                }

                if (segment.Count == 0)
                    segmentKey = key;
                segment.Add(entry);
                segmentTime += entry.TrainingTime;
            }
            if (segment.Count > 0)
                segments.Add(segment);

            // The remap budget caps how many segments we may keep (the first consumes
            // a remap at plan start). Merging the SHORTEST segment into its shorter
            // neighbour repeatedly keeps the boundaries that matter: consuming the
            // budget front-to-back instead meant a short leading segment could eat
            // the only remap and the long tail never got one (issue #122's shape).
            while (segments.Count > Math.Max(1, maxRemaps))
            {
                int shortest = 0;
                TimeSpan shortestTime = TimeSpan.MaxValue;
                for (int i = 0; i < segments.Count; i++)
                {
                    TimeSpan t = TimeSpan.FromTicks(
                        segments[i].Sum(e => e.TrainingTime.Ticks));
                    if (t < shortestTime)
                    {
                        shortestTime = t;
                        shortest = i;
                    }
                }
                int mergeInto = shortest == 0 ? 1
                    : shortest == segments.Count - 1 ? shortest - 1
                    : segments[shortest - 1].Sum(e => e.TrainingTime.Ticks) <=
                      segments[shortest + 1].Sum(e => e.TrainingTime.Ticks)
                        ? shortest - 1 : shortest + 1;
                if (mergeInto < shortest)
                {
                    segments[mergeInto].AddRange(segments[shortest]);
                }
                else
                {
                    segments[shortest].AddRange(segments[mergeInto]);
                    segments[mergeInto] = segments[shortest];
                }
                segments.RemoveAt(shortest);
            }

            // Optimize each segment's attributes on a CUMULATIVE scratchpad: segment N
            // trains on top of the SP from segments 1..N-1. Optimizing each segment from
            // the bare base state double-counted prerequisite SP and produced "optimized"
            // times WORSE than the current plan (seen live: 466d current → 503d optimized).
            var result = new List<ProposedRemap>();
            var cumulative = new CharacterScratchpad(baseScratchpad);
            TimeSpan trainedSoFar = TimeSpan.Zero;
            foreach (var seg in segments)
            {
                var best = AttributesOptimizer.Optimize(
                    seg, new CharacterScratchpad(cumulative), TimeSpan.MaxValue);

                // Remap the cumulative scratchpad to the segment's optimal attributes,
                // then train the segment on it — carrying SP and time forward.
                cumulative.Memory.Base = best.Memory.Base;
                cumulative.Charisma.Base = best.Charisma.Base;
                cumulative.Willpower.Base = best.Willpower.Base;
                cumulative.Perception.Base = best.Perception.Base;
                cumulative.Intelligence.Base = best.Intelligence.Base;
                foreach (var entry in seg)
                    cumulative.Train(entry);

                TimeSpan segmentDuration = cumulative.TrainingTime - trainedSoFar;
                trainedSoFar = cumulative.TrainingTime;

                var first = seg[0];
                var dominant = seg.GroupBy(e => e.Skill.PrimaryAttribute)
                    .OrderByDescending(g => g.Sum(e => e.TrainingTime.Ticks))
                    .First().Key;
                result.Add(new ProposedRemap
                {
                    Skill = first.Skill,
                    Level = (int)first.Level,
                    SegmentDuration = segmentDuration,
                    SegmentLabel = $"{seg.Count} skills, {dominant}-focused",
                    Attributes = new Dictionary<EveAttribute, long>
                    {
                        [EveAttribute.Intelligence] = best.Intelligence.Base,
                        [EveAttribute.Perception] = best.Perception.Base,
                        [EveAttribute.Charisma] = best.Charisma.Base,
                        [EveAttribute.Willpower] = best.Willpower.Base,
                        [EveAttribute.Memory] = best.Memory.Base,
                    },
                });
            }

            long liveBaseTotal = baseScratchpad.Intelligence.Base +
                baseScratchpad.Perception.Base + baseScratchpad.Charisma.Base +
                baseScratchpad.Willpower.Base + baseScratchpad.Memory.Base;
            var proposal = new RemapProposal
            {
                CurrentDuration = currentDuration,
                CurrentIncludesRemaps = entries.Any(e => e.Remapping != null
                    && e.Remapping.Status == RemappingPointStatus.UpToDate),
                OptimizedDuration = trainedSoFar,
                CurrentLikelyBoosted = liveBaseTotal > 99,
            };
            proposal.Remaps.AddRange(result);
            return proposal;
        }

        /// <summary>
        /// Removes every remap point from the plan, returning it to training purely on the
        /// character's current attributes. The optimizer window's "Reset" action.
        /// </summary>
        public static void ClearRemaps(BasePlan plan)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            foreach (var entry in plan)
                entry.Remapping = null!;
        }

        /// <summary>
        /// Applies a proposal to the plan ATOMICALLY: clears all existing remap points, then
        /// sets the proposed ones (skipping the first segment — that is the "remap now"
        /// starting state, not a mid-plan point). Single mutation pass; callers refresh once.
        /// </summary>
        public static void Apply(BasePlan plan, RemapProposal proposal)
        {
            if (plan == null) throw new ArgumentNullException(nameof(plan));
            if (proposal == null) throw new ArgumentNullException(nameof(proposal));

            foreach (var entry in plan)
                entry.Remapping = null!;

            // First proposed remap describes the plan START (apply as a remap on the first
            // entry so the character knows the target spread); subsequent ones are mid-plan.
            foreach (var remap in proposal.Remaps)
            {
                var entry = plan.GetEntry(remap.Skill, remap.Level);
                if (entry == null) continue;

                var point = new RemappingPoint();
                point.SetAttributes(
                    (int)remap.Attributes[EveAttribute.Intelligence],
                    (int)remap.Attributes[EveAttribute.Perception],
                    (int)remap.Attributes[EveAttribute.Charisma],
                    (int)remap.Attributes[EveAttribute.Willpower],
                    (int)remap.Attributes[EveAttribute.Memory]);
                entry.Remapping = point;
            }
        }
    }
}
