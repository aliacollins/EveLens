// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the wallet journal to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum WalletJournalGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by date")]
        Date = 1,

        [Header("Group by date (Desc)")]
        DateDesc = 2,

        [Header("Group by type")]
        Type = 3,

        [Header("Group by type (Desc)")]
        TypeDesc = 4,

        [Header("Group by issuer")]
        Issuer = 5,

        [Header("Group by issuer (Desc)")]
        IssuerDesc = 6,

        [Header("Group by recipient")]
        Recipient = 7,

        [Header("Group by recipient (Desc)")]
        RecipientDesc = 8
    }
}