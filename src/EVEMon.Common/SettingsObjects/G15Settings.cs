// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class G15Settings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="G15Settings"/> class.
        /// </summary>
        public G15Settings()
        {
            CharactersCycleInterval = 20;
            TimeFormatsCycleInterval = 10;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="G15Settings"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [XmlElement("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use characters cycle].
        /// </summary>
        /// <value><c>true</c> if [use characters cycle]; otherwise, <c>false</c>.</value>
        [XmlElement("useCharsCycle")]
        public bool UseCharactersCycle { get; set; }

        /// <summary>
        /// Gets or sets the characters cycle interval.
        /// </summary>
        /// <value>The characters cycle interval.</value>
        [XmlElement("charsCycleInterval")]
        public int CharactersCycleInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use time formats cycle].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use time formats cycle]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("useTimeFormatsCycle")]
        public bool UseTimeFormatsCycle { get; set; }

        /// <summary>
        /// Gets or sets the time formats cycle interval.
        /// </summary>
        /// <value>The time formats cycle interval.</value>
        [XmlElement("timeFormatsCycleInterval")]
        public int TimeFormatsCycleInterval { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show system time].
        /// </summary>
        /// <value><c>true</c> if [show system time]; otherwise, <c>false</c>.</value>
        [XmlElement("showSystemTime")]
        public bool ShowSystemTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show EVE time].
        /// </summary>
        /// <value><c>true</c> if [show EVE time]; otherwise, <c>false</c>.</value>
        [XmlElement("showEVETime")]
        public bool ShowEVETime { get; set; }
    }
}