// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Xml.Serialization;
using EveLens.Common.Constants;
using EveLens.Common.Extensions;

namespace EveLens.Common.Serialization.Eve
{
    public sealed class SerializableWalletJournalListItem
    {
        [XmlAttribute("refID")]
        public long ID { get; set; }

        [XmlAttribute("refTypeID")]
        public int RefTypeID { get; set; }

        /// <summary>
        /// Raw ref_type string from ESI (e.g. "player_trading", "market_escrow").
        /// Used as display fallback when the legacy RefTypes.xml mapping is stale.
        /// </summary>
        [XmlAttribute("rawRefType")]
        public string? RawRefType { get; set; }

        [XmlAttribute("ownerID1")]
        public long OwnerID1 { get; set; }

        [XmlAttribute("ownerName1")]
        public string? OwnerName1Xml
        {
            get { return OwnerName1; }
            set { OwnerName1 = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlAttribute("ownerID2")]
        public long OwnerID2 { get; set; }

        [XmlAttribute("ownerName2")]
        public string? OwnerName2Xml
        {
            get { return OwnerName2; }
            set { OwnerName2 = value?.HtmlDecode() ?? string.Empty; }
        }

        [XmlAttribute("argID1")]
        public long ArgID1 { get; set; }

        [XmlAttribute("argName1")]
        public string? ArgName1 { get; set; }

        [XmlAttribute("amount")]
        public decimal Amount { get; set; }

        [XmlAttribute("balance")]
        public decimal Balance { get; set; }

        [XmlAttribute("reason")]
        public string? Reason { get; set; }

        [XmlAttribute("taxReceiverID")]
        public string TaxReceiverIDXml
        {
            get { return TaxReceiverID.ToString(CultureConstants.InvariantCulture); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    TaxReceiverID = Convert.ToInt64(value, CultureConstants.InvariantCulture);
            }
        }

        [XmlAttribute("taxAmount")]
        public string TaxAmountXml
        {
            get { return TaxAmount.ToString(CultureConstants.InvariantCulture); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    TaxAmount = Convert.ToDecimal(value, CultureConstants.InvariantCulture);
            }
        }

        [XmlAttribute("date")]
        public string DateXml
        {
            get { return Date.DateTimeToTimeString(); }
            set
            {
                if (!string.IsNullOrEmpty(value))
                    Date = value.TimeStringToDateTime();
            }
        }

        [XmlIgnore]
        public string? OwnerName1 { get; set; }

        [XmlIgnore]
        public string? OwnerName2 { get; set; }

        [XmlIgnore]
        public DateTime Date { get; set; }

        [XmlIgnore]
        public long TaxReceiverID { get; set; }

        [XmlIgnore]
        public decimal TaxAmount { get; set; }
    }
}