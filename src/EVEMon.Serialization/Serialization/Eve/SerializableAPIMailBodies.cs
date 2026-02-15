using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of a characters' eve mail messages bodies. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIMailBodies
    {
        private readonly Collection<SerializableMailBodiesListItem> m_bodies;

        public SerializableAPIMailBodies()
        {
            m_bodies = new Collection<SerializableMailBodiesListItem>();
        }

        [XmlArray("messages")]
        [XmlArrayItem("message")]
        public Collection<SerializableMailBodiesListItem> Bodies => m_bodies;

        [XmlElement("missingMessageIDs")]
        public string? MissingMessageIDs { get; set; }
    }
}