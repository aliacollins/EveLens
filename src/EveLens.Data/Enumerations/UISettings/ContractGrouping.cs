// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the contracts to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContractGrouping
    {
        [Header("Group by contract state")]
        State = 0,

        [Header("Group by contract state (Desc)")]
        StateDesc = 1,

        [Header("Group by contract type")]
        ContractType = 2,

        [Header("Group by contract type (Desc)")]
        ContractTypeDesc = 3,

        [Header("Group by issue day")]
        Issued = 4,

        [Header("Group by issue day (Desc)")]
        IssuedDesc = 5,

        [Header("Group by starting station")]
        StartLocation = 6,

        [Header("Group by starting station (Desc)")]
        StartLocationDesc = 7
    }
}