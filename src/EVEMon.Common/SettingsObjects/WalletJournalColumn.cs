// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.SettingsObjects
{
    public enum WalletJournalColumn
    {
        None = -1,

        [Header("Date")]
        [Description("Date")]
        Date = 0,

        [Header("Type")]
        [Description("Type (of transaction)")]
        Type = 1,

        [Header("Amount")]
        [Description("Amount")]
        Amount = 2,

        [Header("Balance")]
        [Description("Balance (ISK)")]
        Balance = 3,

        [Header("Reason")]
        [Description("Reason")]
        Reason = 4,

        [Header("From")]
        [Description("From (Issuer)")]
        Issuer = 5,

        [Header("To")]
        [Description("To (Recipient)")]
        Recipient = 6,

        [Header("Tax Receiver")]
        [Description("Tax Receiver")]
        TaxReceiver = 7,

        [Header("Tax Amount")]
        [Description("Tax Amount (ISK)")]
        TaxAmount = 8,

        [Header("ID")]
        [Description("ID (of transaction)")]
        ID = 9
    }
}
