// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a plan entry
    /// </summary>
    public sealed class SerializablePlanEntry
    {
        private readonly Collection<string> m_planGroups;

        public SerializablePlanEntry()
        {
            m_planGroups = new Collection<string>();
            Priority = 3;
        }

        [XmlAttribute("skillID")]
        public int ID { get; set; }

        [XmlAttribute("skill")]
        public string? SkillName { get; set; }

        [XmlAttribute("level")]
        public long Level { get; set; }

        [XmlAttribute("priority")]
        public int Priority { get; set; }

        [XmlAttribute("type")]
        public PlanEntryType Type { get; set; }

        [XmlElement("notes")]
        public string? Notes { get; set; }

        [XmlElement("group")]
        public Collection<string> PlanGroups => m_planGroups;

        [XmlElement("remapping")]
        public SerializableRemappingPoint? Remapping { get; set; }

        [XmlElement("booster")]
        public SerializableBoosterPoint? Booster { get; set; }
    }
}