// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class ItemBrowserSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ItemBrowserSettings"/> class.
        /// </summary>
        public ItemBrowserSettings()
        {
            UsabilityFilter = ObjectUsabilityFilter.All;
            MetagroupFilter = ItemMetaGroup.All;
            SlotFilter = ItemSlot.All;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show all groups].
        /// </summary>
        /// <value><c>true</c> if [show all groups]; otherwise, <c>false</c>.</value>
        [XmlElement("showAllGroups")]
        public bool ShowAllGroups { get; set; }

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
        /// Gets or sets the slot filter.
        /// </summary>
        /// <value>The slot filter.</value>
        [XmlElement("slotFilter")]
        public ItemSlot SlotFilter { get; set; }

        /// <summary>
        /// Gets or sets the metagroup filter.
        /// </summary>
        /// <value>The metagroup filter.</value>
        [XmlElement("metaGroupFilter")]
        public ItemMetaGroup MetagroupFilter { get; set; }
    }
}