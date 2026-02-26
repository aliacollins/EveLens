// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Data;

namespace EveLens.Common.Interfaces
{
    /// <summary>
    /// Represents a static skill and level tuple
    /// </summary>
    public interface ISkillLevel
    {
        long Level { get; }
        StaticSkill Skill { get; }
    }
}