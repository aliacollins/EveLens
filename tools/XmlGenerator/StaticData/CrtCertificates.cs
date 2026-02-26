using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class CrtCertificates : IHasID
    {
        [XmlElement("certificateID")]
        public int ID { get; set; }

        [XmlElement("groupID")]
        public int GroupID { get; set; }

        [XmlElement("classID")]
        public int ClassID { get; set; }

        [XmlElement("grade")]
        public int Grade { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }
    }
}