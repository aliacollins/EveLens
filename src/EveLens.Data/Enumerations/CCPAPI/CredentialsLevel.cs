// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations.CCPAPI
{
    /// <summary>
    /// Enumeration of API credential access levels.
    /// </summary>
    public enum CredentialsLevel
    {
        /// <summary>
        /// Unknown or not determined.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// Limited access credentials.
        /// </summary>
        Limited = 1,

        /// <summary>
        /// Full access credentials.
        /// </summary>
        Full = 2
    }
}
