// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    [Collection("StaticData")]
    public class RemapAdvisorTests
    {
        private static readonly DateTime s_now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        public RemapAdvisorTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        private static EsiAPIAttributes Attributes(int intelligence, int perception, int charisma,
            int willpower, int memory, int bonusRemaps, DateTime? cooldownEnds = null)
        {
            string cooldown = cooldownEnds.HasValue
                ? $",\"accrued_remap_cooldown_date\":\"{cooldownEnds:yyyy-MM-ddTHH:mm:ssZ}\""
                : "";
            string json = $"{{\"intelligence\":{intelligence},\"perception\":{perception},"
                + $"\"charisma\":{charisma},\"willpower\":{willpower},\"memory\":{memory},"
                + $"\"bonus_remaps\":{bonusRemaps}{cooldown}}}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return (EsiAPIAttributes)new DataContractJsonSerializer(typeof(EsiAPIAttributes))
                .ReadObject(stream)!;
        }

        /// <summary>A charisma-heavy character: a poor fit for the plan below.</summary>
        private static CCPCharacter CreateCharacter(int bonus = 0, int bonusRemaps = 2,
            DateTime? cooldownEnds = null)
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.AccountStatusSettings = AccountStatusMode.Omega;
            character.Import(new List<int>());
            character.Import(Attributes(17 + bonus, 17 + bonus, 27 + bonus, 21 + bonus, 17 + bonus,
                bonusRemaps, cooldownEnds));
            return character;
        }

        private static Plan CreatePlan(CCPCharacter character)
        {
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 5);
            return plan;
        }

        [Fact]
        public void CharacterWithoutAttributeData_GetsNoAdvice()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = CreatePlan(character);

            RemapAdvice advice = RemapAdvisor.Advise(plan, s_now);

            advice.State.Should().Be(RemapAdviceState.NoData);
            advice.Proposal.Should().BeNull();
        }

        [Fact]
        public void AttributesThatDoNotAddUp_GetNoAdvice()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.Import(new List<int>());
            character.Import(Attributes(17, 17, 27, 21, 22, 2));
            var plan = CreatePlan(character);

            character.AttributesAreConsistent.Should().BeFalse();
            RemapAdvisor.Advise(plan, s_now).State.Should().Be(RemapAdviceState.NoData);
        }

        [Fact]
        public void EmptyPlan_IsReportedAsEmpty()
        {
            var character = CreateCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            RemapAdvisor.Advise(plan, s_now).State.Should().Be(RemapAdviceState.EmptyPlan);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        public void PoorFit_WithARemapToSpend_AdvisesRemapNow(int bonus)
        {
            var plan = CreatePlan(CreateCharacter(bonus));

            RemapAdvice advice = RemapAdvisor.Advise(plan, s_now);

            advice.State.Should().Be(RemapAdviceState.RemapNow);
            advice.Attributes.Values.Sum().Should().Be(99);
            advice.Attributes.Values.Should().OnlyContain(v => v >= 17 && v <= 27);
            advice.TimeSaved.Should().BeGreaterThan(TimeSpan.FromHours(1));
            advice.OptimizedDuration.Should().BeLessThan(advice.CurrentDuration);
            advice.TemporaryBonus.Should().Be(bonus);
            advice.Proposal.Should().NotBeNull();
        }

        [Fact]
        public void PoorFit_WithNothingToSpend_AdvisesRemapLater()
        {
            DateTime cooldownEnds = s_now.AddDays(104);
            var plan = CreatePlan(CreateCharacter(bonusRemaps: 0, cooldownEnds: cooldownEnds));

            RemapAdvice advice = RemapAdvisor.Advise(plan, s_now);

            advice.State.Should().Be(RemapAdviceState.RemapLater);
            advice.Availability.NextTimedRemap.Should().Be(cooldownEnds);
            advice.Attributes.Should().NotBeEmpty();
            advice.TimeSaved.Should().BeGreaterThan(TimeSpan.Zero);
        }

        [Fact]
        public void CooldownOver_AdvisesRemapNow()
        {
            var plan = CreatePlan(CreateCharacter(bonusRemaps: 0, cooldownEnds: s_now.AddDays(-1)));

            RemapAdvisor.Advise(plan, s_now).State.Should().Be(RemapAdviceState.RemapNow);
        }

        [Fact]
        public void CharacterAlreadyOnTheAdvisedAttributes_NeedsNoRemap()
        {
            var first = RemapAdvisor.Advise(CreatePlan(CreateCharacter()), s_now);
            var character = PlanTestFixture.CreateTestCharacter();
            character.AccountStatusSettings = AccountStatusMode.Omega;
            character.Import(new List<int>());
            character.Import(Attributes(
                (int)first.Attributes[EveAttribute.Intelligence],
                (int)first.Attributes[EveAttribute.Perception],
                (int)first.Attributes[EveAttribute.Charisma],
                (int)first.Attributes[EveAttribute.Willpower],
                (int)first.Attributes[EveAttribute.Memory], 2));

            RemapAdvice advice = RemapAdvisor.Advise(CreatePlan(character), s_now);

            advice.State.Should().Be(RemapAdviceState.NoRemapNeeded);
            advice.TimeSaved.Should().Be(TimeSpan.Zero);
            advice.Proposal.Should().BeNull();
        }

        [Fact]
        public void SavingAtTheThreshold_IsNotWorthARemap()
        {
            RemapAdvisor.IsWorthwhile(RemapAdvisor.WorthwhileSaving).Should().BeFalse();
            RemapAdvisor.IsWorthwhile(RemapAdvisor.WorthwhileSaving + TimeSpan.FromSeconds(1)).Should().BeTrue();
            RemapAdvisor.IsWorthwhile(TimeSpan.Zero).Should().BeFalse();
        }

        [Fact]
        public void AppliedAdvice_IsReportedAsAlreadyPlanned()
        {
            var plan = CreatePlan(CreateCharacter());
            RemapAdvice advice = RemapAdvisor.Advise(plan, s_now);
            RemapPlanningService.Apply(plan, advice.Proposal!);

            RemapAdvice after = RemapAdvisor.Advise(plan, s_now);

            after.State.Should().Be(RemapAdviceState.AlreadyPlanned);
            after.PlannedRemapCount.Should().BeGreaterThan(0);
            after.CurrentDuration.Should().BeCloseTo(advice.OptimizedDuration, TimeSpan.FromMinutes(1));
        }

        [Fact]
        public void Advise_DoesNotChangeThePlan()
        {
            var plan = CreatePlan(CreateCharacter());
            var before = plan.Select(e => (e.Skill.Name, e.Level, e.Remapping)).ToList();

            RemapAdvisor.Advise(plan, s_now);

            plan.Select(e => (e.Skill.Name, e.Level, e.Remapping)).Should().Equal(before);
        }
    }
}
