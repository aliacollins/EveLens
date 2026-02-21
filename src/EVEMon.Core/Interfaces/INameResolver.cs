// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE entity IDs (characters, corporations, alliances) to display names,
    /// and resolves wallet journal reference type IDs to their display names.
    /// Breaks the Model to <c>EveIDToName</c>/<c>EveRefType</c> static service dependency
    /// (52 call sites across 18 files).
    /// </summary>
    /// <remarks>
    /// Name resolution is cached internally. A cache miss triggers an asynchronous ESI lookup
    /// in the background and returns "Unknown" immediately. On the next call (after the background
    /// lookup completes), the resolved name will be returned. UI controls typically re-query
    /// on the next timer tick to pick up newly resolved names.
    ///
    /// Reference type names (<see cref="GetRefTypeName"/>) are loaded from static data and
    /// do not require an ESI call.
    ///
    /// Production: <c>NameResolverAdapter</c> in <c>EVEMon.Common/Services/NameResolverAdapter.cs</c>
    /// (delegates to static <c>EveIDToName</c> and <c>EveRefType</c>).
    /// Testing: Provide a stub that returns deterministic names for known IDs.
    /// </remarks>
    public interface INameResolver
    {
        /// <summary>
        /// Gets the display name for an EVE entity ID. Returns "Unknown" if the name
        /// has not yet been resolved; a background ESI lookup is triggered in that case.
        /// </summary>
        /// <param name="id">The EVE entity ID (character, corporation, or alliance).</param>
        /// <param name="bypassCache">When true, forces a fresh ESI lookup even if cached.</param>
        /// <returns>The entity name, or "Unknown" if not yet resolved.</returns>
        string GetName(long id, bool bypassCache = false);

        /// <summary>
        /// Batch-resolves multiple EVE entity IDs to display names.
        /// Returns null for entries that are still being queried asynchronously.
        /// </summary>
        /// <param name="ids">The EVE entity IDs to resolve.</param>
        /// <returns>An enumerable of resolved names (null entries indicate pending lookups).</returns>
        IEnumerable<string> GetNames(IEnumerable<long> ids);

        /// <summary>
        /// Gets the display name for a wallet journal reference type ID.
        /// Resolved from static data (no ESI call required).
        /// </summary>
        /// <param name="refTypeId">The reference type ID from a wallet journal entry.</param>
        /// <returns>The reference type display name.</returns>
        string GetRefTypeName(int refTypeId);
    }
}
