// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Settings
{
    public sealed class SerializableStanding
    {
        [XmlAttribute("entityID")]
        public int EntityID { get; set; }

        [XmlAttribute("entityName")]
        public string? EntityName { get; set; }

        [XmlAttribute("standing")]
        public double StandingValue { get; set; }

        [XmlAttribute("group")]
        public string? Group { get; set; }
    }
}