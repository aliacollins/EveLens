using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a certificate class from our datafiles
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableCertificateClass
    {
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
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [XmlAttribute("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets the certificates.
        /// </summary>
        [XmlElement("certificate")]
        public SerializableCertificate? Certificate { get; set; }
    }
}