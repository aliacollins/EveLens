using System.Xml.Serialization;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class MapSolarSystemsJump
    {
        [XmlElement("fromSolarSystemID")]
        public int A { get; set; }

        [XmlElement("toSolarSystemID")]
        public int B { get; set; }
    }
}