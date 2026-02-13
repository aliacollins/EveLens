using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableEveFactionWarsListItem
    {
        [XmlAttribute("factionID")]
        public int FactionID { get; set; }

        [XmlAttribute("factionName")]
        public string? FactionName { get; set; }

        [XmlAttribute("againstID")]
        public int AgainstID { get; set; }

        [XmlAttribute("againstName")]
        public string? AgainstName { get; set; }
    }
}