// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Controls.MultiPanel
{
    /// <summary>
    /// Argument for the <see cref="MultiPanel.SelectionChange"/> event.
    /// </summary>
    public sealed class MultiPanelSelectionChangeEventArgs : EventArgs
    {
        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="oldPage"></param>
        /// <param name="newPage"></param>
        public MultiPanelSelectionChangeEventArgs(MultiPanelPage oldPage, MultiPanelPage newPage)
        {
            OldPage = oldPage;
            NewPage = newPage;
        }

        /// <summary>
        /// Gets the old selection.
        /// </summary>
        public MultiPanelPage OldPage { get; }

        /// <summary>
        /// Gets the new selection.
        /// </summary>
        public MultiPanelPage NewPage { get; }
    }
}