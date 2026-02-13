using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.FittingXml
{
    public sealed class SerializableXmlFittingDescription
    {
        [XmlAttribute("value")]
        public string? Text { get; set; }
    }
}