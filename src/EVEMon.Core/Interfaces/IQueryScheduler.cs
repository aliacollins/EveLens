using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts the query scheduling system that drives character data polling.
    /// Replaces CentralQueryScheduler with adaptive polling and priority scheduling.
    /// </summary>
    public interface IQueryScheduler : IDisposable
    {
        /// <summary>
        /// Registers a queryable to be driven by the scheduler.
        /// </summary>
        void Register(IScheduledQueryable queryable);

        /// <summary>
        /// Unregisters a queryable, stopping its polling.
        /// </summary>
        void Unregister(IScheduledQueryable queryable);

        /// <summary>
        /// Sets the currently visible character, which receives priority scheduling.
        /// </summary>
        void SetVisibleCharacter(long characterId);

        /// <summary>
        /// Gets the number of currently registered queryables.
        /// </summary>
        int RegisteredCount { get; }

        /// <summary>
        /// Gets the current polling interval in milliseconds.
        /// </summary>
        int CurrentPollingIntervalMs { get; }

        /// <summary>
        /// Gets the number of API calls made in the current window.
        /// </summary>
        long ApiCallsInWindow { get; }

        /// <summary>
        /// Gets whether background polling is paused due to rate limiting.
        /// </summary>
        bool IsRateLimitPaused { get; }
    }

    /// <summary>
    /// Interface for objects that can be driven by the query scheduler.
    /// </summary>
    public interface IScheduledQueryable
    {
        /// <summary>
        /// Gets the character ID associated with this queryable.
        /// </summary>
        long CharacterID { get; }

        /// <summary>
        /// Processes a single tick, performing any pending query work.
        /// </summary>
        void ProcessTick();

        /// <summary>
        /// Gets whether the initial startup queries have completed.
        /// </summary>
        bool IsStartupComplete { get; }

        /// <summary>
        /// Gets the number of consecutive API responses that returned Not Modified (304).
        /// </summary>
        int ConsecutiveNotModifiedCount { get; }
    }
}
