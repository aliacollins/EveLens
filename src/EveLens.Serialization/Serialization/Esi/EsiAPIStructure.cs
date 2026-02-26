// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiAPIStructure
    {
        [DataMember(Name = "name")]
        public string? StationName { get; set; }

        [DataMember(Name = "type_id", EmitDefaultValue = false, IsRequired = false)]
        public int StationTypeID { get; set; }

        [DataMember(Name = "solar_system_id")]
        public int SolarSystemID { get; set; }
        
        [DataMember(Name = "position", EmitDefaultValue = false, IsRequired = false)]
        public EsiPosition? Position { get; set; }

        [DataMember(Name = "owner_id", EmitDefaultValue = false, IsRequired = false)]
        public int OwnerID { get; set; }

        public SerializableOutpost ToXMLItem(long id)
        {
            return new SerializableOutpost()
            {
                CorporationID = OwnerID,
                StationID = id,
                SolarSystemID = SolarSystemID,
                StationTypeID = StationTypeID,
                StationName = StationName
            };
        }
    }
}
