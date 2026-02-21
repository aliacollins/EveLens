// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The policy to apply when removing obsolete entries from a plan.
    /// </summary>
    public enum ObsoleteRemovalPolicy
    {
        None = 0,
        RemoveAll = 1,
        ConfirmedOnly = 2
    }
}