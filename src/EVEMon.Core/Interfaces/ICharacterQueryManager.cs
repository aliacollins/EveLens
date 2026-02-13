using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Manages query monitors for a single character.
    /// Supports on-demand monitor creation to reduce resource usage at scale.
    /// </summary>
    public interface ICharacterQueryManager : IScheduledQueryable, IDisposable
    {
        /// <summary>
        /// Requests that a specific data type be actively queried.
        /// Creates the monitor lazily on first request.
        /// </summary>
        /// <param name="dataType">The ESI data type enum value to query.</param>
        void RequestDataType(int dataType);

        /// <summary>
        /// Gets the number of active monitors for this character.
        /// </summary>
        int ActiveMonitorCount { get; }

        /// <summary>
        /// Gets whether the character sheet is currently being updated.
        /// </summary>
        bool IsCharacterSheetUpdating { get; }

        /// <summary>
        /// Gets whether a specific query type has completed at least once.
        /// </summary>
        /// <param name="dataType">The ESI data type enum value.</param>
        bool IsQueryComplete(int dataType);
    }
}
