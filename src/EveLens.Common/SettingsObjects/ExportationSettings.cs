// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.SettingsObjects
{
    public sealed class ExportationSettings
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ExportationSettings"/> class.
        /// </summary>
        public ExportationSettings()
        {
            PlanToText = new PlanExportSettings();
        }

        /// <summary>
        /// Gets or sets the plan to text.
        /// </summary>
        /// <value>The plan to text.</value>
        [XmlElement("planToText")]
        public PlanExportSettings PlanToText { get; set; }
    }
}