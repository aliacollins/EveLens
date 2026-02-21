// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections;
using EVEMon.Common.Models;

namespace EVEMon.Common.Data
{
    public class MasteryShipCollection : ReadonlyKeyedCollection<int, MasteryShip>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MasteryShipCollection"/> class.
        /// </summary>
        /// <param name="character">The character.</param>
        public MasteryShipCollection(Character character)
        {
            if (StaticMasteries.AllMasteryShips == null)
                return;

            // Builds the list
            foreach (var masteryShip in StaticMasteries.AllMasteryShips)
                if (masteryShip.Ship != null)
                    Items[masteryShip.Ship.ID] = new MasteryShip(character, masteryShip);
        }

        /// <summary>
        /// Gets the mastery ship by identifier.
        /// </summary>
        /// <param name="id">The identifier.</param>
        /// <returns></returns>
        public MasteryShip GetMasteryShipByID(int id) => Items.ContainsKey(id) ? Items[id] :
            null;

        /// <summary>
        /// Initializes each item in the collection.
        /// </summary>
        public void Initialize()
        {
            foreach (KeyValuePair<int, MasteryShip> item in Items)
                item.Value.Initialize();
        }
    }
}
