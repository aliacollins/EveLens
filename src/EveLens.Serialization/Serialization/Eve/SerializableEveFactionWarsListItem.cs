// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableEveFactionWarsListItem
    {
        [XmlAttribute("factionID")]
        public int FactionID { get; set; }

        [XmlAttribute("factionName")]
        public string? FactionName { get; set; }

        [XmlAttribute("againstID")]
        public int AgainstID { get; set; }

        [XmlAttribute("againstName")]
        public string? AgainstName { get; set; }
    }
}