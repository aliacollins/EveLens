// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    public sealed class SerializableProperty
    {
        /// <summary>
        /// Gets or sets the ID.
        /// </summary>
        /// <value>The ID.</value>
        [XmlElement("id")]
        public int ID { get; set; }

        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>The name.</value>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        /// <value>The description.</value>
        [XmlElement("description")]
        public string? Description { get; set; }

        /// <summary>
        /// Gets or sets the default value.
        /// </summary>
        /// <value>The default value.</value>
        [XmlElement("defaultValue")]
        public string? DefaultValue { get; set; }

        /// <summary>
        /// Gets or sets the icon.
        /// </summary>
        /// <value>The icon.</value>
        [XmlElement("icon")]
        public string? Icon { get; set; }

        /// <summary>
        /// Gets or sets the unit.
        /// </summary>
        /// <value>The unit.</value>
        [XmlElement("unit")]
        public string? Unit { get; set; }

        /// <summary>
        /// Gets or sets the unit ID.
        /// </summary>
        /// <value>The unit ID.</value>
        [XmlElement("unitID")]
        public long UnitID { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [higher is better].
        /// </summary>
        /// <value><c>true</c> if [higher is better]; otherwise, <c>false</c>.</value>
        [XmlElement("higherIsBetter")]
        public bool HigherIsBetter { get; set; }
    }
}