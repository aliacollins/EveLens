// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Collections;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models.Collections
{
    /// <summary>
    /// The SKINR component licenses (nanocoatings and patterns) a character owns.
    /// Kept current by the <c>SkinrComponents</c> query monitor on ESI's own cache cadence.
    /// </summary>
    public sealed class SkinrComponentCollection : ReadonlyCollection<SkinrComponentLicense>
    {
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal SkinrComponentCollection()
        {
        }

        /// <summary>
        /// Imports the API inventory, replacing the previous state.
        /// </summary>
        /// <param name="src">The serializable inventory from the API.</param>
        internal void Import(EsiSkinrComponentInventory src)
        {
            Items.Clear();
            foreach (EsiSkinrComponentLicense license in src.Licenses)
            {
                Items.Add(new SkinrComponentLicense(license));
            }
        }
    }
}
