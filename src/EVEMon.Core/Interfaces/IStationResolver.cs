using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves station/structure IDs to station information objects.
    /// Breaks the Model to <c>EveIDToStation</c> static service dependency
    /// (12 call sites across 10 files).
    /// </summary>
    /// <remarks>
    /// Handles both NPC stations (resolved from static data) and player-owned citadels
    /// (resolved via ESI with an authenticated character token). For citadels, the sync
    /// method <see cref="GetStation"/> may return an "Inaccessible Structure" placeholder
    /// while the async ESI lookup is in progress; subsequent calls return the real data
    /// once resolved.
    ///
    /// Return type is <c>object?</c> because the Core assembly cannot reference the
    /// <c>Station</c> class in <c>EVEMon.Common.Data</c>. Consumers that need typed access
    /// should cast to <c>Station</c> or use <see cref="IStation"/>.
    ///
    /// The optional <paramref name="characterId"/> parameter on both methods is used to
    /// select the best ESI token for citadel lookups: the character who docked there
    /// typically has access rights.
    ///
    /// Production: <c>StationResolverAdapter</c> in <c>EVEMon.Common/Services/StationResolverAdapter.cs</c>
    /// (delegates to static <c>EveIDToStation</c>).
    /// Testing: Provide a stub returning mock station objects by ID.
    /// </remarks>
    public interface IStationResolver
    {
        /// <summary>
        /// Synchronously gets station information by ID. For NPC stations, returns immediately
        /// from static data. For citadels, may return an "Inaccessible Structure" placeholder
        /// while the background ESI lookup runs. Returns null if the ID is completely unknown.
        /// </summary>
        /// <param name="id">The station or structure ID.</param>
        /// <param name="characterId">Optional EVE character ID to prioritize for ESI token selection.</param>
        /// <returns>A <c>Station</c> object (as <c>object</c>), or null if not found.</returns>
        object? GetStation(long id, long characterId = 0);

        /// <summary>
        /// Asynchronously gets station information by ID, waiting for citadel ESI lookups
        /// to complete rather than returning a placeholder. Returns null if not found.
        /// </summary>
        /// <param name="id">The station or structure ID.</param>
        /// <param name="characterId">Optional EVE character ID to prioritize for ESI token selection.</param>
        /// <returns>A <c>Station</c> object (as <c>object</c>), or null if not found.</returns>
        Task<object?> GetStationAsync(long id, long characterId = 0);
    }
}
