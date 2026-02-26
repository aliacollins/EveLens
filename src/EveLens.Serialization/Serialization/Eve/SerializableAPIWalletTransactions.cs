// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EveLens.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of wallet transactions. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIWalletTransactions
    {
        private readonly Collection<SerializableWalletTransactionsListItem> m_walletTransactions;

        public SerializableAPIWalletTransactions()
        {
            m_walletTransactions = new Collection<SerializableWalletTransactionsListItem>();
        }

        [XmlArray("transactions")]
        [XmlArrayItem("transaction")]
        public Collection<SerializableWalletTransactionsListItem> WalletTransactions => m_walletTransactions;
    }
}
