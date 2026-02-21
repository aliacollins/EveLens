// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EVEMon.Common.SettingsObjects;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a plan
    /// </summary>
    public class SerializablePlan
    {
        private readonly Collection<SerializablePlanEntry> m_entries;
        private readonly Collection<SerializableInvalidPlanEntry> m_invalidEntries;

        public SerializablePlan()
        {
            SortingPreferences = new PlanSorting();
            m_entries = new Collection<SerializablePlanEntry>();
            m_invalidEntries = new Collection<SerializableInvalidPlanEntry>();
        }

        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlAttribute("owner")]
        public Guid Owner { get; set; }

        [XmlAttribute("description")]
        public string? Description { get; set; }

        [XmlElement("sorting")]
        public PlanSorting SortingPreferences { get; set; }

        [XmlElement("entry")]
        public Collection<SerializablePlanEntry> Entries => m_entries;

        [XmlElement("invalidEntry")]
        public Collection<SerializableInvalidPlanEntry> InvalidEntries => m_invalidEntries;
    }
}