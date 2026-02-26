// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details


namespace EveLens.Common.EmailProvider
{
    public sealed class HotmailProvider : IEmailProvider
    {
        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public string Name => "Hotmail / Live";

        /// <summary>
        /// Gets the server address.
        /// </summary>
        /// <value>The server address.</value>
        public string ServerAddress => "smtp-mail.outlook.com";

        /// <summary>
        /// Gets the server port.
        /// </summary>
        /// <value>The server port.</value>
        public int ServerPort => 587;

        /// <summary>
        /// Gets a value indicating whether the server requires SSL.
        /// </summary>
        /// <value><c>true</c> if the server requires SSL; otherwise, <c>false</c>.</value>
        public bool RequiresSsl => true;

        /// <summary>
        /// Gets a value indicating whether the server requires authentication.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the server requires authentication ; otherwise, <c>false</c>.
        /// </value>
        public bool RequiresAuthentication => true;
    }
}
