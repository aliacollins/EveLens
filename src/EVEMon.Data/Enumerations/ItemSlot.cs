// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Flags for the items slots.
    /// </summary>
    [Flags]
    public enum ItemSlot
    {
        None = 0,
        NoSlot = 1,
        Low = 2,
        Medium = 4,
        High = 8,

        All = Low | Medium | High | NoSlot
    }
}