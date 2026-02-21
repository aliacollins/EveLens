// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class SkillBrowserSettings
    {
        /// <summary>
        /// Gets or sets the filter.
        /// </summary>
        /// <value>The filter.</value>
        [XmlElement("filter")]
        public SkillFilter Filter { get; set; }

        /// <summary>
        /// Gets or sets the filter by attributes.
        /// </summary>
        /// <value>The filter by attributes.</value>
        [XmlElement("filterByAttributesIndex")]
        public int FilterByAttributesIndex { get; set; }

        /// <summary>
        /// Gets or sets the sort.
        /// </summary>
        /// <value>The sort.</value>
        [XmlElement("sort")]
        public SkillSort Sort { get; set; }

        /// <summary>
        /// Gets or sets the text search.
        /// </summary>
        /// <value>The text search.</value>
        [XmlElement("textSearch")]
        public string TextSearch { get; set; }

        /// <summary>
        /// Gets or sets the index of the icons group.
        /// </summary>
        /// <value>The index of the icons group.</value>
        [XmlElement("iconsGroupIndex")]
        public int IconsGroupIndex { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show non public skills].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [show non public skills]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showNonPublicSkills")]
        public bool ShowNonPublicSkills { get; set; }
    }
}