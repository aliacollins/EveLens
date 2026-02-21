// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Common.Collections;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models.Collections
{
    public sealed class WalletTransactionsCollection : ReadonlyCollection<WalletTransaction>
    {
        private readonly CCPCharacter m_character;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal WalletTransactionsCollection(CCPCharacter character)
        {
            m_character = character;
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable wallet transactions from the API.</param>
        internal void Import(IEnumerable<SerializableWalletTransactionsListItem> src)
        {
            Items.Clear();

            // Import the wallet transactions from the API
            foreach (SerializableWalletTransactionsListItem srcWalletTransaction in src)
            {
                Items.Add(new WalletTransaction(srcWalletTransaction, m_character));
            }
        }
    }
}
