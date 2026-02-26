// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations.UISettings;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Configuration settings for the tray icon popup window
    /// </summary>
    public sealed class TrayPopupSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TrayPopupSettings"/> class.
        /// </summary>
        public TrayPopupSettings()
        {
            PortraitSize = PortraitSizes.x48;
            HighlightConflicts = true;
            ShowSkillQueueTrainingTime = true;
            ShowSkillInTraining = true;
            ShowRemainingTime = true;
            ShowPortrait = true;
            ShowServerStatus = true;
            ShowEveTime = true;
            ShowWarning = true;
            IndentGroupedAccounts = true;
        }

        /// <summary>
        /// Gets or sets the style.
        /// </summary>
        /// <value>The style.</value>
        [XmlElement("style")]
        public TrayPopupStyles Style { get; set; }

        /// <summary>
        /// Gets or sets the group by.
        /// </summary>
        /// <value>The group by.</value>
        [XmlElement("groupBy")]
        public TrayPopupGrouping GroupBy { get; set; }

        /// <summary>
        /// Gets or sets the primary sort order.
        /// </summary>
        /// <value>The primary sort order.</value>
        [XmlElement("primarySortOrder")]
        public TrayPopupSort PrimarySortOrder { get; set; }

        /// <summary>
        /// Gets or sets the secondary sort order.
        /// </summary>
        /// <value>The secondary sort order.</value>
        [XmlElement("secondarySortOrder")]
        public TrayPopupSort SecondarySortOrder { get; set; }

        /// <summary>
        /// Gets or sets the size of the portrait.
        /// </summary>
        /// <value>The size of the portrait.</value>
        [XmlElement("portraitSize")]
        public PortraitSizes PortraitSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show char not training].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [show char not training]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showCharNotTraining")]
        public bool ShowCharNotTraining { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show skill in training].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [show skill in training]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showSkillInTraining")]
        public bool ShowSkillInTraining { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show remaining time].
        /// </summary>
        /// <value><c>true</c> if [show remaining time]; otherwise, <c>false</c>.</value>
        [XmlElement("showRemainingTime")]
        public bool ShowRemainingTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show completion time].
        /// </summary>
        /// <value><c>true</c> if [show completion time]; otherwise, <c>false</c>.</value>
        [XmlElement("showTimeToCompletion")]
        public bool ShowCompletionTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show wallet].
        /// </summary>
        /// <value><c>true</c> if [show wallet]; otherwise, <c>false</c>.</value>
        [XmlElement("showWallet")]
        public bool ShowWallet { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show portrait].
        /// </summary>
        /// <value><c>true</c> if [show portrait]; otherwise, <c>false</c>.</value>
        [XmlElement("showPortrait")]
        public bool ShowPortrait { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show skill queue training time].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [show skill queue training time]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("showSkillQueueTrainingTime")]
        public bool ShowSkillQueueTrainingTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show warning].
        /// </summary>
        /// <value><c>true</c> if [show warning]; otherwise, <c>false</c>.</value>
        [XmlElement("showWarning")]
        public bool ShowWarning { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show server status].
        /// </summary>
        /// <value><c>true</c> if [show server status]; otherwise, <c>false</c>.</value>
        [XmlElement("showServerStatus")]
        public bool ShowServerStatus { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [show eve time].
        /// </summary>
        /// <value><c>true</c> if [show eve time]; otherwise, <c>false</c>.</value>
        [XmlElement("showEveTime")]
        public bool ShowEveTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [highlight conflicts].
        /// </summary>
        /// <value><c>true</c> if [highlight conflicts]; otherwise, <c>false</c>.</value>
        [XmlElement("highlightConflicts")]
        public bool HighlightConflicts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [indent grouped accounts].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [indent grouped accounts]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("indentGroupedAccounts")]
        public bool IndentGroupedAccounts { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use increased contrast].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [use increased contrast]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("useIncreasedContrast")]
        public bool UseIncreasedContrast { get; set; }
    }
}