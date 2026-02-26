// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Serialization.Esi;
using EveLens.Core;

namespace EveLens.Common.Models
{
    public sealed class ContractBid
    {
        private readonly long m_bidderId;
        private string m_bidder;


        #region Constructor

        /// <summary>
        /// Constructor from the API.
        /// </summary>
        /// <param name="src">The source.</param>
        /// <exception cref="System.ArgumentNullException">src</exception>
        internal ContractBid(EsiContractBidsListItem src)
        {
            src.ThrowIfNull(nameof(src));

            ID = src.ID;
            m_bidderId = src.BidderID;
            m_bidder = ServiceLocator.NameResolver.GetName(src.BidderID);
            BidDate = src.DateBid;
            Amount = src.Amount;
        }
        
        #endregion


        #region Properties

        /// <summary>
        /// Gets the ID.
        /// </summary>
        public long ID { get; }
        
        /// <summary>
        /// Gets the bidder.
        /// </summary>
        public string Bidder => m_bidder.IsEmptyOrUnknown() ? (m_bidder = ServiceLocator.
            NameResolver.GetName(m_bidderId)) : m_bidder;

        /// <summary>
        /// Gets the bid date.
        /// </summary>
        public DateTime BidDate { get; }

        /// <summary>
        /// Gets the amount.
        /// </summary>
        public decimal Amount { get; }

        #endregion

    }
}
