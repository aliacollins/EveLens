// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Root Blueprint Browser Settings Class
    /// </summary>
    public sealed class BlueprintBrowserSettings
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public BlueprintBrowserSettings()
        {
            UsabilityFilter = ObjectUsabilityFilter.All;
            MetagroupFilter = ItemMetaGroup.All;
            ActivityFilter = ObjectActivityFilter.Any;
        }

        /// <summary>
        /// Gets or sets the text search.
        /// </summary>
        /// <value>The text search.</value>
        [XmlElement("textSearch")]
        public string TextSearch { get; set; }

        /// <summary>
        /// Gets or sets the usability filter.
        /// </summary>
        /// <value>The usability filter.</value>
        [XmlElement("usabilityFilter")]
        public ObjectUsabilityFilter UsabilityFilter { get; set; }

        /// <summary>
        /// Gets or sets the activity filter.
        /// </summary>
        /// <value>The activity filter.</value>
        [XmlElement("activityFilter")]
        public ObjectActivityFilter ActivityFilter { get; set; }

        /// <summary>
        /// Gets or sets the metagroup filter.
        /// </summary>
        /// <value>The metagroup filter.</value>
        [XmlElement("metaGroupFilter")]
        public ItemMetaGroup MetagroupFilter { get; set; }

        /// <summary>
        /// Gets or sets the index of the production facility.
        /// </summary>
        /// <value>The index of the production facility.</value>
        [XmlElement("productionFacilityIndex")]
        public int ProductionFacilityIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the research facility.
        /// </summary>
        /// <value>The index of the research facility.</value>
        [XmlElement("researchFacilityIndex")]
        public int ResearchFacilityIndex { get; set; }

        /// <summary>
        /// Gets or sets the index of the implant set.
        /// </summary>
        /// <value>The index of the implant set.</value>
        [XmlElement("implantSetIndex")]
        public int ImplantSetIndex { get; set; }
    }
}