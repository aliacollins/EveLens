// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Exportation
{
    /// <summary>
    /// A serialization class designed for HTML exportation.
    /// </summary>
    public sealed class OutputSkillGroup
    {
        private readonly Collection<OutputSkill> m_skills;

        public OutputSkillGroup()
        {
            m_skills = new Collection<OutputSkill>();
        }

        [XmlAttribute("groupName")]
        public string? Name { get; set; }

        [XmlAttribute("skillsCount")]
        public int SkillsCount { get; set; }

        [XmlAttribute("totalSP")]
        public string? TotalSP { get; set; }

        [XmlElement("skill")]
        public Collection<OutputSkill> Skills => m_skills;
    }
}