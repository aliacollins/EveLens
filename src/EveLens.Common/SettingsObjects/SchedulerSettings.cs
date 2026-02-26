// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EveLens.Common.Serialization.Settings;

namespace EveLens.Common.SettingsObjects
{
    public sealed class SchedulerSettings
    {
        private readonly Collection<SerializableScheduleEntry> m_entries;

        public SchedulerSettings()
        {
            m_entries = new Collection<SerializableScheduleEntry>();
        }

        [XmlArray("entries")]
        [XmlArrayItem("simple", typeof(SerializableScheduleEntry))]
        [XmlArrayItem("recurring", typeof(SerializableRecurringScheduleEntry))]
        public Collection<SerializableScheduleEntry> Entries
        {
            get => m_entries;
            set
            {
                m_entries.Clear();
                if (value != null)
                {
                    foreach (var item in value)
                        m_entries.Add(item);
                }
            }
        }
    }
}