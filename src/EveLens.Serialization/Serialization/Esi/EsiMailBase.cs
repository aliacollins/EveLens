// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EveLens.Common.Serialization.Esi
{
    /// <summary>
    /// The base class of MailBody and MailMessages which has their shared parameters.
    /// </summary>
    [DataContract]
    public abstract class EsiMailBase
    {
        private DateTime sentDate;

        public EsiMailBase()
        {
            sentDate = DateTime.MinValue;
        }

        [DataMember(Name = "from")]
        public long SenderID { get; set; }

        [DataMember(Name = "timestamp")]
        private string SentDateJson
        {
            get { return sentDate.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    sentDate = value.TimeStringToDateTime();
            }
        }

        [DataMember(Name = "subject", EmitDefaultValue = false, IsRequired = false)]
        public string? Title { get; set; }

        [DataMember(Name = "labels", EmitDefaultValue = false, IsRequired = false)]
        public List<long> Labels { get; set; } = new();

        [DataMember(Name = "recipients")]
        public List<EsiMailRecipientListItem> Recipients { get; set; } = new();

        [IgnoreDataMember]
        public DateTime SentDate
        {
            get
            {
                return sentDate;
            }
        }
    }
}
