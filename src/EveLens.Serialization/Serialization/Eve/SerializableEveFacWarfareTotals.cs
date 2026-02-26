// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableEveFacWarfareTotals
    {
        [XmlElement("killsYesterday")]
        public int KillsYesterday { get; set; }

        [XmlElement("killsLastWeek")]
        public int KillsLastWeek { get; set; }

        [XmlElement("killsTotal")]
        public int KillsTotal { get; set; }

        [XmlElement("victoryPointsYesterday")]
        public int VictoryPointsYesterday { get; set; }

        [XmlElement("victoryPointsLastWeek")]
        public int VictoryPointsLastWeek { get; set; }

        [XmlElement("victoryPointsTotal")]
        public int VictoryPointsTotal { get; set; }
    }
}