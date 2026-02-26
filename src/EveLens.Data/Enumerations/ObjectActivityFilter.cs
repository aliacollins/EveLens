// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Enumeration of blueprint browser activity filter.
    /// </summary>
    public enum ObjectActivityFilter
    {
        Any = 0,
        All = 1,
        Manufacturing = 2,
        Copying = 3,
        ResearchingMaterialEfficiency = 4,
        ResearchingTimeEfficiency = 5,
        Invention = 6
    }
}