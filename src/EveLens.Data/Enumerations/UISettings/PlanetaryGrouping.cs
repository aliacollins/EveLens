// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Attributes;

namespace EveLens.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the planetary colonies to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum PlanetaryGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by colony")]
        Colony = 1,

        [Header("Group by colony (Desc)")]
        ColonyDesc = 2,

        [Header("Group by solar system")]
        SolarSystem = 3,

        [Header("Group by solar system (Desc)")]
        SolarSystemDesc = 4,

        [Header("Group by planet type")]
        PlanetType = 5,

        [Header("Group by planet type (Desc)")]
        PlanetTypeDesc = 6,

        [Header("Group by ending date")]
        EndDate = 7,

        [Header("Group by ending date (Desc)")]
        EndDateDesc = 8,

        [Header("Group by type group")]
        GroupName = 9,

        [Header("Group by type group (Desc)")]
        GroupNameDesc = 10,
    }
}