// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE inventory flag IDs to human-readable display text and vice versa.
    /// Inventory flags indicate where an item is located within a ship or station
    /// (e.g., "Cargo", "DroneBay", "LoSlot0").
    /// Breaks the Model to <c>EveFlag</c> static service dependency (5 call sites, 2 files).
    /// </summary>
    /// <remarks>
    /// Flag data is loaded from static datafiles at startup and does not require ESI calls.
    /// Both methods are pure lookups with no side effects.
    ///
    /// Production: <c>FlagResolverAdapter</c> in <c>EVEMon.Common/Services/FlagResolverAdapter.cs</c>
    /// (delegates to static <c>EveFlag.GetFlagText()</c> and <c>EveFlag.GetFlagID()</c>).
    /// Testing: Provide a stub with a small dictionary of test flag IDs and names.
    /// </remarks>
    public interface IFlagResolver
    {
        /// <summary>
        /// Gets the human-readable display text for an inventory flag ID
        /// (e.g., flag 5 returns "Cargo").
        /// </summary>
        /// <param name="flagId">The EVE inventory flag ID.</param>
        /// <returns>The display text for the flag.</returns>
        string GetFlagText(int flagId);

        /// <summary>
        /// Gets the flag ID for a given flag name string.
        /// Used for reverse lookups when parsing serialized data.
        /// </summary>
        /// <param name="flagName">The flag name (e.g., "Cargo").</param>
        /// <returns>The corresponding flag ID.</returns>
        int GetFlagID(string flagName);
    }
}
