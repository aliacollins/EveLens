// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.ObjectModel;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.Eve
{
    /// <summary>
    /// Represents a serializable version of wallet journal. Used for querying CCP.
    /// </summary>
    public sealed class SerializableAPIWalletJournal
    {
        private readonly Collection<SerializableWalletJournalListItem> m_walletJournal;

        public SerializableAPIWalletJournal()
        {
            m_walletJournal = new Collection<SerializableWalletJournalListItem>();
        }

        [XmlArray("transactions")]
        [XmlArrayItem("transaction")]
        public Collection<SerializableWalletJournalListItem> WalletJournal => m_walletJournal;
    }
}
