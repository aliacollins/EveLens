// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.Collections
{
    /// <summary>
    /// Represents a base readonly collection based on an inner dictionary.
    /// </summary>
    public abstract class ReadonlyKeyedCollection<TKey, TItem> : IReadonlyKeyedCollection<TKey, TItem>
        where TItem : class
    {
        protected readonly Dictionary<TKey, TItem> Items = new Dictionary<TKey, TItem>();

        /// <summary>
        /// Gets the number of items in this collection
        /// </summary>
        public int Count => Items.Count;

        /// <summary>
        /// Gets the item with the given key
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        protected TItem GetByKey(TKey key)
        {
            TItem item;
            Items.TryGetValue(key, out item);
            return item;
        }


        #region Enumerators

        IEnumerator<TItem> IEnumerable<TItem>.GetEnumerator() => Items.Values.GetEnumerator();

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => Items.Values.GetEnumerator();

        #endregion


        #region IReadonlyKeyedCollection<TKey,TItem> Members

        TItem IReadonlyKeyedCollection<TKey, TItem>.this[TKey key] => GetByKey(key);

        #endregion
    }
}