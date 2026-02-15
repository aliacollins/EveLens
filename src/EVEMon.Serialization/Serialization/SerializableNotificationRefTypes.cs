using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    [XmlRoot("NotificationRefTypes")]
    public sealed class SerializableNotificationRefTypes
    {
        private readonly Collection<SerializableNotificationRefTypesListItem> m_types;

        public SerializableNotificationRefTypes()
        {
            m_types = new Collection<SerializableNotificationRefTypesListItem>();
        }

        [XmlArray("refTypes")]
        [XmlArrayItem("refType")]
        public Collection<SerializableNotificationRefTypesListItem> Types => m_types;
    }
}