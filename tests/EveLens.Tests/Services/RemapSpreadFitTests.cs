// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations;
using EveLens.Common.Models;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Whether a remap's attributes still suit the skills trained under it.
    /// </summary>
    [Collection("StaticData")]
    public class RemapSpreadFitTests
    {
        private static readonly DateTime s_now = new(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

        private static readonly EveAttribute[] s_attributes =
        {
            EveAttribute.Intelligence, EveAttribute.Perception, EveAttribute.Charisma,
            EveAttribute.Willpower, EveAttribute.Memory,
        };

        public RemapSpreadFitTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        private static CCPCharacter CreateCharacter()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.AccountStatusSettings = AccountStatusMode.Omega;
            character.Import(new List<int>());
            character.Import(new EsiAPIAttributes
            {
                Intelligence = 20, Perception = 20, Charisma = 25, Willpower = 17, Memory = 17, BonusRemaps = 2,
            });
            return character;
        }

        /// <summary>Perception/Willpower skills with an Intelligence/Perception skill between them.</summary>
        private static Plan CreateMixedPlan(CCPCharacter character)
        {
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Gunnery"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Drones"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("Power Grid Management"), 5);
            plan.PlanTo(PlanTestFixture.GetSkill("CPU Management"), 5);
            plan.UpdateStatistics();
            return plan;
        }

        private static Dictionary<EveAttribute, long> Spread(long intelligence, long perception,
            long charisma, long willpower, long memory) => new()
        {
            [EveAttribute.Intelligence] = intelligence,
            [EveAttribute.Perception] = perception,
            [EveAttribute.Charisma] = charisma,
            [EveAttribute.Willpower] = willpower,
            [EveAttribute.Memory] = memory,
        };

        private static List<(RemappingPoint Remap, List<PlanEntry> Block)> RemapBlocks(Plan plan)
        {
            var blocks = new List<(RemappingPoint, List<PlanEntry>)>();
            foreach (PlanEntry entry in plan)
            {
                if (entry.Remapping != null)
                    blocks.Add((entry.Remapping, new List<PlanEntry>()));
                if (blocks.Count > 0)
                    blocks[^1].Item2.Add(entry);
            }
            return blocks;
        }

        [Fact]
        public void AppliedSchedule_IsNotOutdated_EvenWhenABlockMixesAttributePairs()
        {
            var character = CreateCharacter();
            Plan plan = CreateMixedPlan(character);
            RemapAdvice advice = RemapAdvisor.Advise(plan, s_now);
            RemapPlanningService.Apply(plan, advice.Proposal!);
            plan.UpdateStatistics();

            var blocks = RemapBlocks(plan);

            blocks.Should().NotBeEmpty();
            foreach (var (remap, block) in blocks)
            {
                var spread = s_attributes.ToDictionary(attribute => attribute, attribute => remap[attribute]);
                RemapAdvisor.IsSpreadOutdated(plan, block, spread).Should().BeFalse();
                RemapPlanningService.SavingOverSpread(plan, block, spread)
                    .Should().BeLessThan(TimeSpan.FromMinutes(1));
            }
        }

        [Fact]
        public void BestSpreadForAMixedBlock_NeedNotLeadWithTheBlocksMainPair()
        {
            var character = CreateCharacter();
            Plan plan = CreateMixedPlan(character);
            List<PlanEntry> block = plan.Where(entry =>
                entry.Skill.Name is "Spaceship Command" or "Navigation" or "Gunnery").ToList();

            TimeSpan perceptionWillpower = RemapPlanningService.SavingOverSpread(plan, block,
                Spread(17, 27, 17, 21, 17));
            TimeSpan perceptionIntelligence = RemapPlanningService.SavingOverSpread(plan, block,
                Spread(20, 27, 17, 18, 17));

            perceptionIntelligence.Should().BeLessThan(perceptionWillpower);
            RemapAdvisor.IsSpreadOutdated(plan, block, Spread(20, 27, 17, 18, 17)).Should().BeFalse();
        }

        [Fact]
        public void SpreadForOtherAttributes_IsOutdated()
        {
            var character = CreateCharacter();
            Plan plan = CreateMixedPlan(character);
            List<PlanEntry> block = plan.ToList();

            var charismaHeavy = Spread(17, 17, 27, 21, 17);

            RemapPlanningService.SavingOverSpread(plan, block, charismaHeavy)
                .Should().BeGreaterThan(RemapAdvisor.WorthwhileSaving);
            RemapAdvisor.IsSpreadOutdated(plan, block, charismaHeavy).Should().BeTrue();
        }

        [Fact]
        public void ShortBlock_UnderAPoorSpread_IsNotWorthFlagging()
        {
            var character = CreateCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 2);
            plan.UpdateStatistics();

            RemapAdvisor.IsSpreadOutdated(plan, plan.ToList(), Spread(17, 17, 27, 21, 17)).Should().BeFalse();
        }

        [Fact]
        public void NoEntries_SaveNothing()
        {
            var character = CreateCharacter();
            Plan plan = CreateMixedPlan(character);

            RemapPlanningService.SavingOverSpread(plan, Array.Empty<PlanEntry>(), Spread(17, 17, 27, 21, 17))
                .Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void SavingOverSpread_DoesNotChangeThePlan()
        {
            var character = CreateCharacter();
            Plan plan = CreateMixedPlan(character);
            var before = plan.Select(entry => (entry.Skill.Name, entry.Level, entry.TrainingTime)).ToList();

            RemapPlanningService.SavingOverSpread(plan, plan.ToList(), Spread(17, 17, 27, 21, 17));

            plan.Select(entry => (entry.Skill.Name, entry.Level, entry.TrainingTime)).Should().Equal(before);
            character.Charisma.Base.Should().Be(25);
        }
    }
}
