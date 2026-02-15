using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    public sealed class SerializableRefTypesListItem
    {
        [XmlAttribute("refTypeID")]
        public int ID { get; set; }

        [XmlAttribute("refTypeName")]
        public string? Name { get; set; }
    }
}