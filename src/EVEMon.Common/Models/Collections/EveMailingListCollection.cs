// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Common.Collections;
using EVEMon.Common.Serialization.Esi;
using EVEMon.Core;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Models.Collections
{
    public sealed class EveMailingListCollection : ReadonlyCollection<EveMailingList>
    {
        private readonly CCPCharacter m_ccpCharacter;

        /// <summary>
        /// Internal constructor.
        /// </summary>
        /// <param name="ccpCharacter">The CCP character.</param>
        internal EveMailingListCollection(CCPCharacter ccpCharacter)
        {
            m_ccpCharacter = ccpCharacter;
        }

        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The source.</param>
        internal void Import(IEnumerable<EsiMailingListsListItem> src)
        {
            Items.Clear();

            // Import the mail messages from the API
            foreach (EsiMailingListsListItem srcEVEMailingList in src)
            {
                Items.Add(new EveMailingList(srcEVEMailingList));
            }

            // Fires the event regarding EVE mailing lists update
            ServiceLocator.TraceService.Trace(m_ccpCharacter.Name);
            ServiceLocator.EventAggregator.Publish(new CommonEvents.CharacterEVEMailingListsUpdatedEvent(m_ccpCharacter));
        }
    }
}
