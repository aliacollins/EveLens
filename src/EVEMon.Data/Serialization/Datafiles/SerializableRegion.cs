// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a region of the EVE universe.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class SerializableRegion
    {
        private readonly Collection<SerializableConstellation> m_constellations;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableRegion"/> class.
        /// </summary>
        public SerializableRegion()
        {
            m_constellations = new Collection<SerializableConstellation>();
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
        /// Gets the constellations.
        /// </summary>
        /// <value>The constellations.</value>
        [XmlElement("constellations")]
        public Collection<SerializableConstellation> Constellations => m_constellations;
    }
}