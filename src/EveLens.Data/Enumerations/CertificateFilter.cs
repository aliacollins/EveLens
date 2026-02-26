// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations
{
    public enum CertificateFilter
    {
        [Description("All")]
        All = 0,

        [Description("Completed")]
        Completed = 1,

        [Description("Hide Completed")]
        HideMaxLevel = 2,

        [Description("Trrainable Next Level")]
        NextLevelTrainable = 3,

        [Description("Untrainable Next Level")]
        NextLevelUntrainable = 4
    }
}
