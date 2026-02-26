// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable location containing a solar system and station / structure.
    /// </summary>
    public sealed class SerializableLocation
    {
        [XmlElement("solarSystemID")]
        public int SolarSystemID { get; set; }

        [XmlElement("stationID")]
        public int StationID { get; set; }

        [XmlElement("structureID")]
        public long StructureID { get; set; }
    }
}
