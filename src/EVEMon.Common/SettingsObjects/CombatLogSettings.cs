// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Settings for Combat Log.
    /// </summary>
    /// <remarks>
    /// This is the optimized way to implement the object as serializable and satisfy all FxCop rules.
    /// Don't use auto-property with private setter for the collections as it does not work with XmlSerializer.
    /// </remarks>
    public sealed class CombatLogSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether to show condensed combat log.
        /// </summary>
        /// <value>
        ///   <c>true</c> if to show condensed combat log; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showCondensedLogs")]
        public bool ShowCondensedLogs { get; set; }
    }
}
