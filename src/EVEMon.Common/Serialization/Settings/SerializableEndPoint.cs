using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    public class SerializableEndPoint
    {
        [XmlAttribute("name")]
        public string? Name { get; set; }

        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }
    }
}
