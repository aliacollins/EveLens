// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Eve;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    /// <summary>
    /// Represents a serializable version of a character's location.
    /// </summary>
    [DataContract]
    public sealed class EsiAPILocation
    {
        [DataMember(Name = "solar_system_id")]
        public int SolarSystemID { get; set; }

        [DataMember(Name = "station_id", EmitDefaultValue = false, IsRequired = false)]
        public int StationID { get; set; }

        [DataMember(Name = "structure_id", EmitDefaultValue = false, IsRequired = false)]
        public long StructureID { get; set; }

        public SerializableLocation ToXMLItem()
        {
            return new SerializableLocation()
            {
                SolarSystemID = SolarSystemID,
                StationID = StationID,
                StructureID = StructureID
            };
        }
    }
}
