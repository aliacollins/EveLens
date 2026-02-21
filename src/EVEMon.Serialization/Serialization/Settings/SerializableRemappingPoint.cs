// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a remapping point.
    /// </summary>
    public sealed class SerializableRemappingPoint
    {
        [XmlAttribute("status")]
        public RemappingPointStatus Status { get; set; }

        [XmlAttribute("per")]
        public long Perception { get; set; }

        [XmlAttribute("int")]
        public long Intelligence { get; set; }

        [XmlAttribute("mem")]
        public long Memory { get; set; }

        [XmlAttribute("wil")]
        public long Willpower { get; set; }

        [XmlAttribute("cha")]
        public long Charisma { get; set; }

        [XmlAttribute("description")]
        public string? Description { get; set; }
    }
}