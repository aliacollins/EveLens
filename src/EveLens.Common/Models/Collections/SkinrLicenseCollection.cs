// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Collections;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models.Collections
{
    /// <summary>
    /// The SKINR design licenses a character owns. Kept current by the
    /// <c>SkinrLicenses</c> query monitor on ESI's own cache cadence.
    /// </summary>
    public sealed class SkinrLicenseCollection : ReadonlyCollection<SkinrLicense>
    {
        /// <summary>
        /// Internal constructor.
        /// </summary>
        internal SkinrLicenseCollection()
        {
        }

        /// <summary>
        /// Imports the API inventory, replacing the previous state.
        /// </summary>
        /// <param name="src">The serializable inventory from the API.</param>
        internal void Import(EsiSkinrInventory src)
        {
            Items.Clear();
            foreach (EsiSkinrLicense license in src.Licenses)
            {
                Items.Add(new SkinrLicense(license));
            }
        }
    }
}
