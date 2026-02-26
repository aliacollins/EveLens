using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class InvFlags : IHasID
    {
        [XmlElement("flagID")]
        public int ID { get; set; }

        [XmlElement("flagName")]
        public string Name { get; set; }

        [XmlElement("flagText")]
        public string Text { get; set; }

        [XmlElement("orderID")]
        public short OrderID { get; set; }

    }
}
