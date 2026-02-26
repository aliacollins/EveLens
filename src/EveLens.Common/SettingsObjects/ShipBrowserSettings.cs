// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.SettingsObjects
{
    public sealed class ShipBrowserSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShipBrowserSettings"/> class.
        /// </summary>
        public ShipBrowserSettings()
        {
            UsabilityFilter = ObjectUsabilityFilter.All;
            RacesFilter = Race.All;
        }

        /// <summary>
        /// Gets or sets the usability filter.
        /// </summary>
        /// <value>The usability filter.</value>
        [XmlElement("usabilityFilter")]
        public ObjectUsabilityFilter UsabilityFilter { get; set; }

        /// <summary>
        /// Gets or sets the races filter.
        /// </summary>
        /// <value>The races filter.</value>
        [XmlElement("racesFilter")]
        public Race RacesFilter { get; set; }

        /// <summary>
        /// Gets or sets the text search.
        /// </summary>
        /// <value>The text search.</value>
        [XmlElement("textSearch")]
        public string TextSearch { get; set; }
    }
}