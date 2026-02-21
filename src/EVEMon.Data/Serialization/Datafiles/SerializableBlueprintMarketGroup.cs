// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a blueprint group in our datafile.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableBlueprintMarketGroup
    {
        private readonly Collection<SerializableBlueprint> m_blueprints;
        private readonly Collection<SerializableBlueprintMarketGroup> m_subGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableBlueprintMarketGroup"/> class.
        /// </summary>
        public SerializableBlueprintMarketGroup()
        {
            m_blueprints = new Collection<SerializableBlueprint>();
            m_subGroups = new Collection<SerializableBlueprintMarketGroup>();
        }

        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets the blueprints.
        /// </summary>
        /// <value>The blueprints.</value>
        [XmlElement("blueprint")]
        public Collection<SerializableBlueprint> Blueprints => m_blueprints;

        /// <summary>
        /// Gets the sub groups.
        /// </summary>
        /// <value>The sub groups.</value>
        [XmlArray("subGroups")]
        [XmlArrayItem("subGroup")]
        public Collection<SerializableBlueprintMarketGroup> SubGroups => m_subGroups;
    }
}