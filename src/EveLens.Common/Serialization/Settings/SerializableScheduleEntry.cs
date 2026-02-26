// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Scheduling;

namespace EveLens.Common.Serialization.Settings
{
    public class SerializableScheduleEntry
    {
        [XmlAttribute("startDate")]
        public DateTime StartDate { get; set; }

        [XmlAttribute("endDate")]
        public DateTime EndDate { get; set; }

        [XmlElement("title")]
        public string? Title { get; set; }

        [XmlElement("options")]
        public ScheduleEntryOptions Options { get; set; }
    }
}