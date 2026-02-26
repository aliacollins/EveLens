// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using System;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public class EsiCalendarEventListItem
    {
        private DateTime eventDate;

        public EsiCalendarEventListItem()
        {
            eventDate = DateTime.MinValue;
        }

        [DataMember(Name = "event_id")]
        public long EventID { get; set; }

        [DataMember(Name = "title", EmitDefaultValue = false, IsRequired = false)]
        public string? EventTitle { get; set; }

        [DataMember(Name = "importance", EmitDefaultValue = false, IsRequired = false)]
        public int Importance { get; set; }

        [DataMember(Name = "event_date", EmitDefaultValue = false, IsRequired = false)]
        private string EventDateJson
        {
            get { return eventDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    eventDate = value.TimeStringToDateTime();
            }
        }

        [IgnoreDataMember]
        public DateTime EventDate
        {
            get
            {
                return eventDate;
            }
        }

        // One of: declined, not_responded, accepted, tentative
        [DataMember(Name = "event_response", EmitDefaultValue = false, IsRequired = false)]
        public string? Response { get; set; }
    }
}
