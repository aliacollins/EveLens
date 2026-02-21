// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Describes the kind of changes which occurred.
    /// </summary>
    [Flags]
    public enum PlanChange
    {
        None = 0,
        Notification = 1,
        Prerequisites = 2,
        All = Notification | Prerequisites
    }
}