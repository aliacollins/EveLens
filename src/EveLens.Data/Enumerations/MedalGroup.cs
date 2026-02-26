// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Enumeration of medal group.
    /// </summary>
    public enum MedalGroup
    {
        [Description("Corporation")]
        Corporation,

        [Description("Current Corporation")]
        CurrentCorporation,

        [Description("Other Corporation")]
        OtherCorporation,
    }
}