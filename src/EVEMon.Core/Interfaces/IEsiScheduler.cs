// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Drives all ESI data fetching with priority-based scheduling, per-character rate limiting,
    /// and CCP-compliant cache expiry. Ground-up replacement for the former SmartQueryScheduler.
    /// </summary>
    /// <remarks>
    /// Key behaviors:
    /// <list type="bullet">
    ///   <item><b>Priority queue:</b> Jobs are scheduled by cache expiry time, not fixed intervals.</item>
    ///   <item><b>Per-character rate limiting:</b> TokenTracker enforces ESI's per-(appId, charId) budgets.</item>
    ///   <item><b>Phased cold start:</b> ColdStartPlanner prevents thundering herd on startup.</item>
    ///   <item><b>Tab-switch awareness:</b> SetVisibleCharacter promotes endpoints immediately.</item>
    ///   <item><b>Auth failure isolation:</b> 401/403 suspends only the affected character.</item>
    ///   <item><b>Session persistence:</b> PersistState/RestoreState enable warm restarts with ETags.</item>
    /// </list>
    ///
    /// Production: <c>EsiScheduler</c> in <c>EVEMon.Infrastructure/Scheduling/EsiScheduler.cs</c>.
    /// Testing: Construct with a synchronous <c>IDispatcher</c> stub and mock dependencies.
    /// </remarks>
    public interface IEsiScheduler : IDisposable
    {
        /// <summary>
        /// Registers a character's ESI endpoints for scheduled fetching.
        /// Creates FetchJobs for each endpoint and enqueues them via ColdStartPlanner.
        /// No-op if the character is already registered.
        /// </summary>
        void RegisterCharacter(long characterId, IReadOnlyList<EndpointRegistration> endpoints);

        /// <summary>
        /// Unregisters a character, purging all pending and in-flight jobs.
        /// Uses generation stamps to invalidate stale jobs without lock contention.
        /// </summary>
        void UnregisterCharacter(long characterId);

        /// <summary>
        /// Sets the currently visible character tab. Promotes that character's endpoints
        /// to Active priority and wakes the dispatch loop for immediate processing.
        /// </summary>
        void SetVisibleCharacter(long characterId);

        /// <summary>
        /// Forces an immediate refresh of a specific endpoint (or all endpoints if method is -1)
        /// for the given character. Enqueues at DateTime.UtcNow with bumped generation.
        /// </summary>
        void ForceRefresh(long characterId, long endpointMethod = -1);

        /// <summary>
        /// Called when a character re-authenticates (new OAuth tokens). Clears the AuthFailed
        /// state and re-enqueues critical endpoints.
        /// </summary>
        void OnCharacterReAuthenticated(long characterId);

        /// <summary>
        /// Saves all current FetchJob state (CachedUntil, ETags) for warm restart.
        /// Called during application shutdown after Dispose (dispatch loop stopped).
        /// </summary>
        void PersistState();

        /// <summary>
        /// Restores persisted state for a character's endpoints, enabling warm starts
        /// with conditional GETs (ETags) and correct cache timing.
        /// </summary>
        void RestoreState(long characterId, IReadOnlyList<CachedEndpointState> states);

        /// <summary>
        /// Gets the number of jobs currently in the priority queue.
        /// </summary>
        int QueueDepth { get; }

        /// <summary>
        /// Gets the number of HTTP requests currently in flight.
        /// </summary>
        int ActiveFetches { get; }
    }

    /// <summary>
    /// Outcome of a single ESI fetch operation. Returned by <see cref="EndpointRegistration.ExecuteAsync"/>
    /// to the scheduler for re-scheduling, rate limit tracking, and auth state management.
    /// </summary>
    public readonly struct FetchOutcome
    {
        /// <summary>HTTP status code (200, 304, 401, 403, 429, 5xx) or 0 if skipped.</summary>
        public int StatusCode { get; init; }

        /// <summary>When the cache expires — the scheduler re-enqueues at this time + jitter.</summary>
        public DateTime CachedUntil { get; init; }

        /// <summary>HTTP ETag for conditional GET on next fetch (304 Not Modified).</summary>
        public string? ETag { get; init; }

        /// <summary>Remaining rate limit tokens from ESI response headers.</summary>
        public int? RateLimitRemaining { get; init; }

        /// <summary>Retry-After seconds for 429 responses.</summary>
        public int? RetryAfterSeconds { get; init; }
    }

    /// <summary>
    /// Describes a single ESI endpoint to be scheduled for a character.
    /// The <see cref="ExecuteAsync"/> delegate encapsulates the full fetch cycle:
    /// HTTP call, result parsing, callback invocation, and error handling.
    /// </summary>
    public readonly struct EndpointRegistration
    {
        /// <summary>ESIAPICharacterMethods enum value identifying the endpoint.</summary>
        public long Method { get; init; }

        /// <summary>
        /// Async delegate that performs the full ESI fetch cycle for this endpoint.
        /// Takes the current ETag (null for first fetch) and returns the fetch outcome.
        /// The delegate is created by the orchestrator and encapsulates:
        /// - Finding the ESI key with required scope
        /// - Calling APIProvider.QueryEsiAsync with the correct typed parameters
        /// - Invoking the success/error callback on the UI thread
        /// - Returning cache metadata (CachedUntil, ETag, status code) for the scheduler
        /// </summary>
        public Func<string?, Task<FetchOutcome>> ExecuteAsync { get; init; }

        /// <summary>Required ESI scope bitmask. Endpoint is skipped if character lacks scope.</summary>
        public ulong RequiredScope { get; init; }

        /// <summary>Rate limit group for per-character budgeting (null = default group).</summary>
        public string? RateGroup { get; init; }
    }

    /// <summary>
    /// Persisted state for a single endpoint, enabling warm restarts.
    /// </summary>
    public readonly struct CachedEndpointState
    {
        /// <summary>ESIAPICharacterMethods enum value.</summary>
        public long Method { get; init; }

        /// <summary>When data was last successfully fetched.</summary>
        public DateTime LastUpdate { get; init; }

        /// <summary>HTTP ETag for conditional GET (304 Not Modified).</summary>
        public string? ETag { get; init; }

        /// <summary>When the cached data expires (next fetch should occur after this).</summary>
        public DateTime? CachedUntil { get; init; }
    }
}
