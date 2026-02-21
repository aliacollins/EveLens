// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.PatchXml
{
    public sealed class SerializableDatafile
    {
        /// <summary>
        /// Gets or sets the name.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [XmlElement("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        [XmlElement("date")]
        public string? Date { get; set; }

        /// <summary>
        /// Gets or sets the md5 sum.
        /// </summary>
        /// <value>
        /// The md5 sum.
        /// </value>
        [XmlElement("md5")]
        public string? MD5Sum { get; set; }

        /// <summary>
        /// Gets or sets the address.
        /// </summary>
        /// <value>
        /// The address.
        /// </value>
        [XmlElement("url")]
        public string? Address { get; set; }

        /// <summary>
        /// Gets or sets the message XML.
        /// </summary>
        /// <value>
        /// The message XML.
        /// </value>
        /// <exception cref="System.ArgumentNullException">value</exception>
        [XmlElement("message")]
        public XmlCDataSection MessageXml
        {
            get { return new XmlDocument().CreateCDataSection(Message); }
            set
            {
                value.ThrowIfNull(nameof(value));

                Message = value.Data;
            }
        }

        /// <summary>
        /// Gets or sets the message.
        /// </summary>
        /// <value>
        /// The message.
        /// </value>
        [XmlIgnore]
        public string? Message { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is downloaded.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is downloaded; otherwise, <c>false</c>.
        /// </value>
        [XmlIgnore]
        public bool IsDownloaded { get; set; }
    }
}