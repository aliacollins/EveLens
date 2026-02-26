// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableCharacterNameListItem
    {
        [XmlAttribute("characterID")]
        public long ID { get; set; }

        [XmlAttribute("name")]
        public string? NameXml
        {
            get { return Name; }
            set { Name = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlIgnore]
        public string? Name { get; set; }
    }
}