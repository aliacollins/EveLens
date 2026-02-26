// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.CCPAPI
{
    /// <summary>
    /// Represents the category of error which can occur with the API.
    /// </summary>
    public enum APIErrorType
    {
        /// <summary>
        /// There was no error.
        /// </summary>
        None,

        /// <summary>
        /// The error was caused by the network.
        /// </summary>
        Http,

        /// <summary>
        /// The error occurred during JSON decoding.
        /// </summary>
        Json,

        /// <summary>
        /// The error occurred during XML decoding.
        /// </summary>
        Xml,

        /// <summary>
        /// It was a managed CCP error.
        /// </summary>
        CCP
    }
}
