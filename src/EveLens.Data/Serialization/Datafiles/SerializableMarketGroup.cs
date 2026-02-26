// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents an items category (standard item categories, not market groups) from our datafile.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableMarketGroup
    {
        private readonly Collection<SerializableItem> m_items;
        private readonly Collection<SerializableMarketGroup> m_subGroups;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableMarketGroup"/> class.
        /// </summary>
        public SerializableMarketGroup()
        {
            m_items = new Collection<SerializableItem>();
            m_subGroups = new Collection<SerializableMarketGroup>();
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
        /// Gets the items.
        /// </summary>
        /// <value>The items.</value>
        [XmlArray("items")]
        [XmlArrayItem("item")]
        public Collection<SerializableItem> Items => m_items;

        /// <summary>
        /// Gets the sub groups.
        /// </summary>
        /// <value>The sub groups.</value>
        [XmlArray("marketGroups")]
        [XmlArrayItem("marketGroup")]
        public Collection<SerializableMarketGroup> SubGroups => m_subGroups;
    }
}