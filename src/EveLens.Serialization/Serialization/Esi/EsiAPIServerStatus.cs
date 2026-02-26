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
    /// <summary>
    /// Represents a serializable version of a server status. Used for querying CCP.
    /// </summary>
    [DataContract]
    public sealed class EsiAPIServerStatus
    {
        private DateTime startDate;

        public EsiAPIServerStatus()
        {
            startDate = DateTime.MinValue;
        }

        [DataMember(Name = "players")]
        public int Players { get; set; }

        [DataMember(Name = "start_time")]
        private string StartTimeJson
        {
            get { return startDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    startDate = value.TimeStringToDateTime();
            }
        }

        [IgnoreDataMember]
        public DateTime StartTime
        {
            get
            {
                return startDate;
            }
        }

        [DataMember(Name = "server_version")]
        public string? Version { get; set; }

        [DataMember(Name = "vip", IsRequired = false)]
        public bool VIP { get; set; }
    }
}
