// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Core.Events
{
    /// <summary>
    /// Published when an ESI endpoint's health state transitions (e.g., Healthy → Degraded).
    /// Only fires on actual state changes, never on repeated same-state fetch results.
    /// Uses primitives only (Core has no Infrastructure dependencies).
    /// </summary>
    public sealed class HealthStateChangedEvent
    {
        // State constants matching EndpointHealth enum values.
        // Defined here so Core consumers can interpret states without referencing Infrastructure.
        public const int StateHealthy = 0;
        public const int StateDegraded = 1;
        public const int StateFailing = 2;
        public const int StateSuspended = 3;

        /// <summary>Character whose endpoint health changed.</summary>
        public long CharacterId { get; init; }

        /// <summary>ESIAPICharacterMethods enum value identifying the endpoint.</summary>
        public long EndpointMethod { get; init; }

        /// <summary>Previous health state (one of State* constants).</summary>
        public int OldState { get; init; }

        /// <summary>New health state (one of State* constants).</summary>
        public int NewState { get; init; }

        /// <summary>Count of healthy endpoints for this character after the transition.</summary>
        public int HealthyCount { get; init; }

        /// <summary>Count of degraded endpoints for this character after the transition.</summary>
        public int DegradedCount { get; init; }

        /// <summary>Count of failing endpoints for this character after the transition.</summary>
        public int FailingCount { get; init; }

        /// <summary>
        /// When the first endpoint entered Failing state for this character,
        /// or null if no endpoints are currently failing.
        /// </summary>
        public DateTime? FailingSince { get; init; }
    }
}
