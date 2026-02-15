using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a constellation in the EVE universe.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableConstellation
    {
        private readonly Collection<SerializableSolarSystem> m_systems;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableConstellation"/> class.
        /// </summary>
        public SerializableConstellation()
        {
            m_systems = new Collection<SerializableSolarSystem>();
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets the systems.
        /// </summary>
        /// <value>The systems.</value>
        [XmlElement("systems")]
        public Collection<SerializableSolarSystem> Systems => m_systems;
    }
}