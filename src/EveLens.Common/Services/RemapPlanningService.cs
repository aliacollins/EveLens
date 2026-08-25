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

            /// <summary>How many plan entries this segment trains.</summary>
            public int SkillCount { get; init; }

            /// <summary>The skill trained immediately before this remap ("Starts after"),
            /// or empty when the remap is at plan start.</summary>
            public string StartsAfter { get; init; } = string.Empty;

            /// <summary>The dominant primary/secondary pair, e.g. "Perception / Willpower".</summary>
            public string PairLabel { get; init; } = string.Empty;

            /// <summary>The skills this segment trains, in order — "View affected skills".</summary>
            public List<string> SkillNames { get; init; } = new();
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

            /// <summary>Time the plan trains on CURRENT attributes before the first
            /// proposed remap; zero when the first remap is at plan start.</summary>
            public TimeSpan PrefixDuration { get; init; }

            /// <summary>How many entries train before the first proposed remap.</summary>
            public int PrefixSkillCount { get; init; }

            /// <summary>Dominant attribute pair of the keep-current prefix, for the
            /// "Keep current attributes" card.</summary>
            public string PrefixPairLabel { get; init; } = string.Empty;
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

            // ── Global optimization (issue #122, round two) ─────────────────────
            //
            // The first fix keyed boundaries on the (primary, secondary) PAIR — a
            // real improvement (Mem/Per → Mem/Int IS a boundary), but selecting
            // WHICH boundaries get the budget by merging the shortest segments was
            // a heuristic: a short segment can hold one huge rank-8 skill, a long
            // one many cheap skills. The durable answer is exact:
            //
            //   1. Candidate cuts = consecutive (primary, secondary) group edges.
            //   2. Segment cost = remaining SP per pair ÷ the CANONICAL rate for a
            //      spread (BaseCharacter.GetBaseSPPerHour: implants, Alpha/Omega,
            //      everything), minimized over every legal spread.
            //   3. DP[r][j] = min over i<j of DP[r-1][i] + BestCost(i, j), with the
            //      prefix before the first remap training on CURRENT attributes for
            //      free — so "no remap yet" and "no remap at all" are real options.
            //
            // Tie-breaks: fewer remaps within EPSILON of the optimum (a remap locks
            // for a year — don't spend one for minutes), and later boundaries on
            // exact ties.

            // Consecutive (primary, secondary) groups; SP-per-pair prefix sums.
            var groups = new List<(int Start, int End, (EveAttribute P, EveAttribute S) Key)>();
            for (int e = 0; e < entries.Count; e++)
            {
                var key = (entries[e].Skill.PrimaryAttribute, entries[e].Skill.SecondaryAttribute);
                if (groups.Count > 0 && groups[^1].Key == key)
                    groups[^1] = (groups[^1].Start, e + 1, key);
                else
                    groups.Add((e, e + 1, key));
            }

            // Remaining SP per entry from a canonical pass (SP is attribute-independent).
            var spScratch = new CharacterScratchpad(baseScratchpad);
            var entrySp = new long[entries.Count];
            for (int e = 0; e < entries.Count; e++)
            {
                long before = spScratch.GetSkillPoints(entries[e].Skill);
                spScratch.Train(entries[e]);
                entrySp[e] = Math.Max(0,
                    entries[e].Skill.GetPointsRequiredForLevel(entries[e].Level) - before);
            }

            // Keep the exact search instant even on pathological alternating plans:
            // under ~48 candidate groups the O(G² × spreads) sweep is milliseconds;
            // above, the smallest-SP groups merge into a neighbour first. (The old
            // minSegmentDays boundary rule is obsolete — the DP itself decides
            // whether a boundary earns a remap — but the parameter survives for
            // callers.)
            while (groups.Count > 48)
            {
                int smallest = 0;
                double smallestSp = double.MaxValue;
                for (int g = 0; g < groups.Count; g++)
                {
                    double sp = 0;
                    for (int e = groups[g].Start; e < groups[g].End; e++)
                        sp += entrySp[e];
                    if (sp < smallestSp) { smallestSp = sp; smallest = g; }
                }
                int other = smallest == 0 ? 1 : smallest;
                int into = smallest == 0 ? 0 : smallest - 1;
                groups[into] = (groups[into].Start, groups[other].End, groups[into].Key);
                groups.RemoveAt(other);
            }

            // Distinct pairs in the plan, a representative skill per pair (the canonical
            // rate function takes a skill), and SP-per-pair prefix sums at group cuts.
            var pairIndex = new Dictionary<(EveAttribute, EveAttribute), int>();
            var pairSkill = new List<StaticSkill>();
            foreach (var en in entries)
            {
                var key = (en.Skill.PrimaryAttribute, en.Skill.SecondaryAttribute);
                if (!pairIndex.ContainsKey(key))
                {
                    pairIndex[key] = pairSkill.Count;
                    pairSkill.Add(en.Skill);
                }
            }
            int pairCount = pairSkill.Count;
            int cutCount = groups.Count + 1;
            var prefixSp = new double[cutCount, pairCount];
            for (int g = 0; g < groups.Count; g++)
            {
                for (int p = 0; p < pairCount; p++)
                    prefixSp[g + 1, p] = prefixSp[g, p];
                for (int e = groups[g].Start; e < groups[g].End; e++)
                {
                    var key = (entries[e].Skill.PrimaryAttribute,
                        entries[e].Skill.SecondaryAttribute);
                    prefixSp[g + 1, pairIndex[key]] += entrySp[e];
                }
            }

            // Rate tables: hours per SP for every pair, under every legal spread and
            // under the CURRENT attributes. One reused scratchpad; the rate comes from
            // GetBaseSPPerHour so implants and clone state ride along canonically.
            var rateScratch = new CharacterScratchpad(baseScratchpad);
            double[] RatesFor()
            {
                var rates = new double[pairCount];
                for (int p = 0; p < pairCount; p++)
                    rates[p] = rateScratch.GetBaseSPPerHour(pairSkill[p]);
                return rates;
            }
            double[] currentRates = RatesFor();

            var spreads = new List<long[]>();       // [i, p, c, w, m] bases
            var spreadRates = new List<double[]>();
            for (int i = 0; i <= 10; i++)
            for (int p = 0; p <= 10 && i + p <= 14; p++)
            for (int c = 0; c <= 10 && i + p + c <= 14; c++)
            for (int w = 0; w <= 10 && i + p + c + w <= 14; w++)
            {
                int m = 14 - i - p - c - w;
                if (m > 10) continue;
                rateScratch.Intelligence.Base = 17 + i;
                rateScratch.Perception.Base = 17 + p;
                rateScratch.Charisma.Base = 17 + c;
                rateScratch.Willpower.Base = 17 + w;
                rateScratch.Memory.Base = 17 + m;
                spreads.Add(new long[] { 17 + i, 17 + p, 17 + c, 17 + w, 17 + m });
                spreadRates.Add(RatesFor());
            }

            double SegmentHours(int cutFrom, int cutTo, double[] rates)
            {
                double hours = 0;
                for (int p = 0; p < pairCount; p++)
                {
                    double sp = prefixSp[cutTo, p] - prefixSp[cutFrom, p];
                    if (sp > 0 && rates[p] > 0)
                        hours += sp / rates[p];
                }
                return hours;
            }

            // Best spread per contiguous range, memoized.
            var bestCost = new double[cutCount, cutCount];
            var bestSpread = new int[cutCount, cutCount];
            for (int i = 0; i < cutCount; i++)
            for (int j = i + 1; j < cutCount; j++)
            {
                double best = double.MaxValue;
                int arg = 0;
                for (int s = 0; s < spreads.Count; s++)
                {
                    double h = SegmentHours(i, j, spreadRates[s]);
                    if (h < best) { best = h; arg = s; }
                }
                bestCost[i, j] = best;
                bestSpread[i, j] = arg;
            }

            // DP over cuts. dp[r][j]: groups 0..j trained, r remaps spent; the part
            // before the first remap runs on current attributes (dp[0][j]).
            int budget = Math.Max(1, Math.Min(maxRemaps, groups.Count));
            int G = groups.Count;
            var dp = new double[budget + 1, cutCount];
            var from = new int[budget + 1, cutCount];
            for (int j = 0; j <= G; j++)
            {
                dp[0, j] = SegmentHours(0, j, currentRates);
                from[0, j] = 0;
            }
            for (int r = 1; r <= budget; r++)
            for (int j = 0; j <= G; j++)
            {
                dp[r, j] = dp[r - 1, j];            // spending fewer remaps is always legal
                from[r, j] = -1;                    // -1: inherited, no new split here
                for (int i = 0; i < j; i++)
                {
                    double candidate = dp[r - 1, i] + bestCost[i, j];
                    if (candidate <= dp[r, j])      // <=: later boundary wins exact ties
                    {
                        dp[r, j] = candidate;
                        from[r, j] = i;
                    }
                }
            }

            // Fewest remaps within EPSILON of the true optimum: a remap locks for a
            // year, so saving minutes is not worth one.
            double bestHours = dp[budget, G];
            double epsilonHours = Math.Max(1.0, currentDuration.TotalHours * 0.0025);
            int chosenR = budget;
            for (int r = 0; r <= budget; r++)
            {
                if (dp[r, G] <= bestHours + epsilonHours)
                {
                    chosenR = r;
                    break;
                }
            }

            // Reconstruct the chosen cuts (ascending group indices where remaps land).
            var cuts = new List<int>();
            {
                int r = chosenR, j = G;
                while (r > 0)
                {
                    int i = from[r, j];
                    if (i < 0) { r--; continue; }   // inherited: this level added no split
                    cuts.Add(i);
                    j = i;
                    r--;
                }
                cuts.Reverse();
            }

            // Canonical re-train of the CHOSEN configuration: the DP priced segments
            // through the canonical rate function, but the numbers the user sees come
            // from the same scratchpad training the plan editor uses — one source of
            // arithmetic truth, and a live cross-check on the aggregation.
            var result = new List<ProposedRemap>();
            var cumulative = new CharacterScratchpad(baseScratchpad);
            int prefixEntryEnd = cuts.Count > 0 ? groups[cuts[0]].Start : entries.Count;
            for (int e = 0; e < prefixEntryEnd; e++)
                cumulative.Train(entries[e]);
            TimeSpan prefixDuration = cumulative.TrainingTime;
            TimeSpan trainedSoFar = prefixDuration;

            static string PairLabelOf(List<PlanEntry> seg)
            {
                var dominant = seg.GroupBy(
                        e => (e.Skill.PrimaryAttribute, e.Skill.SecondaryAttribute))
                    .OrderByDescending(g => g.Sum(e => e.TrainingTime.Ticks))
                    .First().Key;
                return $"{dominant.Item1} / {dominant.Item2}";
            }

            for (int c = 0; c < cuts.Count; c++)
            {
                int startEntry = groups[cuts[c]].Start;
                int endEntry = c + 1 < cuts.Count ? groups[cuts[c + 1]].Start : entries.Count;
                var seg = entries.GetRange(startEntry, endEntry - startEntry);
                long[] spread = spreads[bestSpread[cuts[c],
                    c + 1 < cuts.Count ? cuts[c + 1] : G]];

                cumulative.Intelligence.Base = spread[0];
                cumulative.Perception.Base = spread[1];
                cumulative.Charisma.Base = spread[2];
                cumulative.Willpower.Base = spread[3];
                cumulative.Memory.Base = spread[4];
                foreach (var entry in seg)
                    cumulative.Train(entry);

                TimeSpan segmentDuration = cumulative.TrainingTime - trainedSoFar;
                trainedSoFar = cumulative.TrainingTime;

                var first = seg[0];
                result.Add(new ProposedRemap
                {
                    Skill = first.Skill,
                    Level = (int)first.Level,
                    SegmentDuration = segmentDuration,
                    SegmentLabel = $"{seg.Count} skills, {PairLabelOf(seg)}",
                    SkillCount = seg.Count,
                    StartsAfter = startEntry > 0
                        ? entries[startEntry - 1].Skill.Name : string.Empty,
                    PairLabel = PairLabelOf(seg),
                    SkillNames = seg.Select(en => $"{en.Skill.Name} {en.Level}").ToList(),
                    Attributes = new Dictionary<EveAttribute, long>
                    {
                        [EveAttribute.Intelligence] = spread[0],
                        [EveAttribute.Perception] = spread[1],
                        [EveAttribute.Charisma] = spread[2],
                        [EveAttribute.Willpower] = spread[3],
                        [EveAttribute.Memory] = spread[4],
                    },
                });
            }

            // If the canonical re-train somehow lands slower than current (it should
            // not — r = 0 is always a DP option), fall back to "no remaps" honestly
            // rather than presenting a plan that loses time.
            if (trainedSoFar > currentDuration)
            {
                result.Clear();
                trainedSoFar = currentDuration;
                prefixDuration = currentDuration;
                prefixEntryEnd = entries.Count;
            }

            long liveBaseTotal = baseScratchpad.Intelligence.Base +
                baseScratchpad.Perception.Base + baseScratchpad.Charisma.Base +
                baseScratchpad.Willpower.Base + baseScratchpad.Memory.Base;
            var prefixEntries = entries.GetRange(0, prefixEntryEnd);
            var proposal = new RemapProposal
            {
                CurrentDuration = currentDuration,
                CurrentIncludesRemaps = entries.Any(e => e.Remapping != null
                    && e.Remapping.Status == RemappingPointStatus.UpToDate),
                OptimizedDuration = trainedSoFar,
                CurrentLikelyBoosted = liveBaseTotal > 99,
                PrefixDuration = prefixDuration,
                PrefixSkillCount = prefixEntryEnd,
                PrefixPairLabel = prefixEntries.Count > 0
                    ? PairLabelOf(prefixEntries) : string.Empty,
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
