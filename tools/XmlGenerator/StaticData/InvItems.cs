using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class InvItems : IHasID
    {
        [XmlElement("itemID")]
        public int ID { get; set; }

        [XmlElement("typeID")]
        public int TypeID { get; set; }

        [XmlElement("ownerID")]
        public int OwnerID { get; set; }

        [XmlElement("locationID")]
        public int LocationID { get; set; }

        [XmlElement("flagID")]
        public int FlagID { get; set; }

        [XmlElement("quantity")]
        public int Quantity { get; set; }
    }
}
