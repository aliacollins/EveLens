// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// A SplitContainer that respects minimum panel sizes when resizing.
    /// This prevents the splitter from being dragged past the minimum size boundaries.
    /// </summary>
    public class SplitContainerMinFixed : SplitContainer
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SplitContainerMinFixed"/> class.
        /// </summary>
        public SplitContainerMinFixed()
        {
            SplitterMoved += OnSplitterMovedHandler;
        }

        /// <summary>
        /// Handles the SplitterMoved event.
        /// Ensures the splitter position respects the minimum panel sizes.
        /// </summary>
        private void OnSplitterMovedHandler(object sender, SplitterEventArgs e)
        {
            // Ensure Panel1 minimum size is respected
            if (SplitterDistance < Panel1MinSize)
                SplitterDistance = Panel1MinSize;

            // Ensure Panel2 minimum size is respected
            int maxDistance = Orientation == Orientation.Horizontal
                ? Height - Panel2MinSize - SplitterWidth
                : Width - Panel2MinSize - SplitterWidth;

            if (SplitterDistance > maxDistance && maxDistance > Panel1MinSize)
                SplitterDistance = maxDistance;
        }
    }
}
