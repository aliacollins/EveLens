// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EVEMon.Common.Data;
using EVEMon.Common.Models;
using EVEMon.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Models
{
    /// <summary>
    /// Runtime tests for <see cref="Plan"/> using real static skill data loaded
    /// via <see cref="PlanTestFixture"/>.
    /// </summary>
    [Collection("StaticData")]
    public class PlanRuntimeTests
    {
        [Fact]
        public void PlanTo_AddsSkillEntry()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            var skill = PlanTestFixture.GetSkill("Spaceship Command");

            plan.PlanTo(skill, 1);

            plan.Should().NotBeEmpty();
            plan.Any(e => e.Skill == skill && e.Level == 1).Should().BeTrue();
        }

        [Fact]
        public void PlanTo_Level3_AddsIntermediateLevels()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            var skill = PlanTestFixture.GetSkill("Spaceship Command");

            plan.PlanTo(skill, 3);

            plan.Count(e => e.Skill == skill).Should().Be(3);
            plan.Any(e => e.Skill == skill && e.Level == 1).Should().BeTrue();
            plan.Any(e => e.Skill == skill && e.Level == 2).Should().BeTrue();
            plan.Any(e => e.Skill == skill && e.Level == 3).Should().BeTrue();
        }

        [Fact]
        public void PlanTo_SkillWithPrerequisites_IncludesPrereqEntries()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            // Find a skill that has prerequisites
            var skill = StaticSkills.AllSkills.FirstOrDefault(s => s.Prerequisites.Any());
            skill.Should().NotBeNull("test requires a skill with prerequisites");

            plan.PlanTo(skill!, 1);

            // The plan should contain entries for the prerequisite skills too
            plan.Count.Should().BeGreaterThan(1);
        }

        [Fact]
        public void Plan_Export_RoundTrips()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character, "RoundTrip Plan");
            var skill = PlanTestFixture.GetSkill("Spaceship Command");
            plan.PlanTo(skill, 2);
            int originalCount = plan.Count;

            var serial = plan.Export();
            var plan2 = new Plan(character, serial);

            plan2.Count.Should().Be(originalCount);
            plan2.Name.Should().Be("RoundTrip Plan");
        }

        [Fact]
        public void GetSkill_ReturnsValidStaticSkill()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var skill = PlanTestFixture.GetSkill("Spaceship Command");

            skill.Should().NotBeNull();
            skill.Name.Should().Be("Spaceship Command");
            skill.ID.Should().BeGreaterThan(0);
        }

        [Fact]
        public void StaticSkills_AllSkills_ContainsEntries()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();

            StaticSkills.AllSkills.Should().NotBeEmpty();
            StaticSkills.ArrayIndicesCount.Should().BeGreaterThan(0);
        }

        [Fact]
        public void CreateTestCharacter_ReturnsValidCharacter()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter("My Pilot", 12345L);

            character.Should().NotBeNull();
            character.Name.Should().Be("My Pilot");
        }

        [Fact]
        public void CreateTestPlanWithCharacter_ReturnsUsablePlan()
        {
            var plan = PlanTestFixture.CreateTestPlanWithCharacter("Quick Plan");

            plan.Should().NotBeNull();
            plan.Name.Should().Be("Quick Plan");
            plan.Count.Should().Be(0);
        }
    }
}
