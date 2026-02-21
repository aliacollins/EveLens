// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.SettingsObjects
{
    /// <summary>
    /// Represents the available column types.
    /// </summary>
    public enum MarketOrderColumn
    {
        None = -1,

        [Header("Item")]
        [Description("Item")]
        Item = 0,

        [Header("Type")]
        [Description("Item Type")]
        ItemType = 1,

        [Header("Location")]
        [Description("Location (Full)")]
        Location = 2,

        [Header("Region")]
        [Description("Location (Region)")]
        Region = 3,

        [Header("System")]
        [Description("Location (Solar System)")]
        SolarSystem = 4,

        [Header("Station")]
        [Description("Location (Station)")]
        Station = 5,

        [Header("Unit Price")]
        [Description("Unit Price (ISK)")]
        UnitaryPrice = 6,

        [Header("Total Price")]
        [Description("Total Price (ISK)")]
        TotalPrice = 7,

        [Header("Issued")]
        [Description("Issue Date")]
        Issued = 8,

        [Header("Issued For")]
        [Description("Issued For")]
        IssuedFor = 9,

        [Header("Expires In")]
        [Description("Expires In")]
        Expiration = 10,

        [Header("Duration")]
        [Description("Duration")]
        Duration = 11,

        [Header("Quantity")]
        [Description("Quantity (Remaining / Initial)")]
        Volume = 12,

        [Header("Min")]
        [Description("Quantity (Minimum)")]
        MinimumVolume = 13,

        [Header("Remaining")]
        [Description("Quantity (Remaining)")]
        RemainingVolume = 14,

        [Header("Initial")]
        [Description("Quantity (Initial)")]
        InitialVolume = 15,

        [Header("Last Change")]
        [Description("Last Order State Change")]
        LastStateChange = 16,

        [Header("Range")]
        [Description("Order Range")]
        OrderRange = 17,

        [Header("Escrow")]
        [Description("Escrow (ISK)")]
        Escrow = 18
    }
}