// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace EVEMon.SkillPlanner
{
    /// <summary>
    /// Represents a button for changing the value of an attribute.
    /// It is not changes value of <see cref="AttributeBarControl"/> itself. 
    /// Instead this button provides associated <see cref="AttributeBarControl"/> and change amount for parent control.
    /// Parent will perform changes itself, based on unassignet attribute pool and parameters of a button.
    /// </summary>
    public class AttributeButtonControl : Control
    {
        private readonly Pen m_borderPenInactive = Pens.Gray;
        private readonly Brush m_backgroundBrush = Brushes.LightGray;
        private readonly Brush m_backgroundBrushHighlighted = Brushes.WhiteSmoke;
        private readonly Brush m_backgroundBrushPressed = Brushes.DarkGray;

        private Pen m_borderPen = Pens.Black;

        private GraphicsPath? m_borderPath;

        private bool m_hover;
        private bool m_pressed;

        /// <summary>
        /// Initializes new instance of <see cref="AttributeButtonControl"/>.
        /// </summary>
        public AttributeButtonControl()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.SupportsTransparentBackColor, true);
            SetStyle(ControlStyles.StandardDoubleClick, false);
            UpdateStyles();

            m_borderPath = CreateBorderPath();
        }

        /// <summary>
        /// Gets ot sets the change of the atribute value.
        /// </summary>
        [Category("Behavior"), DefaultValue(0)]
        public int ValueChange { get; set; }

        /// <summary>
        /// Gets or sets <see cref="AttributeBarControl"/> associated with this button.
        /// </summary>
        [Category("Behavior"), DefaultValue(null)]
        public AttributeBarControl? AttributeBar { get; set; }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.ForeColorChanged"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnForeColorChanged(EventArgs e)
        {
            base.OnForeColorChanged(e);

            m_borderPen = new Pen(ForeColor);
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseEnter"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            m_hover = true;
            Invalidate();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseLeave"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            m_hover = false;
            Invalidate();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseDown"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);

            if (e.Button == MouseButtons.Left)
                m_pressed = true;

            Invalidate();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.MouseUp"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.MouseEventArgs"/> that contains the event data.</param>
        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);

            if (e.Button == MouseButtons.Left)
                m_pressed = false;

            Invalidate();
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Resize"/> event.
        /// </summary>
        /// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            m_borderPath?.Dispose();

            m_borderPath = CreateBorderPath();
        }

        /// <summary>
        /// Creates a path in form of rounded rectangle. This path is used as shape of the button.
        /// </summary>
        /// <returns>Created path</returns>
        private GraphicsPath CreateBorderPath()
        {
            using (GraphicsPath borderPath = new GraphicsPath())
            {
                const int Radius = 3;
                const int HorizontalPad = 1;
                const int VerticalPad = 1;
                int width = Width - VerticalPad * 2;
                int height = Height - HorizontalPad * 2;
                borderPath.AddLine(HorizontalPad + Radius, VerticalPad, HorizontalPad + width - Radius * 2, VerticalPad);
                borderPath.AddArc(HorizontalPad + width - Radius * 2, VerticalPad, Radius * 2, Radius * 2, 270, 90);
                borderPath.AddLine(HorizontalPad + width, VerticalPad + Radius, HorizontalPad + width,
                                   VerticalPad + height - Radius * 2);
                borderPath.AddArc(HorizontalPad + width - Radius * 2, VerticalPad + height - Radius * 2, Radius * 2,
                                  Radius * 2, 0,
                                  90);
                borderPath.AddLine(HorizontalPad + width - Radius * 2, VerticalPad + height, HorizontalPad + Radius,
                                   VerticalPad + height);
                borderPath.AddArc(HorizontalPad, VerticalPad + height - Radius * 2, Radius * 2, Radius * 2, 90, 90);
                borderPath.AddLine(HorizontalPad, VerticalPad + height - Radius * 2, HorizontalPad, VerticalPad + Radius);
                borderPath.AddArc(HorizontalPad, VerticalPad, Radius * 2, Radius * 2, 180, 90);
                borderPath.CloseFigure();

                return (GraphicsPath)borderPath.Clone();
            }
        }

        /// <summary>
        /// Raises the <see cref="E:System.Windows.Forms.Control.Paint"/> event.
        /// </summary>
        /// <param name="e">A <see cref="T:System.Windows.Forms.PaintEventArgs"/> that contains the event data.</param>
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            using (Graphics g = e.Graphics)
            {
                g.SmoothingMode = SmoothingMode.HighQuality;

                Pen pen = Enabled ? m_borderPen : m_borderPenInactive;
                Brush brush;
                if (m_pressed)
                    brush = m_backgroundBrushPressed;
                else if (m_hover)
                    brush = m_backgroundBrushHighlighted;
                else
                    brush = m_backgroundBrush;

                e.Graphics.FillPath(brush, m_borderPath!);
                e.Graphics.DrawPath(pen, m_borderPath!);

                e.Graphics.DrawLine(pen, 4, Height / 2, Width - 4, Height / 2);

                if (ValueChange >= 0)
                    e.Graphics.DrawLine(pen, Width / 2, 4, Width / 2, Height - 4);
            }
        }
    }
}