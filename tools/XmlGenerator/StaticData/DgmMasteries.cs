using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class DgmMasteries : IHasID
    {
        [XmlElement("masteryID")]
        public int ID { get; set; }

        [XmlElement("certificateID")]
        public int CertificateID { get; set; }

        [XmlElement("grade")]
        public short Grade { get; set; }
    }
}
