// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Constants;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Represents an active cerebral accelerator (booster) providing attribute bonuses.
    /// Cerebral accelerators give a uniform bonus to ALL attributes.
    /// </summary>
    public sealed class BoosterInfo
    {
        /// <summary>
        /// Gets or sets the bonus amount applied to all attributes.
        /// Common values: +2 (Basic), +6 (Standard), +10 (Advanced), +12 (Expert).
        /// </summary>
        public int Bonus { get; set; }

        /// <summary>
        /// Gets or sets the timestamp when the booster was detected.
        /// </summary>
        public DateTime DetectedTime { get; set; }

        /// <summary>
        /// Gets or sets the estimated time when the booster will expire.
        /// Calculated based on Biology skill level when detected.
        /// </summary>
        public DateTime EstimatedExpiry { get; set; }

        /// <summary>
        /// Gets or sets the Biology skill level at detection time (0-5).
        /// Used for duration calculation: 20% bonus per level.
        /// </summary>
        public int BiologyLevel { get; set; }

        /// <summary>
        /// Gets or sets whether this booster was manually confirmed by the user.
        /// Auto-detected boosters may need user confirmation for accuracy.
        /// </summary>
        public bool IsUserConfirmed { get; set; }

        /// <summary>
        /// Gets the estimated remaining duration.
        /// </summary>
        public TimeSpan EstimatedRemainingDuration
        {
            get
            {
                var remaining = EstimatedExpiry - DateTime.UtcNow;
                return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
            }
        }

        /// <summary>
        /// Gets whether the booster is likely still active.
        /// </summary>
        public bool IsActive => DateTime.UtcNow < EstimatedExpiry;

        /// <summary>
        /// Creates a new BoosterInfo with estimated duration based on Biology skill.
        /// </summary>
        /// <param name="bonus">The attribute bonus amount.</param>
        /// <param name="biologyLevel">The Biology skill level (0-5).</param>
        /// <returns>A new BoosterInfo instance.</returns>
        public static BoosterInfo Create(int bonus, int biologyLevel)
        {
            var now = DateTime.UtcNow;
            var baseDuration = TimeSpan.FromHours(EveConstants.BaseBoosterDurationHours);

            // Biology skill increases booster duration by 20% per level
            var durationMultiplier = 1.0 + (biologyLevel * 0.2);
            var totalDuration = TimeSpan.FromTicks((long)(baseDuration.Ticks * durationMultiplier));

            return new BoosterInfo
            {
                Bonus = bonus,
                DetectedTime = now,
                EstimatedExpiry = now.Add(totalDuration),
                BiologyLevel = biologyLevel,
                IsUserConfirmed = false
            };
        }

        /// <summary>
        /// Gets the total booster duration based on Biology skill level.
        /// </summary>
        /// <param name="biologyLevel">The Biology skill level (0-5).</param>
        /// <returns>Total duration including Biology bonus.</returns>
        public static TimeSpan GetTotalDuration(int biologyLevel)
        {
            var baseDuration = TimeSpan.FromHours(EveConstants.BaseBoosterDurationHours);
            var durationMultiplier = 1.0 + (biologyLevel * 0.2);
            return TimeSpan.FromTicks((long)(baseDuration.Ticks * durationMultiplier));
        }

        /// <summary>
        /// Updates the expiry time based on a new Biology skill level.
        /// Call this when the Biology skill level changes.
        /// </summary>
        /// <param name="newBiologyLevel">The new Biology skill level.</param>
        public void UpdateForBiologyLevel(int newBiologyLevel)
        {
            if (newBiologyLevel == BiologyLevel)
                return;

            // Calculate elapsed time since detection
            var elapsed = DateTime.UtcNow - DetectedTime;

            // Calculate new total duration
            var newTotalDuration = GetTotalDuration(newBiologyLevel);

            // New expiry = detection time + new total duration
            EstimatedExpiry = DetectedTime.Add(newTotalDuration);
            BiologyLevel = newBiologyLevel;
        }

        /// <summary>
        /// Returns a string representation of the booster.
        /// </summary>
        public override string ToString()
        {
            var remaining = EstimatedRemainingDuration;
            if (remaining == TimeSpan.Zero)
                return $"+{Bonus} Booster (Expired)";

            if (remaining.TotalHours >= 1)
                return $"+{Bonus} Booster ({remaining.Hours}h {remaining.Minutes}m remaining)";

            return $"+{Bonus} Booster ({remaining.Minutes}m remaining)";
        }
    }
}
