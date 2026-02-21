// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiPlanetaryRoute
    {
        [DataMember(Name = "route_id")]
        public long RouteID { get; set; }

        [DataMember(Name = "source_pin_id")]
        public long SourcePinID { get; set; }

        [DataMember(Name = "destination_pin_id")]
        public long DestinationPinID { get; set; }

        [DataMember(Name = "content_type_id")]
        public long ContentTypeID { get; set; }

        [DataMember(Name = "quantity")]
        public int Quantity { get; set; }

        [DataMember(Name = "waypoints", EmitDefaultValue = false, IsRequired = false)]
        public List<long> Waypoints { get; set; } = new();
    }
}
