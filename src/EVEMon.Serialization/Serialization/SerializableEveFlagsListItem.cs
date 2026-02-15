using System.Xml.Serialization;

namespace EVEMon.Common.Serialization
{
    public sealed class SerializableEveFlagsListItem
    {
        [XmlAttribute("flagID")]
        public int ID { get; set; }

        [XmlAttribute("flagName")]
        public string? Name { get; set; }

        [XmlAttribute("flagText")]
        public string? Text { get; set; }
    }
}