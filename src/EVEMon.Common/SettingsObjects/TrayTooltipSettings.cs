// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class TrayTooltipSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrayTooltipSettings"/> class.
        /// </summary>
        public TrayTooltipSettings()
        {
            Format = "%n - %s %tr - %r";
        }

        /// <summary>
        /// Gets or sets the format.
        /// </summary>
        /// <value>The format.</value>
        [XmlElement("format")]
        public string Format { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [display order].
        /// </summary>
        /// <value><c>true</c> if [display order]; otherwise, <c>false</c>.</value>
        [XmlElement("displayOrder")]
        public bool DisplayOrder { get; set; }
    }
}