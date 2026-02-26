// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableNewImplant
    {
        [XmlAttribute("typeID")]
        public int ID { get; set; }

        [XmlAttribute("typeName")]
        public string? Name { get; set; }
    }
}