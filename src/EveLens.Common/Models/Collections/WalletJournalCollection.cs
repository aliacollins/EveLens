// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Eve;

namespace EveLens.Common.Models.Collections
{
    public sealed class WalletJournalCollection : ReadonlyCollection<WalletJournal>
    {
        private readonly CCPCharacter m_character;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal WalletJournalCollection(CCPCharacter character)
        {
            m_character = character;
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable wallet journal from the API.</param>
        internal void Import(IEnumerable<SerializableWalletJournalListItem> src)
        {
            Items.Clear();

            // Import the wallet journal from the API
            foreach (SerializableWalletJournalListItem srcWalletJournal in src)
            {
                Items.Add(new WalletJournal(srcWalletJournal));
            }
        }
    }
}