using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class AgtResearchAgents : IHasID
    {
        [XmlElement("agentID")]
        public int ID { get; set; }

        [XmlElement("typeID")]
        public int ResearchSkillID { get; set; }
    }
}