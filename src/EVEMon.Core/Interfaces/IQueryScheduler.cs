using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Drives character data polling with adaptive intervals, priority scheduling for the
    /// visible character, and round-robin background processing for all other characters.
    /// Replaces the old <c>EveMonClient.UpdateOnOneSecondTick()</c> polling loop.
    /// </summary>
    /// <remarks>
    /// Key behaviors:
    /// <list type="bullet">
    ///   <item><b>Priority scheduling:</b> The character set via <see cref="SetVisibleCharacter"/>
    ///     is processed every tick (~5 seconds).</item>
    ///   <item><b>Background round-robin:</b> One non-visible character is processed per tick,
    ///     cycling through all registered queryables.</item>
    ///   <item><b>Adaptive back-off:</b> Queryables returning consecutive "Not Modified" (304)
    ///     responses have their polling interval doubled (up to 4x) to reduce API waste.</item>
    ///   <item><b>Rate limit awareness:</b> Background processing pauses when
    ///     <c>ActiveRequests / MaxConcurrentRequests > 80%</c>.</item>
    ///   <item><b>Staggered startup:</b> Each queryable gets a randomized delay on registration
    ///     to prevent a thundering herd of API calls when many characters load simultaneously.</item>
    /// </list>
    ///
    /// Implements <see cref="IDisposable"/> to stop the tick timer and clear all registrations.
    ///
    /// Production: <c>SmartQueryScheduler</c> in <c>EVEMon.Infrastructure/Services/SmartQueryScheduler.cs</c>.
    /// Testing: Construct with a synchronous <c>IDispatcher</c> stub and a mock <c>IEsiClient</c>.
    /// The constructor accepts an optional <c>Random</c> parameter for deterministic tests.
    /// </remarks>
    public interface IQueryScheduler : IDisposable
    {
        /// <summary>
        /// Registers a queryable to be driven by the scheduler's tick cycle.
        /// The queryable receives a staggered startup delay to avoid thundering herd.
        /// No-op if the queryable is already registered or the scheduler is disposed.
        /// </summary>
        /// <param name="queryable">The queryable to register.</param>
        void Register(IScheduledQueryable queryable);

        /// <summary>
        /// Unregisters a queryable, stopping its polling. Adjusts the round-robin index
        /// if needed to prevent skipping entries.
        /// </summary>
        /// <param name="queryable">The queryable to unregister.</param>
        void Unregister(IScheduledQueryable queryable);

        /// <summary>
        /// Sets the currently visible character, which receives priority scheduling
        /// (processed every tick instead of round-robin). Pass 0 to clear.
        /// </summary>
        /// <param name="characterId">The EVE character ID of the visible character, or 0 for none.</param>
        void SetVisibleCharacter(long characterId);

        /// <summary>
        /// Gets the number of queryables currently registered with the scheduler.
        /// </summary>
        int RegisteredCount { get; }

        /// <summary>
        /// Gets the base polling interval in milliseconds (currently 5000ms).
        /// Adaptive back-off multiplies this for idle queryables.
        /// </summary>
        int CurrentPollingIntervalMs { get; }

        /// <summary>
        /// Gets the total number of API calls (ProcessTick invocations) made since startup.
        /// </summary>
        long ApiCallsInWindow { get; }

        /// <summary>
        /// Gets whether background (non-visible) polling is paused due to ESI rate-limit pressure.
        /// True when <c>ActiveRequests / MaxConcurrentRequests > 0.8</c>.
        /// </summary>
        bool IsRateLimitPaused { get; }
    }

    /// <summary>
    /// Contract for objects that can be driven by <see cref="IQueryScheduler"/>.
    /// Each queryable represents a single character's ESI polling state.
    /// </summary>
    /// <remarks>
    /// Typically implemented by a per-character query manager adapter that wraps the
    /// character's collection of <c>QueryMonitor</c> instances.
    ///
    /// The <see cref="ConsecutiveNotModifiedCount"/> property drives the scheduler's adaptive
    /// back-off: after 3+ consecutive 304 responses, the polling interval doubles (up to 4x).
    ///
    /// Production: <c>ScheduledQueryableAdapter</c> in
    /// <c>EVEMon.Infrastructure/Services/ScheduledQueryableAdapter.cs</c>.
    /// Testing: Implement with simple auto-properties and a no-op <see cref="ProcessTick"/>.
    /// </remarks>
    public interface IScheduledQueryable
    {
        /// <summary>
        /// Gets the EVE character ID associated with this queryable.
        /// Used by the scheduler to identify which queryable belongs to the visible character.
        /// </summary>
        long CharacterID { get; }

        /// <summary>
        /// Processes a single tick: checks which monitors need updating and fires ESI requests
        /// for any that are due. Called by the scheduler on the UI thread.
        /// </summary>
        void ProcessTick();

        /// <summary>
        /// Gets whether the initial startup queries (character sheet, skill queue, etc.)
        /// have all completed at least once. Used by the UI to hide loading overlays.
        /// </summary>
        bool IsStartupComplete { get; }

        /// <summary>
        /// Gets the number of consecutive API responses that returned HTTP 304 Not Modified.
        /// Resets to 0 when any monitor returns new data. Used by the scheduler's adaptive
        /// back-off algorithm to reduce polling frequency for idle characters.
        /// </summary>
        int ConsecutiveNotModifiedCount { get; }
    }
}
