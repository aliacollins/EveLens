// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the contacts to be grouped by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContactGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by contact group")]
        ContactGroup = 1,

        [Header("Group by standing bracket")]
        StandingBracket = 2
    }
}
