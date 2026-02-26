// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization
{
    public sealed class SerializableEveIDToNameListItem
    {
        [XmlAttribute("ID")]
        public long ID { get; set; }

        [XmlAttribute("name")]
        public string? Name { get; set; }
    }
}