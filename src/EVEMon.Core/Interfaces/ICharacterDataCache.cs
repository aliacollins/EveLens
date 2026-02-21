// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Persists live ESI response data per character per endpoint to local disk.
    /// On startup, cached data is loaded instantly so character tabs are populated
    /// before ESI refetch completes. ESI scheduler still runs with ETags — if 304
    /// (unchanged), cache stays valid; if 200 (new data), memory + cache are updated.
    /// </summary>
    /// <remarks>
    /// Production: <c>CharacterDataCacheService</c> in <c>EVEMon.Common/Services/</c>.
    /// Stores one JSON file per endpoint per character at
    /// <c>{AppData}/EVEMon/cache/characters/{characterId}/{endpointKey}.json</c>.
    ///
    /// Testing: Use <c>NSubstitute.For&lt;ICharacterDataCache&gt;()</c>.
    /// </remarks>
    public interface ICharacterDataCache
    {
        /// <summary>
        /// Save ESI response data for a character endpoint.
        /// </summary>
        Task SaveAsync<T>(long characterId, string endpointKey, T data);

        /// <summary>
        /// Load cached data for a character endpoint. Returns null if no cache exists.
        /// </summary>
        Task<T?> LoadAsync<T>(long characterId, string endpointKey) where T : class;

        /// <summary>
        /// Delete all cached data for a character.
        /// </summary>
        Task ClearCharacterAsync(long characterId);

        /// <summary>
        /// Delete all cached data for all characters.
        /// </summary>
        Task ClearAllAsync();
    }
}
