using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves station/structure IDs to station information.
    /// Breaks Model -> EveIDToStation Service dependency (12 call sites, 10 files).
    /// Returns object typed as Station at runtime. Consumers in EVEMon.Models cast to Station.
    /// </summary>
    public interface IStationResolver
    {
        /// <summary>
        /// Gets station information from its ID. Works for NPC stations and citadels.
        /// Returns null if not found. For citadels, may return an "inaccessible" placeholder
        /// while async lookup is in progress.
        /// </summary>
        /// <param name="id">The station/structure ID.</param>
        /// <param name="characterId">Optional character ID to prioritize ESI token selection for citadel lookups.</param>
        /// <returns>The station object, or null if not found.</returns>
        object? GetStation(long id, long characterId = 0);

        /// <summary>
        /// Async version that waits for citadel lookup to complete.
        /// </summary>
        /// <param name="id">The station/structure ID.</param>
        /// <param name="characterId">Optional character ID to prioritize ESI token selection for citadel lookups.</param>
        /// <returns>The station object, or null if not found.</returns>
        Task<object?> GetStationAsync(long id, long characterId = 0);
    }
}
