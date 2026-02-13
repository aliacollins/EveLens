using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.FittingXml
{
    [XmlRoot("fittings")]
    public sealed class SerializableXmlFittings
    {
        [XmlElement("fitting")]
        public SerializableXmlFitting? Fitting { get; set; }
    }
}
