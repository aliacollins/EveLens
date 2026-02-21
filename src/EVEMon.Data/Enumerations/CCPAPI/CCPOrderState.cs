// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations.CCPAPI
{
    /// <summary>
    /// The status of a market order.
    /// </summary>
    public enum CCPOrderState
    {
        Opened = 0,
        Closed = 1,
        ExpiredOrFulfilled = 2,
        Canceled = 3,
        Pending = 4,
        CharacterDeleted = 5
    }
}