// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Represents the metagroup of an item.
    /// </summary>
    [Flags]
    public enum ItemMetaGroup
    {
        T1 = 2,
        T2 = 4,
        T3 = 8,
        Faction = 16,
        Officer = 32,
        Deadspace = 64,
        Storyline = 128,

        None = 0,
        AllTechLevel = T1 | T2 | T3,
        AllNonTechLevel = Faction | Officer | Deadspace | Storyline,
        All = AllTechLevel | AllNonTechLevel
    }
}