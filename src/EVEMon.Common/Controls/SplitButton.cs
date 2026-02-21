// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using EVEMon.Common.Extensions;

/* Code from http://blogs.msdn.com/jfoscoding/articles/491523.aspx */

namespace EVEMon.Common.Controls
{
    public sealed class SplitButton : Button
    {
        private PushButtonState m_state;
        private const int PushButtonWidth = 14;
        private static readonly int s_borderSize = SystemInformation.Border3DSize.Width * 2;
        private bool m_skipNextOpen;
        private Rectangle m_dropDownRectangle;
        private bool m_showSplit = true;
        public event EventHandler ContextMenuShowing;

        public SplitButton()
        {
            AutoSize = true;
        }

        [DefaultValue(true)]
        public bool ShowSplit
        {
            get { return m_showSplit; }
            set
            {
                if (value == m_showSplit)
                    return;

                m_showSplit = value;
                Invalidate();
                Parent?.PerformLayout();
            }
        }

        private PushButtonState State
        {
            get { return m_state; }

            set
            {
                if (m_state.Equals(value))
                    return;

                m_state = value;
                Invalidate();
            }
        }

        public override Size GetPreferredSize(Size proposedSize)
        {
            Size preferredSize = base.GetPreferredSize(proposedSize);
            if (m_showSplit && !string.IsNullOrEmpty(Text) &&
                TextRenderer.MeasureText(Text, Font).Width + PushButtonWidth > preferredSize.Width)
                return preferredSize + new Size(PushButtonWidth + s_borderSize * 2, 0);

            return preferredSize;
        }

        protected override bool IsInputKey(Keys keyData)
        {
            if (keyData.Equals(Keys.Down) && m_showSplit)
                return true;

            return base.IsInputKey(keyData);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            if (!m_showSplit)
                return;

            if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                State = PushButtonState.Default;
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (!m_showSplit)
                return;

            if (e.KeyCode.Equals(Keys.Down))
                ShowContextMenuStrip();

            if (e.KeyCode.Equals(Keys.Space) && e.Modifiers == Keys.None)
                State = PushButtonState.Pressed;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            base.OnKeyUp(e);

            if (!e.KeyCode.Equals(Keys.Space))
                return;

            if (MouseButtons == MouseButtons.None)
                State = PushButtonState.Normal;
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);

            if (!m_showSplit)
                return;

            if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                State = PushButtonState.Normal;
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (!m_showSplit)
                return;

            if (m_dropDownRectangle.Contains(e.Location))
                ShowContextMenuStrip();
            else
                State = PushButtonState.Pressed;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            if (!m_showSplit)
                return;

            if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                State = PushButtonState.Hot;
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            if (!m_showSplit)
                return;

            if (!State.Equals(PushButtonState.Pressed) && !State.Equals(PushButtonState.Disabled))
                State = Focused ? PushButtonState.Default : PushButtonState.Normal;
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (!m_showSplit)
                return;

            if (ContextMenuStrip != null && ContextMenuStrip.Visible)
                return;

            SetButtonDrawState();
            if (Bounds.Contains(Parent.PointToClient(Cursor.Position)) && !m_dropDownRectangle.Contains(e.Location))
                OnClick(new EventArgs());
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (!m_showSplit)
                return;

            Graphics g = e.Graphics;
            Rectangle bounds = ClientRectangle;

            // draw the button background as according to the current state.
            if (State != PushButtonState.Pressed && IsDefault && !Application.RenderWithVisualStyles)
            {
                Rectangle backgroundBounds = bounds;
                backgroundBounds.Inflate(-1, -1);
                ButtonRenderer.DrawButton(g, backgroundBounds, State);

                // button renderer doesnt draw the black frame when themes are off =(
                g.DrawRectangle(SystemPens.WindowFrame, 0, 0, bounds.Width - 1, bounds.Height - 1);
            }

            else
                ButtonRenderer.DrawButton(g, bounds, State);

            // calculate the current dropdown rectangle.
            m_dropDownRectangle = new Rectangle(bounds.Right - PushButtonWidth - 1, s_borderSize, PushButtonWidth,
                                                bounds.Height - s_borderSize * 2);
            int internalBorder = s_borderSize;
            Rectangle focusRect =
                new Rectangle(internalBorder,
                              internalBorder,
                              bounds.Width - m_dropDownRectangle.Width - internalBorder,
                              bounds.Height - internalBorder * 2);

            bool drawSplitLine = State == PushButtonState.Hot || State == PushButtonState.Pressed ||
                                 !Application.RenderWithVisualStyles;

            if (RightToLeft == RightToLeft.Yes)
            {
                m_dropDownRectangle.X = bounds.Left + 1;
                focusRect.X = m_dropDownRectangle.Right;
                if (drawSplitLine)
                {
                    // draw two lines at the edge of the dropdown button
                    g.DrawLine(SystemPens.ButtonShadow, bounds.Left + PushButtonWidth, s_borderSize, bounds.Left + PushButtonWidth,
                               bounds.Bottom - s_borderSize);
                    g.DrawLine(SystemPens.ButtonFace, bounds.Left + PushButtonWidth + 1, s_borderSize,
                               bounds.Left + PushButtonWidth + 1, bounds.Bottom - s_borderSize);
                }
            }
            else
            {
                if (drawSplitLine)
                {
                    // draw two lines at the edge of the dropdown button
                    g.DrawLine(SystemPens.ButtonShadow, bounds.Right - PushButtonWidth, s_borderSize,
                               bounds.Right - PushButtonWidth, bounds.Bottom - s_borderSize);
                    g.DrawLine(SystemPens.ButtonFace, bounds.Right - PushButtonWidth - 1, s_borderSize,
                               bounds.Right - PushButtonWidth - 1, bounds.Bottom - s_borderSize);
                }
            }

            // Draw an arrow in the correct location
            PaintArrow(g, m_dropDownRectangle);

            // Figure out how to draw the text
            TextFormatFlags formatFlags = TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter;

            // If we dont' use mnemonic, set formatFlag to NoPrefix as this will show ampersand.
            if (!UseMnemonic)
                formatFlags = formatFlags | TextFormatFlags.NoPrefix;

            if (!ShowKeyboardCues)
                formatFlags = formatFlags | TextFormatFlags.HidePrefix;

            if (!string.IsNullOrEmpty(Text))
                TextRenderer.DrawText(g, Text, Font, focusRect, SystemColors.ControlText, formatFlags);

            // draw the focus rectangle.
            if (State != PushButtonState.Pressed && Focused)
                ControlPaint.DrawFocusRectangle(g, focusRect);
        }

        private static void PaintArrow(Graphics g, Rectangle dropDownRect)
        {
            Point middle = new Point(Convert.ToInt32(dropDownRect.Left + dropDownRect.Width / 2),
                                     Convert.ToInt32(dropDownRect.Top + dropDownRect.Height / 2));

            //if the width is odd - favor pushing it over one pixel right.
            middle.X += dropDownRect.Width % 2;
            Point[] arrow = new[]
                                {
                                    new Point(middle.X - 2, middle.Y - 1), new Point(middle.X + 3, middle.Y - 1),
                                    new Point(middle.X, middle.Y + 2)
                                };
            g.FillPolygon(SystemBrushes.ControlText, arrow);
        }

        private void ShowContextMenuStrip()
        {
            if (m_skipNextOpen)
            {
                // we were called because we're closing the context menu strip
                // when clicking the dropdown button.
                m_skipNextOpen = false;
                return;
            }

            //expose an opportunity to modify the context menu
            ContextMenuShowing?.ThreadSafeInvoke(this, EventArgs.Empty);

            State = PushButtonState.Pressed;
            if (ContextMenuStrip == null)
                return;

            ContextMenuStrip.Closing += ContextMenuStrip_Closing;
            ContextMenuStrip.Show(this, new Point(0, Height), ToolStripDropDownDirection.BelowRight);
        }

        private void ContextMenuStrip_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            ContextMenuStrip cms = sender as ContextMenuStrip;
            if (cms != null)
                cms.Closing -= ContextMenuStrip_Closing;

            SetButtonDrawState();

            if (e.CloseReason == ToolStripDropDownCloseReason.AppClicked)
                m_skipNextOpen = m_dropDownRectangle.Contains(PointToClient(Cursor.Position));
        }

        private void SetButtonDrawState()
        {
            if (Bounds.Contains(Parent.PointToClient(Cursor.Position)))
                State = PushButtonState.Hot;
            else if (Focused)
                State = PushButtonState.Default;
            else
                State = PushButtonState.Normal;
        }
    }
}