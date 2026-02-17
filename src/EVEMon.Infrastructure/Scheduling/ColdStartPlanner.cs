using System;
using System.Collections.Generic;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Scheduling
{
    /// <summary>
    /// Generates phased startup schedules for character endpoints to prevent
    /// thundering herd on application launch.
    /// </summary>
    internal static class ColdStartPlanner
    {
        /// <summary>
        /// Plans initial fetch schedule for a character's endpoints.
        /// </summary>
        /// <param name="characterId">Character to plan for.</param>
        /// <param name="characterIndex">Character's position in the load order (0-based).</param>
        /// <param name="isVisible">Whether this is the currently visible character.</param>
        /// <param name="registrations">Endpoint registrations to schedule.</param>
        /// <param name="persistedStates">Previously persisted cache/ETag state (may be empty).</param>
        /// <returns>List of (FetchJob, dueTime) pairs for initial enqueue.</returns>
        public static List<(FetchJob Job, DateTime DueTime)> Plan(
            long characterId,
            int characterIndex,
            bool isVisible,
            IReadOnlyList<EndpointRegistration> registrations,
            IReadOnlyList<CachedEndpointState>? persistedStates)
        {
            var now = DateTime.UtcNow;
            var result = new List<(FetchJob, DateTime)>(registrations.Count);

            // Build lookup for persisted states
            var stateMap = new Dictionary<long, CachedEndpointState>();
            if (persistedStates != null)
            {
                foreach (var state in persistedStates)
                    stateMap[state.Method] = state;
            }

            foreach (var reg in registrations)
            {
                int phase = FetchPolicy.GetColdStartPhase(reg.Method);

                // For non-visible characters, phase 1 becomes phase 2
                if (!isVisible && phase == 1)
                    phase = 2;

                var baseDelay = FetchPolicy.GetColdStartDelay(phase, characterIndex);
                var dueTime = now + baseDelay;

                // If we have persisted state with valid cache, use that instead
                if (stateMap.TryGetValue(reg.Method, out var persisted))
                {
                    if (persisted.CachedUntil.HasValue && persisted.CachedUntil.Value > now)
                    {
                        dueTime = persisted.CachedUntil.Value + FetchPolicy.GetJitter(FetchPriority.Background);
                    }
                }

                var job = new FetchJob
                {
                    CharacterId = characterId,
                    EndpointMethod = reg.Method,
                    Generation = 0,
                    Priority = isVisible ? FetchPriority.Active : FetchPriority.Background,
                    RateGroup = reg.RateGroup,
                    ETag = stateMap.TryGetValue(reg.Method, out var s) ? s.ETag : null,
                    ExecuteAsync = reg.ExecuteAsync,
                };

                result.Add((job, dueTime));
            }

            return result;
        }
    }
}
