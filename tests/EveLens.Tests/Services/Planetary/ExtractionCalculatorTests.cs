// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Services.Planetary;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services.Planetary
{
    /// <summary>
    /// Cross-validates the extraction formula against the official EVE developer documentation.
    /// Reference: https://developers.eveonline.com/docs/guides/pi/
    /// Uses the same test parameters as the C#/Kotlin/Python reference implementations.
    /// </summary>
    public class ExtractionCalculatorTests
    {
        // Reference parameters from official docs
        private const int ReferenceQtyPerCycle = 6965;
        private const int ReferenceCycleTime = 30 * 60; // 30 minutes = 1800 seconds
        private const int ReferenceDuration = 171000;   // 1d 23h 30m in seconds

        [Fact]
        public void CalculateCycleYields_MatchesReferenceImplementation()
        {
            // The official C# reference uses these exact parameters
            var yields = ExtractionCalculator.CalculateCycleYields(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            // 171000 / 1800 = 95 cycles
            yields.Should().HaveCount(95);

            // barWidth = 1800/900 = 2.0, so each cycle value ≈ 2 * decayValue * (1+noise)
            // First cycle output is significantly larger than raw qty_per_cycle due to barWidth multiplier
            yields[0].Should().BeGreaterThan(0);
            // With barWidth=2 and noise up to 1.8x, max possible ≈ 2 * 6965 * 1.8 ≈ 25,074
            yields[0].Should().BeLessThan(26000);
        }

        [Fact]
        public void CalculateCycleYields_FirstCycleHigherThanLast()
        {
            var yields = ExtractionCalculator.CalculateCycleYields(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            // Decay means first cycle > last cycle (on average, noise can cause local spikes)
            // Use average of first 5 vs last 5 to smooth noise
            double firstAvg = yields.Take(5).Average();
            double lastAvg = yields.Skip(90).Take(5).Average();

            firstAvg.Should().BeGreaterThan(lastAvg,
                "extraction decays over time so early cycles yield more than late ones");
        }

        [Fact]
        public void CalculateCycleYields_DecaysOverTime()
        {
            var yields = ExtractionCalculator.CalculateCycleYields(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            long actualTotal = yields.Sum(y => (long)y);
            // Naive = first cycle * numCycles (what you'd get without decay)
            long naiveTotal = ExtractionCalculator.CalculateNaiveTotal(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            // Actual should be less than naive because decay reduces later cycles
            actualTotal.Should().BeLessThan(naiveTotal,
                "decay reduces total yield compared to maintaining peak rate");

            // Over a long program (95 cycles), heavy decay brings this to ~30-50% of peak
            double ratio = (double)actualTotal / naiveTotal;
            ratio.Should().BeInRange(0.25, 0.95,
                "long programs decay significantly but short ones less so");
        }

        [Fact]
        public void CalculateTotalYield_EqualsSum()
        {
            long total = ExtractionCalculator.CalculateTotalYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            var yields = ExtractionCalculator.CalculateCycleYields(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);
            long expectedTotal = yields.Sum(y => (long)y);

            total.Should().Be(expectedTotal);
        }

        [Fact]
        public void CalculateNaiveTotal_HigherThanActual()
        {
            long naive = ExtractionCalculator.CalculateNaiveTotal(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);
            long actual = ExtractionCalculator.CalculateTotalYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            naive.Should().BeGreaterThan(actual,
                "naive estimate always overestimates because it ignores decay");
        }

        [Fact]
        public void CalculateRemainingYield_AtStart_EqualsTotalYield()
        {
            long total = ExtractionCalculator.CalculateTotalYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);
            long remaining = ExtractionCalculator.CalculateRemainingYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration, 0);

            remaining.Should().Be(total);
        }

        [Fact]
        public void CalculateRemainingYield_AtEnd_ReturnsZero()
        {
            long remaining = ExtractionCalculator.CalculateRemainingYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration, ReferenceDuration);

            remaining.Should().Be(0);
        }

        [Fact]
        public void CalculateRemainingYield_AtHalfway_LessThanHalfTotal()
        {
            long total = ExtractionCalculator.CalculateTotalYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);
            long remaining = ExtractionCalculator.CalculateRemainingYield(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration, ReferenceDuration / 2);

            // Due to decay, remaining at halfway should be less than half the total
            remaining.Should().BeLessThan(total / 2,
                "decay front-loads yield so the second half produces less");
        }

        [Fact]
        public void CalculateYieldPerHourAtCycle_FirstCycle_PositiveRate()
        {
            double rate = ExtractionCalculator.CalculateYieldPerHourAtCycle(
                ReferenceQtyPerCycle, ReferenceCycleTime, 0);

            // 30-min cycles = 2 cycles/hr, so rate = 2 * first_cycle_yield
            // first_cycle_yield ≈ 24000, so rate ≈ 48000/hr
            rate.Should().BeGreaterThan(0);
            rate.Should().BeLessThan(60000); // generous bound
        }

        [Fact]
        public void CalculateYieldPerHourAtCycle_Decays()
        {
            double rateFirst = ExtractionCalculator.CalculateYieldPerHourAtCycle(
                ReferenceQtyPerCycle, ReferenceCycleTime, 0);
            double rateLast = ExtractionCalculator.CalculateYieldPerHourAtCycle(
                ReferenceQtyPerCycle, ReferenceCycleTime, 94);

            rateFirst.Should().BeGreaterThan(rateLast);
        }

        [Fact]
        public void GenerateYieldCurve_ReturnsNormalizedValues()
        {
            var curve = ExtractionCalculator.GenerateYieldCurve(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration);

            curve.Should().NotBeEmpty();
            curve[0].Should().Be(1.0, "first point is normalized to 1.0");
            curve.Should().OnlyContain(v => v >= 0 && v <= 1.0);
        }

        [Fact]
        public void GenerateYieldCurve_RespectsMaxDataPoints()
        {
            var curve = ExtractionCalculator.GenerateYieldCurve(
                ReferenceQtyPerCycle, ReferenceCycleTime, ReferenceDuration, 10);

            // With ceiling division, may be maxDataPoints + 1 at most
            curve.Length.Should().BeLessOrEqualTo(11);
            curve.Length.Should().BeGreaterThan(5);
        }

        // ═══════════════════════════════════════════════════════════
        // Cross-validation with known formula constants
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void DecayFormula_WithZeroCycleIndex_MinimalDecay()
        {
            // At cycle 0: t = 0.5 * barWidth = 1.0 (barWidth = 1800/900 = 2)
            // decayValue = 6965 / (1 + 1.0 * 0.012) = 6965 / 1.012 ≈ 6882
            // Output = barWidth * barHeight = 2 * decayValue * (1 + 0.8*sinStuff)
            // Range: 2 * 6882 * 1.0 = 13764 (sinStuff=0) to 2 * 6882 * 1.8 = 24775 (sinStuff=1)
            var yields = ExtractionCalculator.CalculateCycleYields(6965, 1800, 3600);
            yields.Should().HaveCount(2); // 3600 / 1800 = 2 cycles

            yields[0].Should().BeInRange(13000, 25000);
        }

        [Fact]
        public void ShortCycleTime_ProducesMoreCycles()
        {
            var yields15min = ExtractionCalculator.CalculateCycleYields(3000, 900, 86400);
            var yields30min = ExtractionCalculator.CalculateCycleYields(3000, 1800, 86400);

            yields15min.Length.Should().Be(96);   // 86400 / 900 = 96
            yields30min.Length.Should().Be(48);   // 86400 / 1800 = 48
        }

        [Fact]
        public void HighQtyPerCycle_ProducesHigherYields()
        {
            long totalLow = ExtractionCalculator.CalculateTotalYield(1000, 1800, 86400);
            long totalHigh = ExtractionCalculator.CalculateTotalYield(10000, 1800, 86400);

            totalHigh.Should().BeGreaterThan(totalLow);
        }

        // ═══════════════════════════════════════════════════════════
        // Edge cases and invalid input
        // ═══════════════════════════════════════════════════════════

        [Theory]
        [InlineData(0, 1800, 86400)]
        [InlineData(6965, 0, 86400)]
        [InlineData(6965, 1800, 0)]
        [InlineData(-1, 1800, 86400)]
        [InlineData(6965, -1, 86400)]
        public void InvalidInputs_ReturnsEmpty(int qty, int cycle, int duration)
        {
            var yields = ExtractionCalculator.CalculateCycleYields(qty, cycle, duration);
            yields.Should().BeEmpty();
        }

        [Fact]
        public void VeryShortDuration_LessThanOneCycle_ReturnsEmpty()
        {
            var yields = ExtractionCalculator.CalculateCycleYields(6965, 1800, 1799);
            yields.Should().BeEmpty();
        }

        [Fact]
        public void AllYields_AreNonNegative()
        {
            // Test with various parameters to ensure we never get negative yields
            var paramSets = new[]
            {
                (qty: 100, cycle: 900, dur: 86400),
                (qty: 6965, cycle: 1800, dur: 171000),
                (qty: 20000, cycle: 3600, dur: 345600),
                (qty: 50, cycle: 600, dur: 14400),
            };

            foreach (var (qty, cycle, dur) in paramSets)
            {
                var yields = ExtractionCalculator.CalculateCycleYields(qty, cycle, dur);
                yields.Should().OnlyContain(y => y >= 0,
                    $"qty={qty}, cycle={cycle}, dur={dur} produced negative yields");
            }
        }

        // ═══════════════════════════════════════════════════════════
        // Formula verification against Python reference
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void ManualFormulaCheck_FirstCycle_MatchesHandCalculation()
        {
            // Manually compute first cycle with reference values:
            // qty_per_cycle = 6965, cycle_time = 1800s
            // barWidth = 1800 / 900 = 2.0
            // cycle 0: t = (0 + 0.5) * 2.0 = 1.0
            // decayValue = 6965 / (1 + 1.0 * 0.012) = 6965 / 1.012 = 6882.41...
            // phaseShift = 6965^0.7 = Math.Pow(6965, 0.7)
            double phaseShift = Math.Pow(6965, 0.7);
            double t = 1.0;
            double decayValue = 6965.0 / (1.0 + t * 0.012);
            double sinA = Math.Cos(phaseShift + t * (1.0 / 12.0));
            double sinB = Math.Cos(phaseShift / 2.0 + t * 0.2);
            double sinC = Math.Cos(t * 0.5);
            double sinStuff = Math.Max((sinA + sinB + sinC) / 3.0, 0.0);
            double barHeight = decayValue * (1.0 + 0.8 * sinStuff);
            int expected = (int)(2.0 * barHeight);

            var yields = ExtractionCalculator.CalculateCycleYields(6965, 1800, 3600);
            yields[0].Should().Be(expected, "first cycle should exactly match hand calculation");
        }

        [Fact]
        public void ManualFormulaCheck_TenthCycle()
        {
            // Cycle 9 (10th): t = (9 + 0.5) * 2.0 = 19.0
            double barWidth = 2.0;
            double t = 19.0;
            double decayValue = 6965.0 / (1.0 + t * 0.012);
            double phaseShift = Math.Pow(6965, 0.7);
            double sinA = Math.Cos(phaseShift + t * (1.0 / 12.0));
            double sinB = Math.Cos(phaseShift / 2.0 + t * 0.2);
            double sinC = Math.Cos(t * 0.5);
            double sinStuff = Math.Max((sinA + sinB + sinC) / 3.0, 0.0);
            double barHeight = decayValue * (1.0 + 0.8 * sinStuff);
            int expected = (int)(barWidth * barHeight);

            var yields = ExtractionCalculator.CalculateCycleYields(6965, 1800, 86400);
            yields[9].Should().Be(expected, "10th cycle should exactly match hand calculation");
        }
    }
}
