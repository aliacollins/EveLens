// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Infrastructure.Scheduling.Health
{
    /// <summary>
    /// Health state for a single ESI endpoint. Transitions are driven by
    /// <see cref="EndpointHealthTracker"/> based on fetch outcome rates
    /// within a dynamic time window.
    /// </summary>
    public enum EndpointHealth
    {
        /// <summary>Fetching normally, data is current.</summary>
        Healthy = 0,

        /// <summary>Some recent failures but still getting data intermittently.</summary>
        Degraded = 1,

        /// <summary>Persistent failures, no data coming through.</summary>
        Failing = 2,

        /// <summary>Auth expired (401/403) — requires user re-authentication.</summary>
        Suspended = 3
    }

    /// <summary>
    /// Aggregate health for all endpoints belonging to a single character.
    /// Computed by <see cref="EndpointHealthTracker.GetCharacterHealth"/>.
    /// </summary>
    public enum CharacterHealth
    {
        /// <summary>All endpoints healthy.</summary>
        Healthy = 0,

        /// <summary>Some endpoints degraded but none failing or suspended.</summary>
        Degraded = 1,

        /// <summary>One or more endpoints persistently failing.</summary>
        Failing = 2,

        /// <summary>Auth expired — all endpoints suspended until re-authentication.</summary>
        Suspended = 3
    }

    /// <summary>
    /// Aggregate health summary for all endpoints belonging to a single character.
    /// </summary>
    public readonly struct CharacterHealthSummary
    {
        public CharacterHealth OverallHealth { get; init; }
        public int HealthyCount { get; init; }
        public int DegradedCount { get; init; }
        public int FailingCount { get; init; }

        /// <summary>
        /// When the first endpoint entered <see cref="EndpointHealth.Failing"/> state,
        /// or null if no endpoints are failing.
        /// </summary>
        public DateTime? FailingSince { get; init; }
    }

    /// <summary>
    /// A single fetch outcome recorded by the health tracker.
    /// Stored in a ring buffer per (character, endpoint) pair.
    /// </summary>
    public readonly struct FetchRecord
    {
        public DateTime Timestamp { get; init; }
        public int StatusCode { get; init; }

        /// <summary>
        /// Cache duration from the ESI response (Expires header minus now).
        /// Used to self-tune the health tracker's rolling window.
        /// </summary>
        public TimeSpan CacheDuration { get; init; }
    }
}
