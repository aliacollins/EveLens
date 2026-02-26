// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.FittingXml
{
    public sealed class SerializableXmlFitting
    {
        private readonly Collection<SerializableXmlFittingHardware> m_fittingHardwares;

        public SerializableXmlFitting()
        {
            m_fittingHardwares = new Collection<SerializableXmlFittingHardware>();
        }

        [XmlElement("hardware")]
        public Collection<SerializableXmlFittingHardware> FittingHardware => m_fittingHardwares;

        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlElement("description")]
        public SerializableXmlFittingDescription? Description { get; set; }

        [XmlElement("shipType")]
        public SerializableXmlFittingShipType? ShipType { get; set; }
    }
}