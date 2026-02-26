// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Collections;
using EveLens.Common.Enumerations;
using EveLens.Common.Serialization.Datafiles;

namespace EveLens.Common.Data
{
    /// <summary>
    /// Represents a read-only collection of items.
    /// </summary>
    public sealed class ItemCollection : ReadonlyCollection<Item>
    {
        #region Constructor

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="group">Market Group for the item</param>
        /// <param name="src">One or more source serializable items</param>
        internal ItemCollection(MarketGroup group, ICollection<SerializableItem> src)
            : base(src?.Count ?? 0)
        {
            if (src == null)
                return;

            foreach (SerializableItem item in src)
            {
                switch (item.Family)
                {
                    default:
                        Items.Add(new Item(group, item));
                        break;
                    case ItemFamily.Implant:
                        Items.Add(new Implant(group, item));
                        break;
                    case ItemFamily.Ship:
                        Items.Add(new Ship(group, item));
                        break;
                }
            }
        }

        #endregion
    }
}