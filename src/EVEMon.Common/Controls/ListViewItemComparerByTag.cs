// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// Simple comparer for <see cref="ListViewItem"/> relying on a <see cref="IComparer{T}"/> for tags comparison. 
    /// For use with <see cref="ListView.ListViewItemSorter"/>.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class ListViewItemComparerByTag<T> : IComparer
    {
        private readonly IComparer<T> m_comparer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ListViewItemComparerByTag&lt;T&gt;"/> class.
        /// </summary>
        /// <param name="comparer">The tags comparer</param>
        public ListViewItemComparerByTag(IComparer<T> comparer)
        {
            m_comparer = comparer;
        }

        /// <summary>
        /// Comparer two <see cref="ListViewItem"/>.
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public int Compare(object x, object y)
        {
            ListViewItem itemX = (ListViewItem)x;
            ListViewItem itemY = (ListViewItem)y;
            T tagX = (T)itemX.Tag;
            T tagY = (T)itemY.Tag;

            return m_comparer.Compare(tagX, tagY);
        }
    }
}