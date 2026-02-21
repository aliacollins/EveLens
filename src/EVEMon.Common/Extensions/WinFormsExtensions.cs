// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Windows.Forms;

namespace EVEMon.Common.Extensions
{
    public static class WinFormsExtensions
    {
        /// <summary>
        /// Selects every item in a <c>List</c>.
        /// </summary>
        /// <remarks>
        /// Not recommended for use on a <c>ListView</c> that contains thousands of items.
        /// </remarks>
        /// <param name="lv"><c>ListView</c> to select all items in</param>
        public static void SelectAll(this ListView lv)
        {
            if (lv == null)
                return;

            if (lv.Items.Count == 0)
                return;

            foreach (ListViewItem item in lv.Items)
            {
                item.Selected = true;
            }
        }
    }
}