// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a station in the EVE universe.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableStation
    {
        private readonly Collection<SerializableAgent> m_agents;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableStation"/> class.
        /// </summary>
        public SerializableStation()
        {
            m_agents = new Collection<SerializableAgent>();
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public long ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the corporation ID.
        /// </summary>
        /// <value>The corporation ID.</value>
        [XmlAttribute("corporationID")]
        public int CorporationID { get; set; }

        /// <summary>
        /// Gets or sets the name of the corporation.
        /// </summary>
        /// <value>The name of the corporation.</value>
        [XmlAttribute("corporationName")]
        public string? CorporationName { get; set; }

        /// <summary>
        /// Gets or sets the reprocessing efficiency.
        /// </summary>
        /// <value>The reprocessing efficiency.</value>
        [XmlElement("reprocessingEfficiency")]
        public float ReprocessingEfficiency { get; set; }

        /// <summary>
        /// Gets or sets the reprocessing stations take.
        /// </summary>
        /// <value>The reprocessing stations take.</value>
        [XmlElement("reprocessingStationsTake")]
        public float ReprocessingStationsTake { get; set; }

        /// <summary>
        /// Gets the agents.
        /// </summary>
        /// <value>The agents.</value>
        [XmlArray("agents")]
        [XmlArrayItem("agent")]
        public Collection<SerializableAgent> Agents => m_agents;
    }
}