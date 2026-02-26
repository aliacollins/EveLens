// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Attributes;

namespace EveLens.Common.SettingsObjects
{
    /// <summary>
    /// Represents the available column types.
    /// </summary>
    public enum IndustryJobColumn
    {
        None = -1,

        [Header("State")]
        [Description("Job State")]
        State = 0,

        [Header("TTC")]
        [Description("Time To Completion (TTC)")]
        TTC = 1,

        [Header("Installed Item")]
        [Description("Installed Item (Blueprint)")]
        InstalledItem = 2,

        [Header("Installed Group")]
        [Description("Installed Item Group")]
        InstalledItemType = 3,

        [Header("Output Item")]
        [Description("Output Item")]
        OutputItem = 4,

        [Header("Output Group")]
        [Description("Output Item Group")]
        OutputItemType = 5,

        [Header("Install Date")]
        [Description("Installed Time")]
        InstallTime = 6,

        [Header("End Date")]
        [Description("Estimated End Time")]
        EndTime = 7,

        [Header("Location")]
        [Description("Location (Full)")]
        Location = 8,

        [Header("Region")]
        [Description("Location (Region)")]
        Region = 9,

        [Header("System")]
        [Description("Location (Solar System)")]
        SolarSystem = 10,

        [Header("Installation")]
        [Description("Location (Installation)")]
        Installation = 11,

        [Header("Activity")]
        [Description("Activity")]
        Activity = 12,

        [Header("Issued For")]
        [Description("Issued For")]
        IssuedFor = 13,

        [Header("Installed Item Type")]
        [Description("Installed Item Type (Original or Copy)")]
        OriginalOrCopy = 14,

        [Header("Last Change")]
        [Description("Last Job State Change")]
        LastStateChange = 15,

        [Header("Installed ME")]
        [Description("Installed Material Efficiency Level")]
        InstalledME = 16,

        [Header("End ME")]
        [Description("End Material Efficiency Level")]
        EndME = 17,

        [Header("Installed TE")]
        [Description("Installed Time Efficiency Level")]
        InstalledPE = 18,

        [Header("End TE")]
        [Description("End Time Efficiency Level")]
        EndPE = 19,

        [Header("Cost")]
        [Description("Cost (ISK)")]
        Cost = 20,

        [Header("Probability")]
        [Description("Probability (%)")]
        Probability = 21,

        [Header("Runs")]
        [Description("Runs")]
        Runs = 22,
    }
}