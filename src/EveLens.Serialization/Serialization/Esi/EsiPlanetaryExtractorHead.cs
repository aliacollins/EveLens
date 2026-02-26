// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiPlanetaryExtractorHead
    {
        [DataMember(Name = "head_id")]
        public int HeadID { get; set; }

        [DataMember(Name = "longitude")]
        public double Longitude { get; set; }

        [DataMember(Name = "latitude")]
        public double Latitude { get; set; }
    }
}
