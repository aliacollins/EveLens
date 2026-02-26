// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    public sealed class CustomMsgBoxSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomMsgBoxSettings"/> class.
        /// </summary>
        public CustomMsgBoxSettings()
        {
            ShowDialogBox = true;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [show dialog box].
        /// </summary>
        /// <value><c>true</c> if [show dialog box]; otherwise, <c>false</c>.</value>
        [XmlAttribute("showDialogBox")]
        public bool ShowDialogBox { get; set; }

        /// <summary>
        /// Gets or sets the dialog result as an integer.
        /// Previously used System.Windows.Forms.DialogResult; now stored as int for cross-platform compatibility.
        /// </summary>
        /// <value>The dialog result.</value>
        [XmlAttribute("dialogResult")]
        public int DialogResult { get; set; }
    }
}