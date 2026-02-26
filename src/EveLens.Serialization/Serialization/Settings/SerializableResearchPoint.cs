// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Settings
{
    public sealed class SerializableResearchPoint
    {
        [XmlAttribute("agentID")]
        public int AgentID { get; set; }

        [XmlAttribute("agentName")]
        public string? AgentName { get; set; }

        [XmlAttribute("skillID")]
        public int SkillID { get; set; }

        [XmlAttribute("skillName")]
        public string? SkillName { get; set; }

        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("pointsPerDay")]
        public double PointsPerDay { get; set; }

        [XmlAttribute("remainderPoints")]
        public float RemainderPoints { get; set; }
    }
}