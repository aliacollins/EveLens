using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Manages ESI query monitors for a single character.
    /// Supports lazy, on-demand monitor creation to reduce resource usage when many
    /// characters are loaded but only a few are actively viewed.
    /// Extends <see cref="IScheduledQueryable"/> so it can be registered with
    /// <see cref="IQueryScheduler"/> for adaptive polling.
    /// </summary>
    /// <remarks>
    /// Each <c>CCPCharacter</c> owns one query manager. Monitors are created lazily
    /// via <see cref="RequestDataType"/> the first time a specific data type is needed
    /// (e.g., when the user opens the Assets tab). This avoids creating 15+ monitors
    /// per character on startup for characters whose data is never viewed.
    ///
    /// The <see cref="IsQueryComplete"/> method allows UI code to show loading indicators
    /// until a particular data type has been fetched at least once.
    ///
    /// Implements <see cref="IDisposable"/> to clean up timer subscriptions and HTTP resources.
    ///
    /// Production: Typically implemented within the character model layer.
    /// Testing: Provide a stub that tracks requested data types and returns canned states.
    /// </remarks>
    public interface ICharacterQueryManager : IScheduledQueryable, IDisposable
    {
        /// <summary>
        /// Requests that a specific ESI data type be actively queried for this character.
        /// Creates the corresponding query monitor lazily on first request.
        /// Subsequent calls for the same data type are no-ops.
        /// </summary>
        /// <param name="dataType">The ESI data type enum value (cast from <c>ESIAPICharacterMethods</c>).</param>
        void RequestDataType(int dataType);

        /// <summary>
        /// Gets the number of query monitors currently active (created) for this character.
        /// Useful for diagnostics and monitoring resource usage.
        /// </summary>
        int ActiveMonitorCount { get; }

        /// <summary>
        /// Gets whether the character sheet (basic info) query is currently in progress.
        /// Used by the UI to show a refresh indicator on the character overview.
        /// </summary>
        bool IsCharacterSheetUpdating { get; }

        /// <summary>
        /// Gets whether a specific query type has completed at least one successful fetch.
        /// Returns false if the monitor has not been created or has not finished its first query.
        /// </summary>
        /// <param name="dataType">The ESI data type enum value (cast from <c>ESIAPICharacterMethods</c>).</param>
        /// <returns>True if the query has completed at least once; false otherwise.</returns>
        bool IsQueryComplete(int dataType);
    }
}
