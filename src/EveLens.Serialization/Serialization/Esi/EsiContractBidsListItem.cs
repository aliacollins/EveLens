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
    public sealed class EsiContractBidsListItem
    {
        private DateTime bidDate;

        public EsiContractBidsListItem()
        {
            bidDate = DateTime.MinValue;
        }

        [DataMember(Name = "bid_id")]
        public int ID { get; set; }

        [DataMember(Name = "bidder_id")]
        public long BidderID { get; set; }

        [DataMember(Name = "date_bid")]
        private string DateBidJson
        {
            get
            {
                return bidDate.DateTimeToTimeString();
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    bidDate = value.TimeStringToDateTime();
            }
        }

        [DataMember(Name = "amount")]
        public decimal Amount { get; set; }

        [IgnoreDataMember]
        public DateTime DateBid
        {
            get
            {
                return bidDate;
            }
        }
    }
}
