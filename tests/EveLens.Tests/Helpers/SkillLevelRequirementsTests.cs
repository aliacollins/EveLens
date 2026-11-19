// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Helpers;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    [Collection("StaticData")]
    public class SkillLevelRequirementsTests
    {
        private static (StaticSkill Skill, int Level)[] Levels(StaticSkill skill, params int[] levels)
            => levels.Select(level => (skill, level)).ToArray();

        [Fact]
        public void EveryLevelListed_EachCountsItsOwnPoints_AndTheSumIsTheTopLevel()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            StaticSkill skill = PlanTestFixture.GetSkill("Navigation");

            var perLevel = SkillLevelRequirements.SkillPointsPerLevel(character, Levels(skill, 1, 2, 3, 4, 5));

            perLevel[(skill.ID, 1)].Should().Be(skill.GetPointsRequiredForLevel(1));
            perLevel[(skill.ID, 4)].Should().Be(
                skill.GetPointsRequiredForLevel(4) - skill.GetPointsRequiredForLevel(3));
            perLevel.Values.Sum().Should().Be(skill.GetPointsRequiredForLevel(5));
        }

        [Fact]
        public void LevelsWithAGap_StillAddUpToTheTopLevel()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            StaticSkill skill = PlanTestFixture.GetSkill("Navigation");

            var perLevel = SkillLevelRequirements.SkillPointsPerLevel(character, Levels(skill, 5, 3));

            perLevel[(skill.ID, 3)].Should().Be(skill.GetPointsRequiredForLevel(3));
            perLevel[(skill.ID, 5)].Should().Be(
                skill.GetPointsRequiredForLevel(5) - skill.GetPointsRequiredForLevel(3));
            perLevel.Values.Sum().Should().Be(skill.GetPointsRequiredForLevel(5));
        }

        [Fact]
        public void PointsAlreadyTrained_AreNotCounted()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            StaticSkill skill = PlanTestFixture.GetSkill("Navigation");
            var trained = character.After(new[] { new StaticSkillLevel(skill, 2) });

            var perLevel = SkillLevelRequirements.SkillPointsPerLevel(trained, Levels(skill, 1, 2, 3));

            perLevel[(skill.ID, 1)].Should().Be(0);
            perLevel[(skill.ID, 2)].Should().Be(0);
            perLevel[(skill.ID, 3)].Should().Be(
                skill.GetPointsRequiredForLevel(3) - skill.GetPointsRequiredForLevel(2));
        }

        [Fact]
        public void RepeatedLevels_AreCountedOnce()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            StaticSkill skill = PlanTestFixture.GetSkill("Navigation");

            var perLevel = SkillLevelRequirements.SkillPointsPerLevel(character, Levels(skill, 2, 2));

            perLevel.Should().HaveCount(1);
            perLevel[(skill.ID, 2)].Should().Be(skill.GetPointsRequiredForLevel(2));
        }

        [Fact]
        public void DifferentSkills_AreSplitIndependently()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            StaticSkill navigation = PlanTestFixture.GetSkill("Navigation");
            StaticSkill cpu = PlanTestFixture.GetSkill("CPU Management");

            var perLevel = SkillLevelRequirements.SkillPointsPerLevel(character,
                Levels(navigation, 1, 2).Concat(Levels(cpu, 1)));

            perLevel.Values.Sum().Should().Be(
                navigation.GetPointsRequiredForLevel(2) + cpu.GetPointsRequiredForLevel(1));
        }
    }
}
