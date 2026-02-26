// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Extensions;
using System.Runtime.Serialization;
using EveLens.Common.Serialization.Eve;

namespace EveLens.Common.Serialization.Esi
{
    [DataContract]
    public sealed class EsiMedalsListItem
    {
        private DateTime issued;

        public EsiMedalsListItem()
        {
            issued = DateTime.MinValue;
        }

        [DataMember(Name = "medal_id")]
        public int MedalID { get; set; }

        [DataMember(Name = "reason")]
        public string? Reason { get; set; }

        // One of: public, private
        [DataMember(Name = "status")]
        public string? Status { get; set; }

        [DataMember(Name = "issuer_id")]
        public int IssuerID { get; set; }

        [DataMember(Name = "date")]
        public string IssuedJson
        {
            get { return issued.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    issued = value.TimeStringToDateTime();
            }
        }
        
        [DataMember(Name = "corporation_id")]
        public int CorporationID { get; set; }

        [DataMember(Name = "title")]
        public string? Title { get; set; }

        [DataMember(Name = "description")]
        public string? Description { get; set; }

        // Medal graphics are not used in EveLens

        [IgnoreDataMember]
        public DateTime Issued
        {
            get
            {
                return issued;
            }
        }
    }
}
