// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// What to show in the system tray tooltip.
    /// </summary>
    public enum TrayTooltipDisplay
    {
        /// <summary>Training count and next finisher (default).</summary>
        Both = 0,
        /// <summary>Only the number of characters training.</summary>
        TrainingCountOnly = 1,
        /// <summary>Only the next skill to finish.</summary>
        NextFinisherOnly = 2,
    }

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

        /// <summary>
        /// Gets or sets what information the tray tooltip displays.
        /// </summary>
        [XmlElement("display")]
        public TrayTooltipDisplay Display { get; set; } = TrayTooltipDisplay.Both;
    }
}
