// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Windows.Forms;

namespace EVEMon.Common.Controls
{
    /// <summary>
    /// This control draws a border around its children.
    /// Unfortunately, you need to adjust the padding and such to prevents the top and left lines to be hidden by your controls.
    /// There is probably some way to change the client rectangle area but I didn't find it.
    /// Did I mention I hate winforms very much ?
    /// </summary>
    public sealed class BorderPanel : Panel
    {
        /// <summary>
        /// Constructor.
        /// </summary>
        public BorderPanel()
        {
            DoubleBuffered = true;
            BackColor = SystemColors.Window;
            Padding = new Padding(0);
            Margin = new Padding(0);
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ContainerControl |
                     ControlStyles.DoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw |
                     ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
        }

        /// <summary>
        /// Paints the border.
        /// </summary>
        /// <param name="pe"></param>
        protected override void OnPaint(PaintEventArgs pe)
        {
            base.OnPaint(pe);

            Rectangle rect = ClientRectangle;
            rect.Inflate(-1, -1);

            using (Pen pen = new Pen(Color.Gray, 1.0f))
            {
                pe.Graphics.DrawRectangle(pen, rect);
            }
        }
    }
}