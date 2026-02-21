// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EVEMon.Common.Collections;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Models.Collections
{
    public class EveFactionWarsCollection : ReadonlyCollection<EveFactionWar>
    {
        /// <summary>
        /// Imports an enumeration of API objects.
        /// </summary>
        /// <param name="src">The source.</param>
        internal void Import(IEnumerable<SerializableEveFactionWarsListItem> src)
        {
            Items.Clear();

            foreach (SerializableEveFactionWarsListItem item in src)
            {
                Items.Add(new EveFactionWar(item));
            }
        }
    }
}