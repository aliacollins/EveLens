// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Models
{
    /// <summary>
    /// Stores the automatically determined account status of a character - Omega or Alpha.
    /// </summary>
    public enum AccountStatus
    {
        Unknown, Alpha, Omega
    }

    /// <summary>
    /// Stores the manually set account status of a character.
    /// </summary>
    public enum AccountStatusMode
    {
        Auto, Alpha, Omega
    }
}
