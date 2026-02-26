// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Minmatar Militia ranking.
    /// </summary>
    public enum MinmatarMilitiaRank
    {
        [Description("Nation Warrior")]
        NationWarrior = 0,

        [Description("Spike Lieutenant")]
        SpikeLieutenant = 1,

        [Description("Spear Lieutenant")]
        SpearLieutenant = 2,

        [Description("Venge Captain")]
        VengeCaptain = 3,

        [Description("Lance Commander")]
        LanceCommander = 4,

        [Description("Blade Commander")]
        BladeCommander = 5,

        [Description("Talon Commander")]
        TalonCommander = 6,

        [Description("Voshud Major")]
        VoshudMajor = 7,

        [Description("Matar Colonel")]
        MatarGeneral = 8,

        [Description("Valklear General")]
        ValklearGeneral = 9,
    }
}