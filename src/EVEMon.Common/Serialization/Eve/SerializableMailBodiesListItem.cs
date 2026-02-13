using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableMailBodiesListItem
    {
        [XmlAttribute("messageID")]
        public long MessageID { get; set; }

        [XmlText]
        public string? MessageText { get; set; }
    }
}