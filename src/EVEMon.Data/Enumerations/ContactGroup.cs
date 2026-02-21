// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of contact group.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContactGroup
    {
        [Description("Personal")]
        Personal = 0,

        [Description("Corporation")]
        Corporate = 1,

        [Description("Alliance")]
        Alliance = 2,

        [Description("Agents")]
        Agent = 3,
    }
}