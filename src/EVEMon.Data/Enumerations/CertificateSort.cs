// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    public enum CertificateSort
    {
        [Description("No Sorting")]
        None = 0,

        [Description("Time to Next Level")]
        TimeToNextLevel = 1,

        [Description("Time to Max Level")]
        TimeToMaxLevel = 2,
    }
}
