// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Tests for the optimizer window's Reset action: removing all remap points must return
    /// the plan to exactly its no-remap training state.
    /// </summary>
    [Collection("StaticData")]
    public class RemapClearTests
    {
        public RemapClearTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        [Fact]
        public void ClearRemaps_RemovesEveryRemapPoint()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 4);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 4);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(
                plan, maxRemaps: 2, minSegmentDays: 0);
            RemapPlanningService.Apply(plan, proposal);
            plan.Count(e => e.Remapping != null).Should().BeGreaterThan(0, "setup: remaps applied");

            RemapPlanningService.ClearRemaps(plan);

            plan.Count(e => e.Remapping != null).Should().Be(0,
                "Reset must return the plan to pure current-attribute training");
        }

        [Fact]
        public void ClearRemaps_RestoresNoRemapTrainingTime()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 4);

            var baseline = plan.GetTotalTime(null, applyRemappingPoints: true);

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 1);
            RemapPlanningService.Apply(plan, proposal);
            RemapPlanningService.ClearRemaps(plan);

            plan.GetTotalTime(null, applyRemappingPoints: true).Should().Be(baseline,
                "apply followed by reset must be a perfect round-trip");
        }
    }
}
