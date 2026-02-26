using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class InvControlTowerResourcePurposes : IHasID
    {
        [XmlElement("purpose")]
        public int ID { get; set; }

        [XmlElement("purposeText")]
        public string PurposeName { get; set; }
    }
}