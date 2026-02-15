using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a certificate
    /// </summary>
    public sealed class SerializableCharacterCertificate
    {
        [XmlAttribute("certificateID")]
        public int CertificateID { get; set; }
    }
}