// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization
{
    public sealed class SerializableEveFlagsListItem
    {
        [XmlAttribute("flagID")]
        public int ID { get; set; }

        [XmlAttribute("flagName")]
        public string? Name { get; set; }

        [XmlAttribute("flagText")]
        public string? Text { get; set; }
    }
}