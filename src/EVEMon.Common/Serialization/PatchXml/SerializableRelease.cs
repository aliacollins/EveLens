// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml;
using System.Xml.Serialization;
using EVEMon.Common.Extensions;

namespace EVEMon.Common.Serialization.PatchXml
{
    public sealed class SerializableRelease
    {
        /// <summary>
        /// Gets or sets the date.
        /// </summary>
        /// <value>
        /// The date.
        /// </value>
        [XmlElement("date")]
        public string? Date { get; set; }

        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        [XmlElement("version")]
        public string? Version { get; set; }

        /// <summary>
        /// Gets or sets the md5 sum.
        /// </summary>
        /// <value>
        /// The md5 sum.
        /// </value>
        [XmlElement("md5")]
        public string? MD5Sum { get; set; }

        /// <summary>
        /// Gets or sets the topic address.
        /// </summary>
        /// <value>
        /// The topic address.
        /// </value>
        [XmlElement("url")]
        public string? TopicAddress { get; set; }

        /// <summary>
        /// Gets or sets the patch address.
        /// </summary>
        /// <value>
        /// The patch address.
        /// </value>
        [XmlElement("autopatchurl")]
        public string? PatchAddress { get; set; }

        /// <summary>
        /// Gets or sets the installer arguments.
        /// </summary>
        /// <value>
        /// The installer arguments.
        /// </value>
        [XmlElement("autopatchargs")]
        public string? InstallerArgs { get; set; }

        /// <summary>
        /// Gets or sets the additional arguments.
        /// </summary>
        /// <value>
        /// The additional arguments.
        /// </value>
        [XmlElement("additionalargs")]
        public string? AdditionalArgs { get; set; }

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
    }
}