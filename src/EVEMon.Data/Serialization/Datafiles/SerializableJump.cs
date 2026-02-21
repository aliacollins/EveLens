// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Datafiles
{
    /// <summary>
    /// Represents a connection between two jump gates.
    /// </summary>
    public sealed class SerializableJump
    {
        /// <summary>
        /// Gets or sets the first system ID.
        /// </summary>
        /// <value>The first system ID.</value>
        [XmlAttribute("id1")]
        public int FirstSystemID { get; set; }

        /// <summary>
        /// Gets or sets the second system ID.
        /// </summary>
        /// <value>The second system ID.</value>
        [XmlAttribute("id2")]
        public int SecondSystemID { get; set; }
    }
}