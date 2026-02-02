using System.Collections.ObjectModel;
using System.Xml.Serialization;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Common.SettingsObjects
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