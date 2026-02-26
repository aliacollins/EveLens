// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// The status of a market order.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum OrderState
    {
        [Header("Active orders")]
        Active = 0,

        [Header("Canceled orders")]
        Canceled = 1,

        [Header("Expired orders")]
        Expired = 2,

        [Header("Fulfilled orders")]
        Fulfilled = 3,

        [Header("Modified orders")]
        Modified = 4
    }
}