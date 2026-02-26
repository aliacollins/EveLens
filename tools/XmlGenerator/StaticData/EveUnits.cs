using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class EveUnits : IHasID
    {
        [XmlElement("unitID")]
        public int ID { get; set; }

        [XmlElement("unitName")]
        public string Name { get; set; }

        [XmlElement("displayName")]
        public string DisplayName { get; set; }

        [XmlElement("description")]
        public string Description { get; set; }
    }
}