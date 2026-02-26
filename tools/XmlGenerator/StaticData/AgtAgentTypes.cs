using System.Xml.Serialization;
using EveLens.XmlGenerator.Interfaces;

namespace EveLens.XmlGenerator.StaticData
{
    public sealed class AgtAgentTypes : IHasID
    {
        [XmlElement("agentTypeID")]
        public int ID { get; set; }

        [XmlElement("agentType")]
        public string AgentType { get; set; }
    }
}