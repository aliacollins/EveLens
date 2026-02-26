// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a read-only collection of items.
    /// </summary>
    public sealed class MarketGroupCollection : ReadonlyCollection<MarketGroup>
    {
        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <param name="src">The SRC.</param>
        internal MarketGroupCollection(MarketGroup group, ICollection<SerializableMarketGroup> src)
            : base(src?.Count ?? 0)
        {
            if (src == null)
                return;

            foreach (SerializableMarketGroup subCat in src)
            {
                Items.Add(new MarketGroup(group, subCat));
            }
        }
    }
}