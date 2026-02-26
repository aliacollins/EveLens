// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Constants;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a reference to a character in the charactersList API
    /// </summary>
    public class SerializableCharacterListItem : ISerializableCharacterIdentity
    {
        [XmlAttribute("characterID")]
        public long ID { get; set; }

        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlAttribute("corporationID")]
        public long CorporationID { get; set; }

        [XmlAttribute("corporationName")]
        public string? CorporationNameXml
        {
            get { return CorporationName; }
            set { CorporationName = string.IsNullOrEmpty(value) ? EveLensConstants.UnknownText : value.HtmlDecode(); }
        }

        [XmlAttribute("allianceID")]
        public long AllianceID { get; set; }

        [XmlAttribute("allianceName")]
        public string? AllianceNameXml
        {
            get { return AllianceName; }
            set { AllianceName = string.IsNullOrEmpty(value) ? EveLensConstants.UnknownText : value.HtmlDecode(); }
        }

        [XmlAttribute("factionID")]
        public int FactionID { get; set; }

        [XmlAttribute("factionName")]
        public string? FactionNameXml
        {
            get { return FactionName; }
            set { FactionName = string.IsNullOrEmpty(value) ? EveLensConstants.UnknownText : value; }
        }

        [XmlAttribute("shipTypeID")]
        public int ShipTypeID { get; set; }

        [XmlIgnore]
        public string? CorporationName { get; set; }

        [XmlIgnore]
        public string? AllianceName { get; set; }

        [XmlIgnore]
        public string? FactionName { get; set; }

        [XmlIgnore]
        public string ShipTypeName { get; set; } = EveLensConstants.UnknownText;
    }
}