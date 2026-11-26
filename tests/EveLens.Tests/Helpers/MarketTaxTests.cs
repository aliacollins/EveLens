// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Linq;
using System.Reflection;
using EveLens.Common.Data;
using EveLens.Common.Helpers;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    [Collection("StaticData")]
    public class MarketTaxTests
    {
        [Theory]
        [InlineData(0, 7.5)]
        [InlineData(1, 6.675)]
        [InlineData(2, 5.85)]
        [InlineData(3, 5.025)]
        [InlineData(4, 4.2)]
        [InlineData(5, 3.375)]
        public void SalesTax_DropsByElevenPercentOfTheBasePerAccountingLevel(long level, double expectedPercent)
        {
            MarketTax.SalesTaxPercent(level).Should().BeApproximately(expectedPercent, 1e-9);
            MarketTax.SalesTaxRate(level).Should().BeApproximately(expectedPercent / 100.0, 1e-9);
        }

        [Theory]
        [InlineData(-1, 7.5)]
        [InlineData(6, 3.375)]
        [InlineData(99, 3.375)]
        public void SalesTax_ClampsLevelsOutsideTheSkillRange(long level, double expectedPercent)
        {
            MarketTax.SalesTaxPercent(level).Should().BeApproximately(expectedPercent, 1e-9);
        }

        [Fact]
        public void SalesTax_MatchesTheAccountingSkillDescriptionInTheStaticData()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();

            string description = StaticSkills.GetSkillByName("Accounting").Description;

            description.Should().Contain("reduces sales tax by 11%");
            description.Should().Contain("Sales tax starts at 7.5%");
        }

        [Fact]
        public void SkillFarmCode_TakesItsSalesTaxFromMarketTax()
        {
            string? root = FindDirectory("src");
            if (root == null)
                return;

            string[] files =
            {
                Path.Combine(root, "EveLens.Common", "ViewModels", "SkillFarmDashboardViewModel.cs"),
                Path.Combine(root, "EveLens.Avalonia", "Views", "Dialogs", "SkillFarmDashboardWindow.axaml.cs"),
            };
            foreach (string file in files)
            {
                string content = File.ReadAllText(file);
                content.Should().Contain("MarketTax.");
                string[] taxLiterals = { "* 0.02", "0.075", "BaseSalesTaxPercent", "SalesTaxReductionPerLevel" };
                taxLiterals.Where(content.Contains).Should().BeEmpty($"{Path.GetFileName(file)} must not type tax rates in");
            }
        }

        private static string? FindDirectory(string relativePath)
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (Directory.Exists(Path.Combine(candidate, "EveLens.Common")))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
