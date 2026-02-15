using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    public sealed class SerializableStanding
    {
        [XmlAttribute("entityID")]
        public int EntityID { get; set; }

        [XmlAttribute("entityName")]
        public string? EntityName { get; set; }

        [XmlAttribute("standing")]
        public double StandingValue { get; set; }

        [XmlAttribute("group")]
        public string? Group { get; set; }
    }
}