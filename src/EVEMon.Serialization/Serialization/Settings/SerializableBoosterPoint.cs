// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Settings
{
    /// <summary>
    /// Represents a booster injection point in a skill plan.
    /// </summary>
    public sealed class SerializableBoosterPoint
    {
        [XmlAttribute("bonus")]
        public int Bonus { get; set; }

        [XmlAttribute("durationHours")]
        public int DurationHours { get; set; }
    }
}
