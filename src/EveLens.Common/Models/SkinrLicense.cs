// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models
{
    /// <summary>
    /// A SKINR design license a character owns, from
    /// <c>GET /characters/{id}/cosmetics/skinr</c>.
    /// </summary>
    public sealed class SkinrLicense
    {
        /// <summary>
        /// Constructor from the API.
        /// </summary>
        internal SkinrLicense(EsiSkinrLicense src)
        {
            SkinrId = src.SkinrId;
            Activated = src.Activated;
            Unactivated = src.Unactivated;
        }

        /// <summary>The design's SKINR identifier; resolves through the public
        /// <c>/cosmetics/skinr/{skinr_id}</c> recipe route.</summary>
        public string SkinrId { get; }

        /// <summary>Whether the character has activated this design.</summary>
        public bool Activated { get; }

        /// <summary>Unactivated copies of the design the character holds.</summary>
        public long Unactivated { get; }
    }
}
