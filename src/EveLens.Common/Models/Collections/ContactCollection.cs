// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Models.Collections
{
    public sealed class ContactCollection : ReadonlyCollection<Contact>
    {
        private readonly CCPCharacter m_character;
        
        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="character">The character.</param>
        internal ContactCollection(CCPCharacter character)
        {
            m_character = character;
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The enumeration of serializable contacts from the API.</param>
        internal void Import(IEnumerable<EsiContactListItem> src)
        {
            Items.Clear();

            // Import the contacts from the API
            foreach (EsiContactListItem srcContact in src)
                Items.Add(new Contact(srcContact));
        }
    }
}
