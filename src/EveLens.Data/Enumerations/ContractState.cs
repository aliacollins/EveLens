// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// The status of a contract.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContractState
    {
        [Header("Assigned contracts")]
        Assigned = 0,

        [Header("Created contracts")]
        Created = 1,

        [Header("Canceled contracts")]
        Canceled = 2,

        [Header("Deleted contracts")]
        Deleted = 3,

        [Header("Expired contracts")]
        Expired = 4,

        [Header("Rejected contracts")]
        Rejected = 5,

        [Header("Finished contracts")]
        Finished = 6,

        [Header("Failed contracts")]
        Failed = 7,

        [Header("Unknown contracts")]
        Unknown = 8
    }
}