// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations.CCPAPI
{
    /// <summary>
    /// Enumeration of API key types.
    /// </summary>
    public enum CCPAPIKeyType
    {
        /// <summary>
        /// The API key type wouldn't be checked because of an error.
        /// </summary>
        Unknown = 0,

        /// <summary>
        /// This is a character wide ESI key.
        /// </summary>
        Character = 2
    }
}
