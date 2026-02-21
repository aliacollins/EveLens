// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Controls
{
    public class ListViewDragEventArgs : EventArgs
    {
        internal ListViewDragEventArgs(int from, int count, int to)
        {
            MovingFrom = from;
            MovingCount = count;
            MovingTo = to;
        }

        /// <summary>
        /// Gets the moving from.
        /// </summary>
        /// <value>
        /// The moving from.
        /// </value>
        public int MovingFrom { get; }

        /// <summary>
        /// Gets the moving count.
        /// </summary>
        /// <value>
        /// The moving count.
        /// </value>
        public int MovingCount { get; }

        /// <summary>
        /// Gets the moving to.
        /// </summary>
        /// <value>
        /// The moving to.
        /// </value>
        public int MovingTo { get; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="ListViewDragEventArgs"/> is cancel.
        /// </summary>
        /// <value>
        ///   <c>true</c> if cancel; otherwise, <c>false</c>.
        /// </value>
        public bool Cancel { get; set; }
    }
}