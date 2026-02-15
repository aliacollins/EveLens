using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    [XmlRoot("EveIDToName")]
    public sealed class SerializableEveIDToName
    {
        private readonly Collection<SerializableEveIDToNameListItem> m_entities;

        public SerializableEveIDToName()
        {
            m_entities = new Collection<SerializableEveIDToNameListItem>();
        }

        [XmlArray("entities")]
        [XmlArrayItem("entity")]
        public Collection<SerializableEveIDToNameListItem> Entities => m_entities;
    }
}