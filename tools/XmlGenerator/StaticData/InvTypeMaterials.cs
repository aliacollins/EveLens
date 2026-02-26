using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class InvTypeMaterials : IHasID
    {
        [XmlElement("typeID")]
        public int ID { get; set; }

        [XmlElement("materialTypeID")]
        public int MaterialTypeID { get; set; }

        [XmlElement("quantity")]
        public int Quantity { get; set; }
    }
}