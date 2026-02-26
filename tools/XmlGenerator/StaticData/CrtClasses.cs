using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class CrtClasses : IHasID
    {
        [XmlElement("classID")]
        public int ID { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }

        [XmlElement("className")]
        public string ClassName { get; set; }
    }
}