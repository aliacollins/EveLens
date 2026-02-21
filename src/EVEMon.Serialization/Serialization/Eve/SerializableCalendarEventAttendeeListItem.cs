// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableCalendarEventAttendeeListItem
    {
        [XmlAttribute("characterID")]
        public long CharacterID { get; set; }

        [XmlAttribute("name")]
        public string? CharacterNameXml
        {
            get { return CharacterName; }
            set { CharacterName = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlAttribute("response")]
        public string? ResponseXml
        {
            get { return Response; }
            set { Response = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlIgnore]
        public string? CharacterName { get; set; }

        [XmlIgnore]
        public string? Response { get; set; }
    }
}