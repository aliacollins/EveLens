using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class EveIcons : IHasID
    {
        [XmlElement("iconID")]
        public int ID { get; set; }

        [XmlElement("iconFile")]
        public string Icon { get; set; }
    }
}