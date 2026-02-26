// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Represents a serializable version of a planetary colony. Used for querying CCP.
    /// </summary>
    [DataContract]
    public sealed class EsiAPIPlanetaryColony
    {
        [DataMember(Name = "links")]
        public List<EsiPlanetaryLink> Links { get; set; } = new();

        [DataMember(Name = "pins")]
        public List<EsiPlanetaryPin> Pins { get; set; } = new();

        [DataMember(Name = "routes")]
        public List<EsiPlanetaryRoute> Routes { get; set; } = new();
    }
}
