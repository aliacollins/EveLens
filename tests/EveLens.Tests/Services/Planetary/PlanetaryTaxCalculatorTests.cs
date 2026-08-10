// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Services.Planetary;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services.Planetary
{
    public class PlanetaryTaxCalculatorTests
    {
        // ═══════════════════════════════════════════════════════════
        // Security Classification (uses raw value, not rounded)
        // ═══════════════════════════════════════════════════════════

        [Theory]
        [InlineData(1.0f, PlanetaryTaxCalculator.SecurityClass.HighSec)]
        [InlineData(0.9f, PlanetaryTaxCalculator.SecurityClass.HighSec)]
        [InlineData(0.5f, PlanetaryTaxCalculator.SecurityClass.HighSec)]
        [InlineData(0.45f, PlanetaryTaxCalculator.SecurityClass.HighSec)]     // boundary: >= 0.45
        [InlineData(0.449f, PlanetaryTaxCalculator.SecurityClass.LowSec)]    // just below threshold
        [InlineData(0.4f, PlanetaryTaxCalculator.SecurityClass.LowSec)]
        [InlineData(0.1f, PlanetaryTaxCalculator.SecurityClass.LowSec)]
        [InlineData(0.001f, PlanetaryTaxCalculator.SecurityClass.LowSec)]    // positive = lowsec
        [InlineData(0.0f, PlanetaryTaxCalculator.SecurityClass.NullSec)]     // zero = null
        [InlineData(-0.1f, PlanetaryTaxCalculator.SecurityClass.NullSec)]
        [InlineData(-1.0f, PlanetaryTaxCalculator.SecurityClass.NullSec)]
        public void GetSecurityClass_CorrectClassification(float secStatus, PlanetaryTaxCalculator.SecurityClass expected)
        {
            PlanetaryTaxCalculator.GetSecurityClass(secStatus).Should().Be(expected);
        }

        // ═══════════════════════════════════════════════════════════
        // NPC Base Tax Rates
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void NpcTaxRate_HighSec_Is10Percent()
        {
            PlanetaryTaxCalculator.GetNpcBaseTaxRate(PlanetaryTaxCalculator.SecurityClass.HighSec)
                .Should().Be(0.10);
        }

        [Fact]
        public void NpcTaxRate_LowSec_Is5Percent()
        {
            PlanetaryTaxCalculator.GetNpcBaseTaxRate(PlanetaryTaxCalculator.SecurityClass.LowSec)
                .Should().Be(0.05);
        }

        [Fact]
        public void NpcTaxRate_NullSec_IsZero()
        {
            PlanetaryTaxCalculator.GetNpcBaseTaxRate(PlanetaryTaxCalculator.SecurityClass.NullSec)
                .Should().Be(0.0);
        }

        // ═══════════════════════════════════════════════════════════
        // Customs Code Expertise Skill Reduction
        // ═══════════════════════════════════════════════════════════

        [Theory]
        [InlineData(0.5f, 0, 0.10)]    // HighSec, no skill = full 10%
        [InlineData(0.5f, 1, 0.09)]    // HighSec, L1 = 10% * (1 - 0.1) = 9%
        [InlineData(0.5f, 2, 0.08)]    // HighSec, L2 = 10% * (1 - 0.2) = 8%
        [InlineData(0.5f, 3, 0.07)]    // HighSec, L3 = 10% * (1 - 0.3) = 7%
        [InlineData(0.5f, 4, 0.06)]    // HighSec, L4 = 10% * (1 - 0.4) = 6%
        [InlineData(0.5f, 5, 0.05)]    // HighSec, L5 = 10% * (1 - 0.5) = 5%
        public void EffectiveNpcTaxRate_HighSec_ReducedBySkill(float sec, int skillLevel, double expectedRate)
        {
            PlanetaryTaxCalculator.GetEffectiveNpcTaxRate(sec, skillLevel)
                .Should().BeApproximately(expectedRate, 0.0001);
        }

        [Theory]
        [InlineData(0.3f, 0, 0.05)]    // LowSec, no skill = 5%
        [InlineData(0.3f, 5, 0.025)]   // LowSec, L5 = 5% * 0.5 = 2.5%
        public void EffectiveNpcTaxRate_LowSec_ReducedBySkill(float sec, int skillLevel, double expectedRate)
        {
            PlanetaryTaxCalculator.GetEffectiveNpcTaxRate(sec, skillLevel)
                .Should().BeApproximately(expectedRate, 0.0001);
        }

        [Theory]
        [InlineData(-0.5f, 0, 0.0)]    // NullSec — always 0 regardless of skill
        [InlineData(-0.5f, 5, 0.0)]
        public void EffectiveNpcTaxRate_NullSec_AlwaysZero(float sec, int skillLevel, double expectedRate)
        {
            PlanetaryTaxCalculator.GetEffectiveNpcTaxRate(sec, skillLevel)
                .Should().Be(expectedRate);
        }

        [Theory]
        [InlineData(-1)]   // below valid
        [InlineData(6)]    // above valid
        [InlineData(99)]   // way above
        public void EffectiveNpcTaxRate_ClampedSkillLevel(int invalidLevel)
        {
            // Should not throw, should clamp to valid range
            var rate = PlanetaryTaxCalculator.GetEffectiveNpcTaxRate(0.5f, invalidLevel);
            rate.Should().BeInRange(0.0, 0.10);
        }

        // ═══════════════════════════════════════════════════════════
        // Total Export Tax Rate (NPC + POCO)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void TotalExportTax_NpcOnly_EqualsNpcRate()
        {
            double total = PlanetaryTaxCalculator.GetTotalExportTaxRate(0.5f, 0, null);
            total.Should().Be(0.10); // NPC only, no POCO
        }

        [Fact]
        public void TotalExportTax_WithPoco_AddsRates()
        {
            // HighSec, CCE L3 (7% NPC) + 10% POCO = 17%
            double total = PlanetaryTaxCalculator.GetTotalExportTaxRate(0.5f, 3, 0.10);
            total.Should().BeApproximately(0.17, 0.0001);
        }

        [Fact]
        public void TotalExportTax_NullSecWithPoco_OnlyPoco()
        {
            // NullSec (0% NPC) + 15% POCO = 15%
            double total = PlanetaryTaxCalculator.GetTotalExportTaxRate(-0.5f, 5, 0.15);
            total.Should().BeApproximately(0.15, 0.0001);
        }

        // ═══════════════════════════════════════════════════════════
        // Tax ISK Calculations
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void CalculateExportTax_BasicMath()
        {
            // 1000 ISK base value, 100 units, 10% tax = 10,000 ISK
            double tax = PlanetaryTaxCalculator.CalculateExportTax(1000, 100, 0.10);
            tax.Should().Be(10000);
        }

        [Fact]
        public void CalculateImportTax_HalfOfExportRate()
        {
            // Import is half the export rate
            double exportTax = PlanetaryTaxCalculator.CalculateExportTax(1000, 100, 0.10);
            double importTax = PlanetaryTaxCalculator.CalculateImportTax(1000, 100, 0.10);
            importTax.Should().Be(exportTax / 2.0);
        }

        [Theory]
        [InlineData(0, 100, 0.10)]
        [InlineData(1000, 0, 0.10)]
        [InlineData(1000, 100, 0)]
        [InlineData(-1, 100, 0.10)]
        public void CalculateExportTax_InvalidInputs_ReturnsZero(double baseValue, int qty, double rate)
        {
            PlanetaryTaxCalculator.CalculateExportTax(baseValue, qty, rate).Should().Be(0);
        }

        // ═══════════════════════════════════════════════════════════
        // Security Status Rounding (EVE special rule)
        // ═══════════════════════════════════════════════════════════

        [Theory]
        [InlineData(0.0, 0.0)]         // Zero stays zero
        [InlineData(1.0, 1.0)]         // Max stays max
        [InlineData(0.94, 0.9)]        // Normal rounding
        [InlineData(0.95, 1.0)]        // Normal rounding up
        [InlineData(0.45, 0.5)]        // Rounds to 0.5
        [InlineData(0.44, 0.4)]        // Rounds to 0.4
        [InlineData(-0.45, -0.5)]      // Negative: rounds away from zero
        [InlineData(-0.55, -0.6)]      // Negative: rounds away from zero
        public void RoundSecurityForDisplay_NormalRounding(double input, double expected)
        {
            PlanetaryTaxCalculator.RoundSecurityForDisplay(input)
                .Should().BeApproximately(expected, 0.001);
        }

        [Theory]
        [InlineData(0.01, 0.1)]        // Positive tiny → 0.1 (special rule)
        [InlineData(0.001, 0.1)]       // Very tiny positive → 0.1
        [InlineData(0.04, 0.1)]        // Below 0.05 → 0.1
        [InlineData(0.05, 0.1)]        // Exactly 0.05 → 0.1 (boundary of special rule)
        public void RoundSecurityForDisplay_SpecialPositiveRule(double input, double expected)
        {
            PlanetaryTaxCalculator.RoundSecurityForDisplay(input)
                .Should().BeApproximately(expected, 0.001,
                "positive values in (0, 0.05] must round to 0.1, never 0.0");
        }

        // ═══════════════════════════════════════════════════════════
        // Security Color Codes
        // ═══════════════════════════════════════════════════════════

        [Theory]
        [InlineData(1.0, "#2C75E1")]
        [InlineData(0.9, "#399AEB")]
        [InlineData(0.5, "#F5FF83")]
        [InlineData(0.4, "#DC6C06")]
        [InlineData(0.1, "#731F1F")]
        [InlineData(0.01, "#731F1F")]   // rounds to 0.1 via special rule
        [InlineData(-0.1, "#8D3163")]
        [InlineData(-1.0, "#8D3163")]
        public void GetSecurityColor_MatchesEveStandard(double sec, string expectedHex)
        {
            PlanetaryTaxCalculator.GetSecurityColor(sec).Should().Be(expectedHex);
        }
    }
}
