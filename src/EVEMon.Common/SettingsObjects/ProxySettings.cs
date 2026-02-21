// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.SettingsObjects
{
    public sealed class ProxySettings
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ProxySettings"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        [XmlAttribute("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Gets or sets the host.
        /// </summary>
        /// <value>The host.</value>
        [XmlElement("host")]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port.
        /// </summary>
        /// <value>The port.</value>
        [XmlElement("port")]
        public int Port { get; set; }

        /// <summary>
        /// Gets or sets the authentication.
        /// </summary>
        /// <value>The authentication.</value>
        [XmlElement("authenticationType")]
        public ProxyAuthentication Authentication { get; set; }

        /// <summary>
        /// Gets or sets the username.
        /// </summary>
        /// <value>The username.</value>
        [XmlElement("username")]
        public string Username { get; set; }

        /// <summary>
        /// Gets or sets the password.
        /// </summary>
        /// <value>The password.</value>
        [XmlElement("password")]
        public string Password { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [disable requests on authentication failure].
        /// </summary>
        /// <value>
        /// 	<c>true</c> if [disable requests on authentication failure]; otherwise, <c>false</c>.
        /// </value>
        [XmlElement("disableRequestsOnAuthenticationFailure")]
        public bool DisableRequestsOnAuthenticationFailure { get; set; }
    }
}