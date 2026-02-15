using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents our certificates datafile
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    [XmlRoot("certificatesDatafile")]
    public sealed class CertificatesDatafile
    {
        private readonly Collection<SerializableCertificateGroup> m_groups;

        /// <summary>
        /// Initializes a new instance of the <see cref="CertificatesDatafile"/> class.
        /// </summary>
        public CertificatesDatafile()
        {
            m_groups = new Collection<SerializableCertificateGroup>();
        }

        /// <summary>
        /// Gets the certificates groups.
        /// </summary>
        /// <value>The groups.</value>
        [XmlElement("certificateGroup")]
        public Collection<SerializableCertificateGroup> Groups => m_groups;
    }
}