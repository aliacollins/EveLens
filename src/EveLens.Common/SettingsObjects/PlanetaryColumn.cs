// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EveLens.Common.Attributes;

namespace EveLens.Common.SettingsObjects
{
    public enum PlanetaryColumn
    {
        None = -1,

        [Header("State")]
        [Description("Installation State")]
        State = 0,

        [Header("TTC")]
        [Description("Time To Completion (TTC)")]
        TTC = 1,

        [Header("Installation")]
        [Description("Installation Name")]
        TypeName = 2,

        [Header("Install Date")]
        [Description("Installed Time")]
        InstallTime = 3,

        [Header("End Date")]
        [Description("Estimated End Time")]
        EndTime = 4,

        [Header("Planet Type")]
        [Description("Planet Type")]
        PlanetTypeName = 5,

        [Header("Planet")]
        [Description("Planet Name")]
        PlanetName = 6,

        [Header("Location")]
        [Description("Location (Full)")]
        Location = 7,

        [Header("Region")]
        [Description("Location (Region)")]
        Region = 8,

        [Header("System")]
        [Description("Location (Solar System)")]
        SolarSystem = 9,

        [Header("Commodity")]
        [Description("Commodity Name")]
        ContentTypeName = 10,

        [Header("Cycle Time")]
        [Description("Cycle Time")]
        CycleTime = 11,

        [Header("Quantity / Cycle")]
        [Description("Quantity Per Cycle")]
        QuantityPerCycle = 12,

        [Header("Quantity")]
        [Description("Quantity")]
        Quantity = 13,

        [Header("Volume")]
        [Description("Volume (m³)")]
        Volume = 14,

        [Header("Linked to")]
        [Description("Linked to")]
        LinkedTo = 15,

        [Header("Routed to")]
        [Description("Routed to")]
        RoutedTo = 16,

        [Header("Type Group")]
        [Description("Type Group Name")]
        GroupName = 17
    }
}
