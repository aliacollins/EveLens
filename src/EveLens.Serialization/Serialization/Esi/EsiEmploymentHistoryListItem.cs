// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Eve;
using System;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiEmploymentHistoryListItem
    {
        private DateTime startDate;

        public EsiEmploymentHistoryListItem()
        {
            startDate = DateTime.MinValue;
        }

        [DataMember(Name = "record_id")]
        public int RecordID { get; set; }

        [DataMember(Name = "corporation_id")]
        public long CorporationID { get; set; }

        [DataMember(Name = "is_deleted", IsRequired = false)]
        public bool Closed { get; set; }

        [DataMember(Name = "start_date")]
        public string StartDateJson
        {
            get { return startDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    startDate = value.TimeStringToDateTime();
            }
        }

        [IgnoreDataMember]
        public DateTime StartDate
        {
            get
            {
                return startDate;
            }
            set
            {
                startDate = value;
            }
        }

        public SerializableEmploymentHistoryListItem ToXMLItem()
        {
            return new SerializableEmploymentHistoryListItem()
            {
                RecordID = RecordID,
                CorporationID = (int)CorporationID,
                StartDate = StartDate
            };
        }
    }
}
