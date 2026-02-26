// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Scheduling
{
    /// <summary>
    /// Individual rate limit bucket for a (characterId, rateGroup) pair.
    /// Tracks remaining tokens based on ESI X-Ratelimit-* response headers.
    /// </summary>
    internal sealed class TokenBucket
    {
        private const int DefaultLimit = 150;
        private static readonly TimeSpan DefaultWindow = TimeSpan.FromMinutes(15);
        private const int SafetyMargin = 15; // 10% of typical 150

        public int Limit { get; set; } = DefaultLimit;
        public TimeSpan Window { get; set; } = DefaultWindow;
        public int Remaining { get; set; } = DefaultLimit;
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Whether there is budget remaining (above safety margin) for a fetch.
        /// </summary>
        public bool HasBudget => Remaining > SafetyMargin;

        /// <summary>
        /// When the current rate limit window resets and tokens refill.
        /// </summary>
        public DateTime NextRefillTime => LastUpdated + Window;

        /// <summary>
        /// Updates bucket state from response headers.
        /// </summary>
        public void Update(int? remaining, int? limit, DateTime now)
        {
            if (remaining.HasValue)
                Remaining = remaining.Value;
            if (limit.HasValue)
                Limit = limit.Value;
            LastUpdated = now;
        }

        /// <summary>
        /// Checks if the window has rolled over and resets if so.
        /// </summary>
        public void CheckRefill(DateTime now)
        {
            if (now >= NextRefillTime)
            {
                Remaining = Limit;
                LastUpdated = now;
            }
        }
    }
}
