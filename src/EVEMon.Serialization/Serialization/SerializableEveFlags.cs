using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    [XmlRoot("invFlags")]
    public sealed class SerializableEveFlags
    {
        private readonly Collection<SerializableEveFlagsListItem> m_eveFlags;

        public SerializableEveFlags()
        {
            m_eveFlags = new Collection<SerializableEveFlagsListItem>();
        }

        [XmlArray("flags")]
        [XmlArrayItem("flag")]
        public Collection<SerializableEveFlagsListItem> EVEFlags => m_eveFlags;
    }
}
