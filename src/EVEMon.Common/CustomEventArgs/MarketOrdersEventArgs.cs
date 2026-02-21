// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Models;

namespace EVEMon.Common.CustomEventArgs
{
    public sealed class MarketOrdersEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedOrders">The ended orders.</param>
        public MarketOrdersEventArgs(Character character, IEnumerable<MarketOrder> endedOrders)
        {
            Character = character;
            EndedOrders = endedOrders;
        }

        /// <summary>
        /// Gets the character.
        /// </summary>
        /// <value>The character.</value>
        public Character Character { get; }

        /// <summary>
        /// Gets the ended orders.
        /// </summary>
        /// <value>The ended orders.</value>
        public IEnumerable<MarketOrder> EndedOrders { get; }
    }
}
