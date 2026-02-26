// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Exportation
{
    /// <summary>
    /// A serialization class designed for HTML exportation.
    /// </summary>
    public sealed class OutputSkill
    {
        [XmlAttribute("typeName")]
        public string? Name { get; set; }

        [XmlElement("rank")]
        public long Rank { get; set; }

        [XmlElement("level")]
        public long Level { get; set; }

        [XmlElement("romanLevel")]
        public string? RomanLevel { get; set; }

        [XmlElement("SP")]
        public string? SkillPoints { get; set; }

        [XmlElement("maxSP")]
        public string? MaxSkillPoints { get; set; }
    }
}