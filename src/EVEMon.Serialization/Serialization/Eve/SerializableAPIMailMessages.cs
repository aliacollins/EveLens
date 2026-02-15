using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a characters' eve mail messages headers. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIMailMessages
    {
        private readonly Collection<SerializableMailMessagesListItem> m_messages;

        public SerializableAPIMailMessages()
        {
            m_messages = new Collection<SerializableMailMessagesListItem>();
        }

        [XmlArray("messages")]
        [XmlArrayItem("message")]
        public Collection<SerializableMailMessagesListItem> Messages => m_messages;
    }
}