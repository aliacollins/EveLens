// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration of portrait sizes
    /// </summary>
    public enum PortraitSizes
    {
        [DefaultValue(16)]
        x16 = 0,

        [DefaultValue(24)]
        x24 = 1,

        [DefaultValue(32)]
        x32 = 2,

        [DefaultValue(48)]
        x48 = 3,

        [DefaultValue(64)]
        x64 = 4,

        [DefaultValue(80)]
        x80 = 5,

        [DefaultValue(96)]
        x96 = 6
    }
}