// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Calculates accurate extraction yields using the official EVE Online decay+noise formula.
    /// The ESI qty_per_cycle value is NOT the actual per-cycle output — real extraction follows
    /// a decay curve with sinusoidal noise that front-loads yield and tapers over time.
    /// </summary>
    public static class ExtractionCalculator
    {
        // Dogma attribute 1683 — controls how fast yield decays over time
        private const double DecayFactor = 0.012;

        // Dogma attribute 1687 — amplitude of the sinusoidal noise overlay
        private const double NoiseFactor = 0.8;

        /// <summary>
        /// Calculates the yield for each individual cycle of an extraction program.
        /// This implements the official EVE formula with exponential decay and
        /// triple-cosine noise modulation.
        /// </summary>
        /// <param name="qtyPerCycle">Base quantity per cycle from ESI (extractor_details.qty_per_cycle)</param>
        /// <param name="cycleTimeSeconds">Cycle duration in seconds from ESI (extractor_details.cycle_time)</param>
        /// <param name="totalDurationSeconds">Total program duration in seconds (expiry_time - install_time)</param>
        /// <returns>Array of per-cycle yields (floored integers), index 0 = first cycle</returns>
        public static int[] CalculateCycleYields(int qtyPerCycle, int cycleTimeSeconds, int totalDurationSeconds)
        {
            if (qtyPerCycle <= 0 || cycleTimeSeconds <= 0 || totalDurationSeconds <= 0)
                return Array.Empty<int>();

            int numCycles = totalDurationSeconds / cycleTimeSeconds;
            if (numCycles <= 0)
                return Array.Empty<int>();

            double barWidth = cycleTimeSeconds / 900.0;
            var values = new int[numCycles];

            for (int i = 0; i < numCycles; i++)
            {
                values[i] = CalculateSingleCycleYield(qtyPerCycle, barWidth, i);
            }

            return values;
        }

        /// <summary>
        /// Calculates the total yield across the entire extraction program.
        /// Significantly lower than naive qty_per_cycle * numCycles (typically 30-50% less).
        /// </summary>
        public static long CalculateTotalYield(int qtyPerCycle, int cycleTimeSeconds, int totalDurationSeconds)
        {
            var yields = CalculateCycleYields(qtyPerCycle, cycleTimeSeconds, totalDurationSeconds);
            long total = 0;
            for (int i = 0; i < yields.Length; i++)
                total += yields[i];
            return total;
        }

        /// <summary>
        /// Calculates the remaining yield from a given point in the extraction program.
        /// Used to show "what you'll still get if you don't restart".
        /// </summary>
        /// <param name="qtyPerCycle">Base quantity per cycle from ESI</param>
        /// <param name="cycleTimeSeconds">Cycle duration in seconds</param>
        /// <param name="totalDurationSeconds">Total program duration</param>
        /// <param name="elapsedSeconds">How many seconds have already elapsed since install_time</param>
        /// <returns>Sum of yields for remaining cycles</returns>
        public static long CalculateRemainingYield(int qtyPerCycle, int cycleTimeSeconds, int totalDurationSeconds, int elapsedSeconds)
        {
            if (qtyPerCycle <= 0 || cycleTimeSeconds <= 0 || totalDurationSeconds <= 0)
                return 0;

            int numCycles = totalDurationSeconds / cycleTimeSeconds;
            int completedCycles = Math.Min(elapsedSeconds / cycleTimeSeconds, numCycles);

            if (completedCycles >= numCycles)
                return 0;

            double barWidth = cycleTimeSeconds / 900.0;
            long remaining = 0;

            for (int i = completedCycles; i < numCycles; i++)
            {
                remaining += CalculateSingleCycleYield(qtyPerCycle, barWidth, i);
            }

            return remaining;
        }

        /// <summary>
        /// Calculates the average yield per hour at a specific point in the program.
        /// Useful for showing current extraction rate vs. initial rate.
        /// </summary>
        /// <param name="qtyPerCycle">Base quantity per cycle from ESI</param>
        /// <param name="cycleTimeSeconds">Cycle duration in seconds</param>
        /// <param name="cycleIndex">Which cycle to calculate rate for (0-based)</param>
        /// <returns>Units per hour at that cycle</returns>
        public static double CalculateYieldPerHourAtCycle(int qtyPerCycle, int cycleTimeSeconds, int cycleIndex)
        {
            if (qtyPerCycle <= 0 || cycleTimeSeconds <= 0 || cycleIndex < 0)
                return 0;

            double barWidth = cycleTimeSeconds / 900.0;
            int cycleYield = CalculateSingleCycleYield(qtyPerCycle, barWidth, cycleIndex);
            double cyclesPerHour = 3600.0 / cycleTimeSeconds;

            return cycleYield * cyclesPerHour;
        }

        /// <summary>
        /// Generates data points for rendering the yield decay curve.
        /// Returns normalized values (0.0 to 1.0) relative to the first cycle's yield,
        /// suitable for sparkline or chart rendering.
        /// </summary>
        /// <param name="qtyPerCycle">Base quantity per cycle from ESI</param>
        /// <param name="cycleTimeSeconds">Cycle duration in seconds</param>
        /// <param name="totalDurationSeconds">Total program duration</param>
        /// <param name="maxDataPoints">Maximum number of data points (for UI performance)</param>
        /// <returns>Array of normalized yield values (0.0 to 1.0)</returns>
        public static double[] GenerateYieldCurve(int qtyPerCycle, int cycleTimeSeconds, int totalDurationSeconds, int maxDataPoints = 48)
        {
            var yields = CalculateCycleYields(qtyPerCycle, cycleTimeSeconds, totalDurationSeconds);
            if (yields.Length == 0)
                return Array.Empty<double>();

            int step = Math.Max(1, yields.Length / maxDataPoints);
            int pointCount = (yields.Length + step - 1) / step;
            var curve = new double[pointCount];

            double maxYield = yields[0];
            if (maxYield <= 0)
                return Array.Empty<double>();

            for (int i = 0; i < pointCount; i++)
            {
                int idx = i * step;
                if (idx < yields.Length)
                    curve[i] = yields[idx] / maxYield;
            }

            return curve;
        }

        /// <summary>
        /// Calculates the naive (incorrect) total yield assuming no decay and maximum noise.
        /// This represents what you'd get if the first cycle's rate held steady forever.
        /// Useful for showing users the difference: "Actual yield: X vs. Peak estimate: Y"
        /// </summary>
        public static long CalculateNaiveTotal(int qtyPerCycle, int cycleTimeSeconds, int totalDurationSeconds)
        {
            if (qtyPerCycle <= 0 || cycleTimeSeconds <= 0 || totalDurationSeconds <= 0)
                return 0;

            int numCycles = totalDurationSeconds / cycleTimeSeconds;
            if (numCycles <= 0) return 0;

            // The first cycle yield represents the peak output rate
            double barWidth = cycleTimeSeconds / 900.0;
            int firstCycleYield = CalculateSingleCycleYield(qtyPerCycle, barWidth, 0);
            return (long)firstCycleYield * numCycles;
        }

        /// <summary>
        /// Core formula implementation for a single cycle.
        /// Matches the official EVE client calculation exactly.
        /// </summary>
        private static int CalculateSingleCycleYield(int qtyPerCycle, double barWidth, int cycleIndex)
        {
            double t = (cycleIndex + 0.5) * barWidth;
            double decayValue = qtyPerCycle / (1.0 + t * DecayFactor);
            double phaseShift = Math.Pow(qtyPerCycle, 0.7);

            double sinA = Math.Cos(phaseShift + t * (1.0 / 12.0));
            double sinB = Math.Cos(phaseShift / 2.0 + t * 0.2);
            double sinC = Math.Cos(t * 0.5);

            double sinStuff = Math.Max((sinA + sinB + sinC) / 3.0, 0.0);
            double barHeight = decayValue * (1.0 + NoiseFactor * sinStuff);

            return (int)(barWidth * barHeight);
        }
    }
}
