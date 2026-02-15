using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a collection of implants sets.
    /// </summary>
    public sealed class SerializableImplantSetCollection
    {
        private readonly Collection<SerializableSettingsImplantSet> m_customSets;

        public SerializableImplantSetCollection()
        {
            ActiveClone = new SerializableSettingsImplantSet();
            JumpClones = new Collection<SerializableSettingsImplantSet>();
            m_customSets = new Collection<SerializableSettingsImplantSet>();
        }

        [XmlElement("activeCloneSet")]
        public SerializableSettingsImplantSet ActiveClone { get; set; }

        [XmlElement("jumpCloneSet")]
        public Collection<SerializableSettingsImplantSet> JumpClones { get; set; }

        [XmlElement("customSet")]
        public Collection<SerializableSettingsImplantSet> CustomSets => m_customSets;

        [XmlElement("selectedIndex")]
        public int SelectedIndex { get; set; }
    }
}