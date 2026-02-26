// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a prerequisite material for a blueprint.
    /// </summary>
    public sealed class SerializableRequiredMaterial
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlAttribute("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the quantity.
        /// </summary>
        /// <value>The quantity.</value>
        [XmlAttribute("quantity")]
        public long Quantity { get; set; }

        /// <summary>
        /// Gets or sets the activity.
        /// </summary>
        /// <value>The activity.</value>
        [XmlAttribute("activityId")]
        public int Activity { get; set; }
    }
}