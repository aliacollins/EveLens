// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Represents the status of the Internet connection.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Everything normal, we're online
        /// </summary>
        Online,

        /// <summary>
        /// The user requested to stay offline after connection failures
        /// </summary>
        Offline,

        /// <summary>
        /// The connection has not been tested yet
        /// </summary>
        Unknown
    }
}