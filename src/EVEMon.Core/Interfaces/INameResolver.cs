using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE entity IDs (characters, corporations, alliances) to display names.
    /// Breaks Model -> EveIDToName/EveRefType Service dependency (52 call sites, 18 files).
    /// </summary>
    public interface INameResolver
    {
        /// <summary>
        /// Gets the name for an EVE entity ID. Returns "Unknown" if not yet resolved.
        /// May trigger an async background lookup.
        /// </summary>
        /// <param name="id">The EVE entity ID.</param>
        /// <param name="bypassCache">When true, forces a fresh lookup.</param>
        /// <returns>The entity name, or "Unknown" if not yet resolved.</returns>
        string GetName(long id, bool bypassCache = false);

        /// <summary>
        /// Gets names for multiple IDs. Returns null for entries still being queried.
        /// </summary>
        /// <param name="ids">The EVE entity IDs to resolve.</param>
        /// <returns>The resolved names.</returns>
        IEnumerable<string> GetNames(IEnumerable<long> ids);

        /// <summary>
        /// Gets a reference type name from its ID (wallet journal entries).
        /// </summary>
        /// <param name="refTypeId">The reference type ID.</param>
        /// <returns>The reference type display name.</returns>
        string GetRefTypeName(int refTypeId);
    }
}
