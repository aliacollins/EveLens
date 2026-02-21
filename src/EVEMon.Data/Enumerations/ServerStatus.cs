// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents a server status.
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// The server is offline
        /// </summary>
        Offline,

        /// <summary>
        /// The server is online
        /// </summary>
        Online,

        /// <summary>
        /// The API couldn't be queried or has not been queried yet.
        /// </summary>
        Unknown,

        /// <summary>
        /// The server's status checks have been disabled.
        /// </summary>
        CheckDisabled
    }
}