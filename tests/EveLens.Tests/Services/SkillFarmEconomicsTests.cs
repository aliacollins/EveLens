// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    public class SkillFarmEconomicsTests
    {
        [Theory]
        [InlineData(new string[0], 3, 0)]
        [InlineData(new[] { "", "", "" }, 3, 1)]
        [InlineData(new[] { "", "", "", "" }, 3, 2)]
        [InlineData(new[] { "", "", "" }, 1, 3)]
        [InlineData(new[] { "Main", "main", " MAIN " }, 1, 1)]
        [InlineData(new[] { "A", "B", "", "" }, 3, 3)]
        [InlineData(new[] { "A", "A", "", "", "", "" }, 3, 3)]
        public void Accounts_ShareLabels_AndPackUnlabeledCharacters(string[] labels, int perAccount, int expected)
        {
            SkillFarmEconomics.AccountsFor(labels, perAccount).Should().Be(expected);
        }

        [Theory]
        [InlineData(0, 3)]
        [InlineData(-4, 3)]
        [InlineData(9, 1)]
        public void CharactersPerAccount_IsClampedToOneThroughThree(int perAccount, int expectedAccounts)
        {
            SkillFarmEconomics.AccountsFor(new[] { "", "", "" }, perAccount).Should().Be(expectedAccounts);
        }

        [Fact]
        public void Monthly_UsesTheTrainingRate_WhenCharactersAreTraining()
        {
            SkillFarmMonthly monthly = SkillFarmEconomics.Monthly(new SkillFarmMonthlyInput
            {
                FarmCharacters = 3,
                AccountLabels = new[] { "", "", "" },
                CharactersPerAccount = 3,
                SkillPointsPerHour = 7500,
                PlexPrice = 5_000_000,
                InjectorPrice = 900_000_000,
                ExtractorPrice = 400_000_000,
                SalesTaxRate = 0.03375,
            });

            monthly.IsEstimated.Should().BeFalse();
            monthly.FarmAccounts.Should().Be(1);
            monthly.MctSlots.Should().Be(2);
            monthly.SkillPointsPerHour.Should().Be(7500);
            monthly.Extractions.Should().BeApproximately(7500.0 * 720 / 500_000, 1e-9);
            monthly.SubscriptionCost.Should().BeApproximately((500 + 2 * 485) * 5_000_000.0, 1e-3);
            monthly.Revenue.Should().BeApproximately(10.8 * 900_000_000 * (1 - 0.03375), 1e-3);
            monthly.ExtractorCost.Should().BeApproximately(10.8 * 400_000_000, 1e-3);
            monthly.Net.Should().BeApproximately(
                monthly.Revenue - monthly.ExtractorCost - monthly.SubscriptionCost, 1e-3);
            monthly.SubscriptionSavings.Should().BeApproximately(monthly.Revenue - monthly.ExtractorCost, 1e-3);
        }

        [Fact]
        public void Monthly_EstimatesAtTheMaximumRate_WhenNothingIsTraining()
        {
            SkillFarmMonthly monthly = SkillFarmEconomics.Monthly(new SkillFarmMonthlyInput
            {
                FarmCharacters = 2,
                AccountLabels = new[] { "", "" },
                CharactersPerAccount = 1,
                SkillPointsPerHour = 0,
                PlexPrice = 5_000_000,
                InjectorPrice = 900_000_000,
                ExtractorPrice = 400_000_000,
            });

            monthly.IsEstimated.Should().BeTrue();
            monthly.SkillPointsPerHour.Should().Be(5400);
            monthly.FarmAccounts.Should().Be(2);
            monthly.MctSlots.Should().Be(0);
        }

        [Fact]
        public void Monthly_WithNoCharacters_IsAllZero()
        {
            SkillFarmMonthly monthly = SkillFarmEconomics.Monthly(new SkillFarmMonthlyInput());

            monthly.IsEstimated.Should().BeFalse();
            monthly.Extractions.Should().Be(0);
            monthly.SubscriptionCost.Should().Be(0);
            monthly.Net.Should().Be(0);
            monthly.SubscriptionSavings.Should().Be(0);
        }

        [Fact]
        public void Monthly_RejectsNullInput()
        {
            Action act = () => SkillFarmEconomics.Monthly(null!);

            act.Should().Throw<ArgumentNullException>();
        }
    }
}
