// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Text.Json.Serialization;
using System.Xml.Serialization;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableEmploymentHistory
    {
        [XmlAttribute("corporationID")]
        public long CorporationID { get; set; }

        [XmlAttribute("corporationName")]
        [JsonIgnore]
        public string? CorporationNameXml
        {
            get { return CorporationName; }
            set { CorporationName = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlAttribute("startDate")]
        [JsonIgnore]
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
        [JsonInclude]
        public string? CorporationName { get; set; }

        [XmlIgnore]
        [JsonInclude]
        public DateTime StartDate { get; set; }
    }
}
