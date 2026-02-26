// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a mastery ship from our datafiles
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public class SerializableMasteryShip
    {
        private readonly Collection<SerializableMastery> m_masteries;

        /// <summary>
        /// Initializes a new instance of the <see cref="SerializableMasteryShip"/> class.
        /// </summary>
        public SerializableMasteryShip()
        {
            m_masteries = new Collection<SerializableMastery>();
        }
        
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlAttribute("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets the masteries.
        /// </summary>
        /// <value>
        /// The masteries.
        /// </value>
        [XmlElement("mastery")]
        public Collection<SerializableMastery> Masteries => m_masteries;
    }
}