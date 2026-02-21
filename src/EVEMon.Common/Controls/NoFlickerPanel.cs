// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    public class NoFlickerPanel : Panel
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NoFlickerPanel"/> class.
        /// </summary>
        public NoFlickerPanel()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.ContainerControl |
                     ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }
    }
}