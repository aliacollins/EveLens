// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a plan entry
    /// </summary>
    public sealed class SerializableInvalidPlanEntry
    {
        [XmlAttribute("skill")]
        public string? SkillName { get; set; }

        [XmlAttribute("level")]
        public long PlannedLevel { get; set; }

        [XmlAttribute("acknowledged")]
        public bool Acknowledged { get; set; }
    }
}