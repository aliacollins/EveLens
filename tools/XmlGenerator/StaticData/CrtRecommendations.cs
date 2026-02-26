using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class CrtRecommendations : IHasID
    {
        [XmlElement("recommendationID")]
        public int ID { get; set; }

        [XmlElement("shipTypeID")]
        public int ShipTypeID { get; set; }

        [XmlElement("certificateID")]
        public int CertificateID { get; set; }

        [XmlElement("recommendationLevel")]
        public int Level { get; set; }
    }
}