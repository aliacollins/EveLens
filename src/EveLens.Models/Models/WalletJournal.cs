// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Eve;
using EveLens.Core;

namespace EveLens.Common.Models
{
    public sealed class WalletJournal
    {
        private readonly long m_taxReceiverID;
        private readonly long m_ownerID1;
        private readonly long m_ownerID2;
        private readonly int m_refTypeID;
        private string m_taxReceiver;
        private string m_ownerName1;
        private string m_ownerName2;
        private string m_refType;


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletJournal" /> class.
        /// </summary>
        /// <param name="src">The SRC.</param>
        /// <exception cref="System.ArgumentNullException">src</exception>
        internal WalletJournal(SerializableWalletJournalListItem src)
        {
            src.ThrowIfNull(nameof(src));

            m_refTypeID = src.RefTypeID;
            m_taxReceiverID = src.TaxReceiverID;

            ID = src.ID;
            Date = src.Date;
            Amount = src.Amount;
            Balance = src.Balance;
            m_ownerID1 = src.OwnerID1;
            m_ownerName1 = ServiceLocator.NameResolver.GetName(m_ownerID1);
            m_ownerID2 = src.OwnerID2;
            m_ownerName2 = ServiceLocator.NameResolver.GetName(m_ownerID2);
            TaxAmount = src.TaxAmount;

            Reason = ParseReason(src.Reason ?? string.Empty);

            // Use the raw ESI ref_type string (humanized) as primary display name.
            // Fall back to the legacy RefTypes.xml mapping only if no raw string available.
            string legacyName = ServiceLocator.NameResolver.GetRefTypeName(src.RefTypeID);
            if (!string.IsNullOrEmpty(src.RawRefType) &&
                (legacyName == "Undefined" || legacyName == "Unknown" || string.IsNullOrEmpty(legacyName)))
            {
                m_refType = HumanizeRefType(src.RawRefType);
            }
            else
            {
                m_refType = legacyName;
            }

            m_taxReceiver = GetTaxReceiver();
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the ID.
        /// </summary>
        public long ID { get; private set; }

        /// <summary>
        /// Gets the date.
        /// </summary>
        public DateTime Date { get; private set; }

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public decimal Amount { get; private set; }

        /// <summary>
        /// Gets the balance.
        /// </summary>
        public decimal Balance { get; private set; }

        /// <summary>
        /// Gets the reason.
        /// </summary>
        public string Reason { get; private set; }

        /// <summary>
        /// Gets the issuer.
        /// </summary>
        public string Issuer => m_ownerName1.IsEmptyOrUnknown() ?
            (m_ownerName1 = ServiceLocator.NameResolver.GetName(m_ownerID1)) : m_ownerName1;

        /// <summary>
        /// Gets the recipient.
        /// </summary>
        public string Recipient => m_ownerName2.IsEmptyOrUnknown() ?
            (m_ownerName2 = ServiceLocator.NameResolver.GetName(m_ownerID2)) : m_ownerName2;

        /// <summary>
        /// Gets the tax amount.
        /// </summary>
        public decimal TaxAmount { get; private set; }

        /// <summary>
        /// Gets the type.
        /// </summary>
        public string Type => m_refType.IsEmptyOrUnknown() ? (m_refType = ServiceLocator.
            NameResolver.GetRefTypeName(m_refTypeID)) : m_refType;

        /// <summary>
        /// Gets the tax receiver.
        /// </summary>
        public string TaxReceiver => m_taxReceiver.IsEmptyOrUnknown() ? (m_taxReceiver =
            GetTaxReceiver()) : m_taxReceiver;

        #endregion


        #region Helper Methods

        /// <summary>
        /// Converts ESI ref_type snake_case string to human-readable title case.
        /// e.g. "player_trading" → "Player Trading", "market_escrow" → "Market Escrow"
        /// </summary>
        private static string HumanizeRefType(string rawRefType)
        {
            if (string.IsNullOrEmpty(rawRefType))
                return "Unknown";

            var words = rawRefType.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                    words[i] = char.ToUpper(words[i][0]) + words[i].Substring(1);
            }
            return string.Join(" ", words);
        }

        /// <summary>
        /// Gets the tax receiver.
        /// </summary>
        /// <returns></returns>
        private string GetTaxReceiver() => (m_taxReceiverID == 0) ? string.Empty :
            ServiceLocator.NameResolver.GetName(m_taxReceiverID);

        /// <summary>
        /// Parses the reason text.
        /// </summary>
        /// <param name="reasonText">The reason text.</param>
        /// <returns></returns>
        // If RefType is of type "Bounty Prizes" return a generic message,
        // otherwise clean the header of a player entered text if it exists
        private string ParseReason(string reasonText) => m_refTypeID == 85 ?
            "Killing NPC entities" : reasonText.Replace("DESC: ", string.Empty);

        #endregion
    }
}
