using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a certificate from our datafiles
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableCertificate
    {
        private readonly Collection<SerializableCertificatePrerequisite> m_prerequisites;
        private readonly Collection<SerializableCertificateRecommendation> m_recommendations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableCertificate"/> class.
        /// </summary>
        public SerializableCertificate()
        {
            m_prerequisites = new Collection<SerializableCertificatePrerequisite>();
            m_recommendations = new Collection<SerializableCertificateRecommendation>();
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [XmlAttribute("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets the prerequisites.
        /// </summary>
        /// <value>The prerequisites.</value>
        [XmlElement("requires")]
        public Collection<SerializableCertificatePrerequisite> Prerequisites => m_prerequisites;

        /// <summary>
        /// Gets the recommendations.
        /// </summary>
        /// <value>The recommendations.</value>
        [XmlElement("recommendation")]
        public Collection<SerializableCertificateRecommendation> Recommendations => m_recommendations;
    }
}