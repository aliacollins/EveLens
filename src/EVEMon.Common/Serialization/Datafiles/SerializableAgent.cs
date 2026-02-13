using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents an eve agent.
    /// </summary>
    public sealed class SerializableAgent
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("agentID")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("agentName")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the name of the division.
        /// </summary>
        /// <value>The name of the division.</value>
        [XmlAttribute("divisionName")]
        public string? DivisionName { get; set; }

        /// <summary>
        /// Gets or sets the level.
        /// </summary>
        /// <value>The level.</value>
        [XmlAttribute("level")]
        public int Level { get; set; }

        /// <summary>
        /// Gets or sets the quality.
        /// </summary>
        /// <value>The quality.</value>
        [XmlAttribute("quality")]
        public int Quality { get; set; }

        /// <summary>
        /// Gets or sets the type of the agent.
        /// </summary>
        /// <value>The type of the agent.</value>
        [XmlAttribute("agentType")]
        public string? AgentType { get; set; }

        /// <summary>
        /// Gets or sets the research skill ID.
        /// </summary>
        /// <value>The research skill ID.</value>
        [XmlAttribute("researchSkillID")]
        public int ResearchSkillID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the agent offers locator service.
        /// </summary>
        /// <value><c>true</c> if the agent offers locator service; otherwise, <c>false</c>.</value>
        [XmlAttribute("locatorService")]
        public bool LocatorService { get; set; }
    }
}