// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;

namespace EveLens.Common.Data
{
    public static class MarketGroupExtensions
    {
        /// <summary>
        /// Gets true if the item's market group belongs to the questioned group. 
        /// </summary>
        public static bool BelongsIn(this MarketGroup marketGroup, int group)
            => marketGroup.BelongsIn(new[] { group });

        /// <summary>
        /// Gets true if the item's market group belongs to the questioned group. 
        /// </summary>
        public static bool BelongsIn(this MarketGroup marketGroup, IList<int> group)
        {
            while (marketGroup != null)
            {
                if (group.Any(x => x == marketGroup.ID))
                    return true;

                marketGroup = marketGroup.ParentGroup;
            }

            return false;
        }
    }
}