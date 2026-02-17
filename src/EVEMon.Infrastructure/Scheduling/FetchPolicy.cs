using System;
using EVEMon.Core.Enumerations;

namespace EVEMon.Common.Scheduling
{
    /// <summary>
    /// Static policy configuration for ESI endpoints: priority classification,
    /// jitter calculation, and cold-start phase assignment.
    /// </summary>
    internal static class FetchPolicy
    {
        // Cold start phases: lower = earlier
        // Phase 1: Character identity (CharSheet, Skills, SkillQueue) — visible char only
        // Phase 2: Skills+Queue for all characters (staggered)
        // Phase 3: Alert endpoints (Mail, Orders, Contracts, Jobs, Notifications, PI)
        // Phase 4: Everything else

        private static readonly Random s_defaultRandom = new();

        /// <summary>
        /// Gets the fetch priority for an endpoint based on character visibility.
        /// </summary>
        public static FetchPriority GetPriority(bool isVisible, bool isMonitored)
        {
            if (!isMonitored)
                return FetchPriority.Off;
            return isVisible ? FetchPriority.Active : FetchPriority.Background;
        }

        /// <summary>
        /// Gets the jitter to add after cache expiry before scheduling the next fetch.
        /// Active characters get minimal jitter; background gets more spread.
        /// </summary>
        public static TimeSpan GetJitter(FetchPriority priority) => GetJitter(priority, s_defaultRandom);

        /// <summary>
        /// Gets the jitter with an explicit random source (for deterministic testing).
        /// </summary>
        internal static TimeSpan GetJitter(FetchPriority priority, Random random)
        {
            return priority switch
            {
                FetchPriority.Active => TimeSpan.FromMilliseconds(random.Next(100, 500)),
                FetchPriority.Background => TimeSpan.FromMilliseconds(random.Next(500, 3000)),
                FetchPriority.Dormant => TimeSpan.FromMilliseconds(random.Next(3000, 10000)),
                _ => TimeSpan.Zero
            };
        }

        /// <summary>
        /// Gets the cold-start phase (1-4) for an endpoint method.
        /// Phase 1 endpoints are fetched first on startup.
        /// </summary>
        /// <param name="method">ESIAPICharacterMethods int value.</param>
        public static int GetColdStartPhase(long method)
        {
            // Phase 1: Core identity — CharacterSheet(0), Skills(1), SkillQueue(2)
            if (method <= 2)
                return 1;

            // Phase 2: Implants(3), Attributes(4), Location(7?), Ship, Clones — supplement identity
            if (method is 3 or 4)
                return 2;

            // Phase 3: Alert endpoints — MarketOrders, Contracts, IndustryJobs, Mail, Notifications, PI
            // These are mapped by ESIAPICharacterMethods enum values
            if (method is >= 14 and <= 19 or 21 or 23)
                return 3;

            // Phase 4: Everything else
            return 4;
        }

        /// <summary>
        /// Gets the base delay offset for a cold-start phase.
        /// </summary>
        public static TimeSpan GetColdStartDelay(int phase, int characterIndex)
        {
            return phase switch
            {
                1 => TimeSpan.Zero, // Immediate for visible char
                2 => TimeSpan.FromMilliseconds(20 * characterIndex),
                3 => TimeSpan.FromMilliseconds(200 + 10 * characterIndex),
                _ => TimeSpan.FromMilliseconds(1000 + 50 * characterIndex),
            };
        }
    }
}
