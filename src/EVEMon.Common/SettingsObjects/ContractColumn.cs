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
    public enum ContractColumn
    {
        None = -1,

        [Header("Contract")]
        [Description("Contract")]
        ContractText = 0,

        [Header("Title")]
        [Description("Description of Contract")]
        Title = 1,

        [Header("Type")]
        [Description("Contract Type")]
        ContractType = 2,

        [Header("Status")]
        [Description("Contract Status")]
        Status = 3,

        [Header("From")]
        [Description("Issuer")]
        Issuer = 4,

        [Header("To")]
        [Description("Assigned to (Recipient)")]
        Assignee = 5,

        [Header("Accepted by")]
        [Description("Accepted by (Recipient)")]
        Acceptor = 6,

        [Header("Availability")]
        [Description("Availability")]
        Availability = 7,

        [Header("Price")]
        [Description("Price")]
        Price = 8,

        [Header("Buyout")]
        [Description("Buyout")]
        Buyout = 9,

        [Header("Reward")]
        [Description("Reward")]
        Reward = 10,

        [Header("Collateral")]
        [Description("Collateral")]
        Collateral = 11,

        [Header("Volume")]
        [Description("Volume (m³)")]
        Volume = 12,

        [Header("Starting Location")]
        [Description("Starting Location (Full)")]
        StartLocation = 13,

        [Header("Starting Region")]
        [Description("Starting Location (Region)")]
        StartRegion = 14,

        [Header("Starting System")]
        [Description("Starting Location (Solar System)")]
        StartSolarSystem = 15,

        [Header("Starting Station")]
        [Description("Starting Location (Station)")]
        StartStation = 16,

        [Header("Ending Location")]
        [Description("Ending Location (Full)")]
        EndLocation = 17,

        [Header("Ending Region")]
        [Description("Ending Location (Region)")]
        EndRegion = 18,

        [Header("Ending System")]
        [Description("Ending Location (Solar System)")]
        EndSolarSystem = 19,

        [Header("Ending Station")]
        [Description("Ending Location (Station)")]
        EndStation = 20,

        [Header("Issued")]
        [Description("Issue Date")]
        Issued = 21,

        [Header("Accepted")]
        [Description("Accept Date")]
        Accepted = 22,

        [Header("Completed")]
        [Description("Complete Date")]
        Completed = 23,

        [Header("Duration")]
        [Description("Duration")]
        Duration = 24,

        [Header("Days To Complete")]
        [Description("Days To Complete")]
        DaysToComplete = 25,

        [Header("Expires In")]
        [Description("Expires In")]
        Expiration = 26,

        [Header("Issued For")]
        [Description("Issued For")]
        IssuedFor = 27
    }
}
