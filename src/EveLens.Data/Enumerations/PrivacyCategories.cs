// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Flags enum for per-category privacy masking.
    /// Each flag controls whether a category of sensitive data is hidden in the UI.
    /// </summary>
    [Flags]
    public enum PrivacyCategories
    {
        None = 0,
        Name = 1,
        CorpAlliance = 2,
        Balance = 4,
        SkillPoints = 8,
        Training = 16,
        Remaps = 32,
        All = Name | CorpAlliance | Balance | SkillPoints | Training | Remaps
    }
}
