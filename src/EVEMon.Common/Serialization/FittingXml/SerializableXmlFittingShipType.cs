using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.FittingXml
{
    public sealed class SerializableXmlFittingShipType
    {
        [XmlAttribute("value")]
        public string? Name { get; set; }
    }
}