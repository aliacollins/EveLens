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
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// A character with an attribute booster keeps its remappable points and the booster
    /// bonus apart, so remap candidates are compared with the same bonus as the current
    /// attributes.
    /// </summary>
    [Collection("StaticData")]
    public class BoostedAttributeComparisonTests
    {
        private static readonly EveAttribute[] s_attributes =
        {
            EveAttribute.Intelligence, EveAttribute.Perception, EveAttribute.Charisma,
            EveAttribute.Willpower, EveAttribute.Memory
        };

        public BoostedAttributeComparisonTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        /// <summary>A charisma-heavy allocation: a poor fit for an engineering/navigation plan.</summary>
        private static EsiAPIAttributes CharismaHeavy(int bonus, int perceptionImplant = 0) => new()
        {
            Intelligence = 17 + bonus,
            Perception = 17 + bonus + perceptionImplant,
            Charisma = 27 + bonus,
            Willpower = 21 + bonus,
            Memory = 17 + bonus,
            BonusRemaps = 2
        };

        private static CCPCharacter CreateCharacter(int bonus)
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.AccountStatusSettings = AccountStatusMode.Omega;
            character.Import(new List<int>());
            character.Import(CharismaHeavy(bonus));
            return character;
        }

        private static Plan CreatePlan(CCPCharacter character)
        {
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Power Grid Management"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("CPU Management"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Capacitor Management"), 5);
            return plan;
        }

        [Theory]
        [InlineData(0, "Charisma (0) = 27 = (17 + 10 + 0)")]
        [InlineData(3, "Charisma (0) = 30 = (17 + 10 + 0 + 3)")]
        public void RemapDescription_PartsAddUpToTheEffectiveValue(int bonus, string expected)
        {
            var character = CreateCharacter(bonus);
            var scratchpad = new CharacterScratchpad(character);

            RemappingPoint.GetStringForAttribute(EveAttribute.Charisma, scratchpad, scratchpad)
                .Should().Be(expected);
        }

        private static int PerceptionImplant(out long bonus)
        {
            var item = StaticItems.GetItemByName("Ocular Filter - Basic");
            item.Should().NotBeNull("the perception implant must exist in the static data");
            var probe = PlanTestFixture.CreateTestCharacter();
            probe.Import(new List<int> { item.ID });
            bonus = probe.CurrentImplants[EveAttribute.Perception].Bonus;
            bonus.Should().BeGreaterThan(0);
            return item.ID;
        }

        private static Character Restore(SerializableSettingsCharacter serial, long id) =>
            new UriCharacter(new CharacterIdentity(id, "Restored Pilot"),
                new Uri("http://localhost/restored.xml"), serial);

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(12)]
        public void Import_KeepsAllocationAndBoosterApart(int bonus)
        {
            var character = CreateCharacter(bonus);

            character.Intelligence.Base.Should().Be(17);
            character.Perception.Base.Should().Be(17);
            character.Charisma.Base.Should().Be(27);
            character.Willpower.Base.Should().Be(21);
            character.Memory.Base.Should().Be(17);
            foreach (var attribute in s_attributes)
                character[attribute].TemporaryBonus.Should().Be(bonus);
            character.Charisma.EffectiveValue.Should().Be(27 + bonus);
        }

        [Fact]
        public void Import_SubtractsImplantsBeforeSplitting()
        {
            int implant = PerceptionImplant(out long implantBonus);
            var character = PlanTestFixture.CreateTestCharacter();
            character.Import(new List<int> { implant });

            character.Import(CharismaHeavy(12, (int)implantBonus));

            character.Perception.Base.Should().Be(17);
            character.Perception.ImplantBonus.Should().Be(implantBonus);
            character.Perception.TemporaryBonus.Should().Be(12);
            character.Perception.EffectiveValue.Should().Be(17 + implantBonus + 12);
        }

        [Fact]
        public void ImplantsArrivingAfterAttributes_CorrectTheBase()
        {
            int implant = PerceptionImplant(out long implantBonus);
            var character = PlanTestFixture.CreateTestCharacter();
            character.Import(new List<int>());
            character.Import(CharismaHeavy(0, (int)implantBonus));

            character.Import(new List<int> { implant });

            character.Perception.Base.Should().Be(17);
            character.Perception.EffectiveValue.Should().Be(17 + implantBonus);
            s_attributes.Sum(a => character[a].Base).Should().Be(99);
        }

        [Fact]
        public void EffectiveValue_AlwaysMatchesWhatWasReported()
        {
            int implant = PerceptionImplant(out _);
            var character = PlanTestFixture.CreateTestCharacter();
            character.Import(new List<int>());
            var reported = CharismaHeavy(12);
            character.Import(reported);

            character.Import(new List<int> { implant });

            character.Intelligence.EffectiveValue.Should().Be(reported.Intelligence);
            character.Perception.EffectiveValue.Should().Be(reported.Perception);
            character.Charisma.EffectiveValue.Should().Be(reported.Charisma);
            character.Willpower.EffectiveValue.Should().Be(reported.Willpower);
            character.Memory.EffectiveValue.Should().Be(reported.Memory);
        }

        [Fact]
        public void SettingsRoundTrip_KeepsTheSplit()
        {
            var character = CreateCharacter(12);

            var serial = character.Export();
            var restored = Restore(serial, 4242L);

            serial.Attributes.Charisma.Should().Be(39);
            restored.Charisma.Base.Should().Be(27);
            restored.Charisma.TemporaryBonus.Should().Be(12);
            restored.Charisma.EffectiveValue.Should().Be(39);
        }

        [Fact]
        public void StoredAttributesWithABoosterInTheBase_AreSplitOnLoad()
        {
            var character = CreateCharacter(0);
            var serial = character.Export();
            serial.Attributes.Intelligence += 12;
            serial.Attributes.Perception += 12;
            serial.Attributes.Charisma += 12;
            serial.Attributes.Willpower += 12;
            serial.Attributes.Memory += 12;

            var restored = Restore(serial, 4243L);

            restored.Willpower.Base.Should().Be(21);
            restored.Willpower.TemporaryBonus.Should().Be(12);
        }

        [Fact]
        public void Scratchpad_KeepsTheBoosterThroughCopyRemapAndReset()
        {
            var character = CreateCharacter(12);
            var scratchpad = new CharacterScratchpad(character);
            var copy = new CharacterScratchpad(scratchpad);

            copy.Perception.Base = 27;
            copy.Perception.EffectiveValue.Should().Be(27 + 12);
            copy.Reset();

            copy.Perception.Base.Should().Be(17);
            copy.Perception.TemporaryBonus.Should().Be(12);
            copy.Perception.EffectiveValue.Should().Be(17 + 12);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(4)]
        [InlineData(12)]
        public void Proposal_RecommendsARemap_WithOrWithoutABooster(int bonus)
        {
            var plan = CreatePlan(CreateCharacter(bonus));

            var proposal = RemapPlanningService.ProposeAtAttributeBoundaries(plan, maxRemaps: 1);

            proposal.Remaps.Should().NotBeEmpty();
            proposal.TimeSaved.Should().BeGreaterThan(TimeSpan.FromHours(1));
            proposal.CurrentLikelyBoosted.Should().BeFalse();
            foreach (var remap in proposal.Remaps)
            {
                remap.Attributes.Values.Sum().Should().Be(99);
                remap.Attributes.Values.Should().OnlyContain(v => v >= 17 && v <= 27);
            }
        }

        [Fact]
        public void Proposal_PicksTheSameAttributes_WithAndWithoutABooster()
        {
            var plain = RemapPlanningService.ProposeAtAttributeBoundaries(
                CreatePlan(CreateCharacter(0)), maxRemaps: 1);
            var boosted = RemapPlanningService.ProposeAtAttributeBoundaries(
                CreatePlan(CreateCharacter(1)), maxRemaps: 1);

            boosted.Remaps.Should().HaveCount(plain.Remaps.Count);
            boosted.Remaps[0].Attributes.Should().Equal(plain.Remaps[0].Attributes);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(12)]
        public void WholePlanOptimizer_ComparesBothSidesWithTheBooster(int bonus)
        {
            var character = CreateCharacter(bonus);
            var plan = CreatePlan(character);

            var result = AttributesOptimizer.OptimizeFromFirstYearOfPlan(plan);

            result.BestDuration.Should().BeLessThan(result.BaseDuration);
            foreach (var attribute in s_attributes)
                result.BestScratchpad[attribute].TemporaryBonus.Should().Be(bonus);
            s_attributes.Sum(a => result.BestScratchpad[a].Base).Should().Be(99);
        }
    }
}
