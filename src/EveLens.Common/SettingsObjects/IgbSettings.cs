// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    public sealed class IgbSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="IgbSettings"/> class.
        /// </summary>
        public IgbSettings()
        {
            IgbServerPort = 80;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [IGB server enabled].
        /// </summary>
        /// <value><c>true</c> if [IGB server enabled]; otherwise, <c>false</c>.</value>
        [XmlElement("igbServerEnabled")]
        public bool IgbServerEnabled { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [IGB server public].
        /// </summary>
        /// <value><c>true</c> if [IGB server public]; otherwise, <c>false</c>.</value>
        [XmlElement("igbServerPublic")]
        public bool IgbServerPublic { get; set; }

        /// <summary>
        /// Gets or sets the IGB server port.
        /// </summary>
        /// <value>The IGB server port.</value>
        [XmlElement("igbServerPort")]
        public int IgbServerPort { get; set; }
    }
}