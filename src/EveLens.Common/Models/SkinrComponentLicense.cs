// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models
{
    /// <summary>
    /// A SKINR component license (nanocoating or pattern) a character owns, from
    /// <c>GET /characters/{id}/cosmetics/skinr/components</c>.
    /// </summary>
    public sealed class SkinrComponentLicense
    {
        /// <summary>
        /// Constructor from the API.
        /// </summary>
        internal SkinrComponentLicense(EsiSkinrComponentLicense src)
        {
            ComponentId = src.ComponentId;
            IsPattern = string.Equals(src.Type, "pattern", StringComparison.OrdinalIgnoreCase);
            RunsRemaining = src.Runs?.Remaining;
            IsUnlimited = src.Runs?.Unlimited == true;
        }

        /// <summary>Resolves through the SDE's skinrComponents data.</summary>
        public long ComponentId { get; }

        /// <summary>True for a pattern; false for a nanocoating.</summary>
        public bool IsPattern { get; }

        /// <summary>Remaining runs, or null when the license is unlimited.</summary>
        public long? RunsRemaining { get; }

        /// <summary>True when the license never runs out.</summary>
        public bool IsUnlimited { get; }
    }
}
