using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class CrpNPCDivisions : IHasID
    {
        [XmlElement("divisionID")]
        public int ID { get; set; }

        [XmlElement("divisionName")]
        public string DivisionName { get; set; }
    }
}