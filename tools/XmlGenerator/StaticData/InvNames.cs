using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class InvNames : IHasID
    {
        [XmlElement("itemID")]
        public int ID { get; set; }

        [XmlElement("itemName")]
        public string Name { get; set; }
    }
}