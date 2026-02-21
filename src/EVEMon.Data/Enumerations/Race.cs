// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Flags for the races.
    /// </summary>
    [Flags]
    public enum Race
    {
        [XmlEnum("Caldari")]
        Caldari = 1,

        [XmlEnum("Minmatar")]
        Minmatar = 2,

        [XmlEnum("Amarr")]
        Amarr = 4,

        [XmlEnum("Gallente")]
        Gallente = 8,

        [XmlEnum("Jove")]
        Jove = 16,

        [XmlEnum("Faction")]
        Faction = 32,

        [XmlEnum("Sleepers")]
        Sleepers = 64,

        [XmlEnum("ORE")]
        Ore = 128,

        None = 0,
        All = Amarr | Minmatar | Caldari | Gallente | Jove | Faction | Sleepers | Ore
    }
}