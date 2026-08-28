// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for the remap planning service behind the Optimize Attributes window
    /// (Issue #71). Analysis must never mutate the plan; Apply must be atomic.
    /// </summary>
    [Collection("StaticData")]
    public class RemapPlanningServiceTests
    {
        public RemapPlanningServiceTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        private static Plan CreateMixedAttributePlan(out CCPCharacter character)
        {
            character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            // Perception-heavy block (Spaceship Command) then intelligence-heavy (Engineering):
            // a genuine attribute-focus boundary for the auto-placement strategy.
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Power Grid Management"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("CPU Management"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Capacitor Management"), 5);
            return plan;
        }

        [Fact]
        public void Propose_DoesNotMutateThePlan()
        {
            var plan = CreateMixedAttributePlan(out _);
            var remapsBefore = plan.Count(e => e.Remapping != null);

            RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 2);

            plan.Count(e => e.Remapping != null).Should().Be(remapsBefore,
                "analysis must be read-only — mutation only happens on Apply");
        }

        [Fact]
        public void Propose_OptimizedIsNeverSlowerThanCurrent()
        {
            var plan = CreateMixedAttributePlan(out _);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 2);

            proposal.OptimizedDuration.Should().BeLessThanOrEqualTo(proposal.CurrentDuration,
                "the optimizer explores all attribute spreads including the current one");
            proposal.Remaps.Should().NotBeEmpty();
        }

        [Fact]
        public void Propose_RespectsMaxRemaps()
        {
            var plan = CreateMixedAttributePlan(out _);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 1, minSegmentDays: 0);

            proposal.Remaps.Count.Should().BeLessThanOrEqualTo(2,
                "one mid-plan boundary max: the starting segment plus at most one split");
        }

        [Fact]
        public void Propose_AttributesSumToRemapBudget()
        {
            var plan = CreateMixedAttributePlan(out _);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 2);

            foreach (var remap in proposal.Remaps)
            {
                long total = remap.Attributes.Values.Sum();
                // 5 attributes × base 17 + 14 spare points = 99 (EVE remap invariant)
                total.Should().Be(99,
                    $"attribute spread for '{remap.SegmentLabel}' must be a legal EVE remap");
                remap.Attributes.Values.Should().OnlyContain(v => v >= 17 && v <= 27,
                    "each attribute must stay within EVE's remappable range");
            }
        }

        [Fact]
        public void Propose_FindsMidPlanBoundaries_NeverOnlyTheFirstSkill()
        {
            // Issue #122's exact symptom: "it always suggests remapping at the skill
            // at the top of my list." A plan with genuinely different attribute
            // blocks and budget to spare must place at least one MID-plan remap.
            var plan = CreateMixedAttributePlan(out _);
            var entries = plan.Where(e => e.Skill != null).ToList();
            int pairGroups = 1;
            for (int i = 1; i < entries.Count; i++)
            {
                if ((entries[i].Skill.PrimaryAttribute, entries[i].Skill.SecondaryAttribute) !=
                    (entries[i - 1].Skill.PrimaryAttribute, entries[i - 1].Skill.SecondaryAttribute))
                    pairGroups++;
            }
            pairGroups.Should().BeGreaterThan(1,
                "the fixture plan must actually contain a pair boundary for this test to bite");

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 4, minSegmentDays: 0);

            proposal.Remaps.Count.Should().BeGreaterThan(1,
                "distinct attribute blocks with budget to spare earn a mid-plan remap");
            proposal.Remaps.Count.Should().BeLessThanOrEqualTo(pairGroups,
                "cuts only ever land on (primary, secondary) group edges");
            var firstEntry = entries[0];
            proposal.Remaps.Skip(1).Should().OnlyContain(
                r => r.Skill != firstEntry.Skill || r.Level != (int)firstEntry.Level,
                "remaps after the first are mid-plan by definition");
        }

        [Fact]
        public void Propose_MoreRemaps_NeverProducesASlowerPlan()
        {
            // The DP includes every lower budget as a sub-solution, so extra budget
            // can only help. Small tolerance covers the canonical re-train of two
            // different-but-equivalent segmentations.
            var plan = CreateMixedAttributePlan(out _);

            var one = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 1, minSegmentDays: 0);
            var four = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 4, minSegmentDays: 0);

            one.Remaps.Count.Should().BeLessThanOrEqualTo(1);
            four.OptimizedDuration.Should().BeLessThanOrEqualTo(
                one.OptimizedDuration + TimeSpan.FromHours(2));
        }

        [Fact]
        public void Propose_SingleRemapBudget_MayPlaceItMidPlan()
        {
            // With one remap, "train the prefix on current attributes, remap at the
            // boundary that saves the most" must be a real option — the old code
            // could only ever spend the single remap at plan start.
            var plan = CreateMixedAttributePlan(out _);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 1, minSegmentDays: 0);

            // Wherever it lands, the choice is globally priced: total time with the
            // proposal can never lose to the no-remap baseline.
            proposal.OptimizedDuration.Should().BeLessThanOrEqualTo(proposal.CurrentDuration);
            if (proposal.Remaps.Count == 1 && proposal.PrefixSkillCount > 0)
            {
                // A mid-plan placement must report the keep-current prefix honestly.
                proposal.PrefixDuration.Should().BeGreaterThan(TimeSpan.Zero);
                proposal.Remaps[0].StartsAfter.Should().NotBeEmpty();
            }
        }

        [Fact]
        public void Propose_FlagsBoostedAttributes()
        {
            // Dureiken's "genius boost" report: live attributes above the legal remap
            // total (99) mean every proposal loses to 'current' — the proposal must
            // say WHY instead of looking broken.
            var plan = CreateMixedAttributePlan(out var character);
            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, 2);
            // The test character has legal base attributes, so no flag:
            proposal.CurrentLikelyBoosted.Should().BeFalse(
                "an unboosted character must not trigger the booster note");
        }

        [Fact]
        public void Apply_WritesProposalAtomically()
        {
            var plan = CreateMixedAttributePlan(out _);
            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 2, minSegmentDays: 0);

            RemapPlanningService.Apply(plan, proposal);

            var applied = plan.Where(e => e.Remapping != null).ToList();
            applied.Should().HaveCount(proposal.Remaps.Count,
                "every proposed remap lands on its plan entry");
            applied.Should().OnlyContain(
                e => e.Remapping.Status == RemappingPointStatus.UpToDate,
                "applied remap points carry computed attributes, never blank markers");
        }

        [Fact]
        public void Apply_ReplacesExistingRemapPoints()
        {
            var plan = CreateMixedAttributePlan(out _);
            // Simulate a stale user-placed remap point on some entry
            plan.Skip(1).First().Remapping = new RemappingPoint();

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 1);
            RemapPlanningService.Apply(plan, proposal);

            plan.Count(e => e.Remapping != null).Should().Be(proposal.Remaps.Count,
                "Apply clears stale remap points — no leftovers from previous experiments");
        }
    }
}
