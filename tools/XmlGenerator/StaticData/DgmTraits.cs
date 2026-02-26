using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class DgmTraits : IHasID
    {
        [XmlElement("traitID")]
        public int ID { get; set; }

        [XmlElement("bonusText")]
        public string BonusText { get; set; }

        [XmlElement("unitID")]
        public int? UnitID { get; set; }

    }
}
