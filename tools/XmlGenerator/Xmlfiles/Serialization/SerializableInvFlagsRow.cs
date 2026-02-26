using System.Xml.Serialization;

namespace EveLens.XmlGenerator.Xmlfiles.Serialization
{
    public class SerializableInvFlagsRow
    {
        [XmlAttribute("flagID")]
        public int ID { get; set; }

        [XmlAttribute("flagName")]
        public string Name { get; set; }

        [XmlAttribute("flagText")]
        public string Text { get; set; }
    }
}