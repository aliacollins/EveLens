using System;
using System.Collections.Concurrent;

namespace EVEMon.Common.Scheduling
{
    /// <summary>
    /// Tracks per-(characterId, rateGroup) rate limit budgets using ESI response headers.
    /// Thread-safe via ConcurrentDictionary.
    /// </summary>
    internal sealed class TokenTracker
    {
        private readonly ConcurrentDictionary<(long CharId, string Group), TokenBucket> _buckets = new();
        private static readonly string DefaultGroup = "default";

        /// <summary>
        /// Whether a fetch is allowed for the given character and rate group.
        /// Returns true for unknown characters (optimistic default).
        /// </summary>
        public bool CanFetch(long charId, string? rateGroup)
        {
            var key = (charId, rateGroup ?? DefaultGroup);
            if (!_buckets.TryGetValue(key, out var bucket))
                return true; // Unknown = allow

            bucket.CheckRefill(DateTime.UtcNow);
            return bucket.HasBudget;
        }

        /// <summary>
        /// Gets the next time tokens will refill for this character/group.
        /// Returns DateTime.UtcNow if no bucket exists.
        /// </summary>
        public DateTime NextRefillTime(long charId, string? rateGroup)
        {
            var key = (charId, rateGroup ?? DefaultGroup);
            if (!_buckets.TryGetValue(key, out var bucket))
                return DateTime.UtcNow;

            return bucket.NextRefillTime;
        }

        /// <summary>
        /// Updates rate limit state from ESI response headers.
        /// </summary>
        public void Update(long charId, string? rateGroup, int? remaining, int? limit)
        {
            var key = (charId, rateGroup ?? DefaultGroup);
            var bucket = _buckets.GetOrAdd(key, _ => new TokenBucket());
            bucket.Update(remaining, limit, DateTime.UtcNow);
        }

        /// <summary>
        /// Removes all buckets for a character (on unregister).
        /// </summary>
        public void RemoveCharacter(long charId)
        {
            foreach (var key in _buckets.Keys)
            {
                if (key.CharId == charId)
                    _buckets.TryRemove(key, out _);
            }
        }
    }
}
