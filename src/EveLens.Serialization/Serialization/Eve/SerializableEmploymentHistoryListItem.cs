// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableEmploymentHistoryListItem
    {
        [XmlAttribute("recordID")]
        public long RecordID { get; set; }

        [XmlAttribute("corporationID")]
        public int CorporationID { get; set; }

        [XmlAttribute("corporationName")]
        public string? CorporationName { get; set; }

        [XmlAttribute("startDate")]
        public string StartDateXml
        {
            get { return StartDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    StartDate = value.TimeStringToDateTime();
            }
        }

        [XmlIgnore]
        public DateTime StartDate { get; set; }
    }
}