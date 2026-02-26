// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;
using EveLens.Common.Enumerations.UISettings;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Represents the set of options for plan exporting
    /// </summary>
    public class PlanExportSettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether [include header].
        /// </summary>
        /// <value><c>true</c> if [include header]; otherwise, <c>false</c>.</value>
        [XmlAttribute("includeHeader")]
        public bool IncludeHeader { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry number].
        /// </summary>
        /// <value><c>true</c> if [entry number]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryNumber")]
        public bool EntryNumber { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry training times].
        /// </summary>
        /// <value><c>true</c> if [entry training times]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryTrainingTimes")]
        public bool EntryTrainingTimes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry start date].
        /// </summary>
        /// <value><c>true</c> if [entry start date]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryStartDate")]
        public bool EntryStartDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry finish date].
        /// </summary>
        /// <value><c>true</c> if [entry finish date]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryFinishDate")]
        public bool EntryFinishDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry notes].
        /// </summary>
        /// <value><c>true</c> if [entry notes]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryNotes")]
        public bool EntryNotes { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [entry cost].
        /// </summary>
        /// <value><c>true</c> if [entry cost]; otherwise, <c>false</c>.</value>
        [XmlAttribute("entryCost")]
        public bool EntryCost { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [remapping points].
        /// </summary>
        /// <value><c>true</c> if [remapping points]; otherwise, <c>false</c>.</value>
        [XmlAttribute("remappingPoints")]
        public bool RemappingPoints { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [footer count].
        /// </summary>
        /// <value><c>true</c> if [footer count]; otherwise, <c>false</c>.</value>
        [XmlAttribute("footerCount")]
        public bool FooterCount { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [footer total time].
        /// </summary>
        /// <value><c>true</c> if [footer total time]; otherwise, <c>false</c>.</value>
        [XmlAttribute("footerTotalTime")]
        public bool FooterTotalTime { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [footer date].
        /// </summary>
        /// <value><c>true</c> if [footer date]; otherwise, <c>false</c>.</value>
        [XmlAttribute("footerDate")]
        public bool FooterDate { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [footer cost].
        /// </summary>
        /// <value><c>true</c> if [footer cost]; otherwise, <c>false</c>.</value>
        [XmlAttribute("footerCost")]
        public bool FooterCost { get; set; }

        /// <summary>
        /// Output markup type.
        /// </summary>
        [XmlElement("markup")]
        public MarkupType Markup { get; set; }

        /// <summary>
        /// If <code>true</code>, known skills are filtered out.
        /// </summary>
        [XmlElement("shoppingList")]
        public bool ShoppingList { get; set; }
    }
}