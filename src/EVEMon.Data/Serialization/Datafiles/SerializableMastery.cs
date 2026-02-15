using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a mastery from our datafiles
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public class SerializableMastery
    {
        private readonly Collection<SerializableMasteryCertificate> m_certificates;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableMasteryShip"/> class.
        /// </summary>
        public SerializableMastery()
        {
            m_certificates = new Collection<SerializableMasteryCertificate>();
        }

        /// <summary>
        /// Gets or sets the grade.
        /// </summary>
        /// <value>
        /// The grade.
        /// </value>
        [XmlAttribute("grade")]
        public int Grade { get; set; }

        /// <summary>
        /// Gets the certificates.
        /// </summary>
        /// <value>
        /// The certificates.
        /// </value>
        [XmlElement("certificate")]
        public Collection<SerializableMasteryCertificate> Certificates => m_certificates;
    }
}