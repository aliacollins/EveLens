// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Serialization.Eve;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EVEMon.Common.Serialization.Esi
{
    [CollectionDataContract]
    public sealed class EsiAPIWalletJournal : List<EsiWalletJournalListItem>
    {
        public SerializableAPIWalletJournal ToXMLItem()
        {
            var ret = new SerializableAPIWalletJournal();
            foreach (var entry in this)
                ret.WalletJournal.Add(entry.ToXMLItem());
            return ret;
        }
    }
}
