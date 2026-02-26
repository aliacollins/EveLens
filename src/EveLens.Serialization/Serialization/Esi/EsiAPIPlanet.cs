// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// Represents planet information.
    /// </summary>
    [DataContract]
    public class EsiAPIPlanet
    {
        [DataMember(Name = "name")]
        public string? Name { get; set; }

        [DataMember(Name = "planet_id")]
        public int PlanetID { get; set; }

        [DataMember(Name = "position")]
        public EsiPosition? Position { get; set; }

        [DataMember(Name = "system_id")]
        public int SystemID { get; set; }

        [DataMember(Name = "type_id")]
        public int TypeID { get; set; }
    }
}
