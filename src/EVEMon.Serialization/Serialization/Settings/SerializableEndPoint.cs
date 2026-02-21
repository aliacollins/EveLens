// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    public class SerializableEndPoint
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }
    }
}
