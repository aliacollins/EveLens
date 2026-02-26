// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Amarr Militia ranking.
    /// </summary>
    public enum AmarrMilitiaRank
    {
        [Description("Paladin Crusader")]
        PaladinCrusader = 0,

        [Description("Templar Lieutenant")]
        TemplarLieutenant = 1,

        [Description("Cardinal Lieutenant")]
        CardinalLieutenant = 2,

        [Description("Arch Lieutenant")]
        ArchLieutenant = 3,

        [Description("Imperial Major")]
        ImperialMajor = 4,

        [Description("Marshal Commander")]
        MarshalCommander = 5,

        [Description("Imperator Commander")]
        ImperatorCommander = 6,

        [Description("Tribunus Colonel")]
        TribunusColonel = 7,

        [Description("Legatus Commodore")]
        LegatusCommodore = 8,

        [Description("Divine Commodore")]
        DivineCommodore = 9,
    }
}