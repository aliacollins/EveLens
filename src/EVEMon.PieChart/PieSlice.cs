// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;

namespace EVEMon.PieChart
{
    /// <summary>
    ///   Object representing 3D pie.
    /// </summary>
    public sealed class PieSlice : IDisposable, ICloneable
    {
        /// <summary>
        ///   Actual start angle.
        /// </summary>
        private readonly float m_actualStartAngle;

        /// <summary>
        ///   Actual sweep angle.
        /// </summary>
        private readonly float m_actualSweepAngle;

        /// <summary>
        ///   Color of the surface.
        /// </summary>
        private readonly Color m_surfaceColor = Color.Empty;

        /// <summary>
        ///   Style used for shadow.
        /// </summary>
        private readonly ShadowStyle m_shadowStyle = ShadowStyle.NoShadow;

        /// <summary>
        ///   <c>EdgeColorType</c> used to draw pie sliece edges.
        /// </summary>
        private readonly EdgeColorType m_edgeColorType = EdgeColorType.NoEdge;

        /// <summary>
        ///   <c>Brush</c> used to render slice top surface.
        /// </summary>
        private Brush m_brushSurface = null!;

        /// <summary>
        ///   <c>Brush</c> used to render slice top surface when highlighted.
        /// </summary>
        private Brush m_brushSurfaceHighlighted = null!;

        /// <summary>
        ///   <c>Brush</c> used to render slice starting cut side.
        /// </summary>
        private Brush m_brushStartSide = null!;

        /// <summary>
        ///   <c>Brush</c> used to render slice ending cut side.
        /// </summary>
        private Brush m_brushEndSide = null!;

        /// <summary>
        ///   <c>Brush</c> used to render pie slice periphery (cylinder outer surface).
        /// </summary>
        private Brush m_brushPeripherySurface = null!;

        /// <summary>
        ///   <c>Pen</c> object used to draw pie slice edges.
        /// </summary>
        private readonly Pen m_pen = null!;

        /// <summary>
        ///   <c>PointF</c> corresponding to pie slice center.
        /// </summary>
        private PointF m_center;

        /// <summary>
        ///   <c>PointF</c> corresponding to the lower pie slice center.
        /// </summary>
        private PointF m_centerBelow;

        /// <summary>
        ///   <c>PointF</c> on the periphery corresponding to the start cut 
        ///   side.
        /// </summary>
        private PointF m_pointStart;

        /// <summary>
        ///   <c>PointF</c> on the periphery corresponding to the start cut 
        ///   side.
        /// </summary>
        private PointF m_pointStartBelow;

        /// <summary>
        ///   <c>PointF</c> on the periphery corresponding to the end cut 
        ///   side.
        /// </summary>
        private PointF m_pointEnd;

        /// <summary>
        ///   <c>PointF</c> on the periphery corresponding to the end cut 
        ///   side.
        /// </summary>
        private PointF m_pointEndBelow;

        /// <summary>
        ///   <c>Quadrilateral</c> representing the start side.
        /// </summary>
        private Quadrilateral m_startSide = new Quadrilateral();

        /// <summary>
        ///   <c>Quadrilateral</c> representing the end side.
        /// </summary>
        private Quadrilateral m_endSide = new Quadrilateral();

        /// <summary>
        ///   Flag indicating if object has been disposed.
        /// </summary>
        private bool m_disposed;

        /// <summary>
        ///   Angle offset used to define reference angle for gradual shadow.
        /// </summary>
        private const float ShadowAngle = 20F;

        /// <summary>
        ///   Initializes an empty instance of <c>PieSlice</c>.
        /// </summary>
        private PieSlice()
        {
        }

        /// <summary>
        ///   Initializes a new instance of flat <c>PieSlice</c> class with given 
        ///   bounds and visual style.
        /// </summary>
        /// <param name="boundingRectX">
        ///   x-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectY">
        ///   y-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectWidth">
        ///   Width of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="boundingRectHeight">
        ///   Height of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="startAngle">
        ///   Starting angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="surfaceColor">
        ///   Color used to paint the pie slice.
        /// </param>
        public PieSlice(float boundingRectX, float boundingRectY, float boundingRectWidth, float boundingRectHeight,
            float startAngle, float sweepAngle, Color surfaceColor)
            : this(
                boundingRectX, boundingRectY, boundingRectWidth, boundingRectHeight, 0F, startAngle, sweepAngle, surfaceColor,
                ShadowStyle.NoShadow, EdgeColorType.NoEdge)
        {
        }

        /// <summary>
        ///   Initializes a new instance of <c>PieSlice</c> class with given 
        ///   bounds and visual style.
        /// </summary>
        /// <param name="boundingRectX">
        ///   x-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectY">
        ///   y-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectWidth">
        ///   Width of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="boundingRectHeight">
        ///   Height of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Height of the pie slice.
        /// </param>
        /// <param name="startAngle">
        ///   Starting angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="surfaceColor">
        ///   Color used to paint the pie slice.
        /// </param>
        /// <param name="shadowStyle">
        ///   Shadow style used for slice rendering.
        /// </param>
        /// <param name="edgeColorType">
        ///   Edge color style used for slice rendering.
        /// </param>
        public PieSlice(float boundingRectX, float boundingRectY, float boundingRectWidth, float boundingRectHeight,
            float sliceHeight, float startAngle, float sweepAngle, Color surfaceColor, ShadowStyle shadowStyle,
            EdgeColorType edgeColorType)
            : this()
        {
            // set some persistent values
            m_actualStartAngle = startAngle;
            m_actualSweepAngle = sweepAngle;
            m_surfaceColor = surfaceColor;
            m_shadowStyle = shadowStyle;
            m_edgeColorType = edgeColorType;
            // create pens for rendering
            Color edgeLineColor = EdgeColor.GetRenderingColor(edgeColorType, surfaceColor);
            using (Pen pen = new Pen(edgeLineColor))
            {
                pen.LineJoin = LineJoin.Round;
                m_pen = (Pen)pen.Clone();
            }
            InitializePieSlice(boundingRectX, boundingRectY, boundingRectWidth, boundingRectHeight, sliceHeight);
        }

        /// <summary>
        ///   Initializes a new instance of <c>PieSlice</c> class with given 
        ///   bounds and visual style.
        /// </summary>
        /// <param name="boundingRect">
        ///   Bounding rectangle used to draw the top surface of the slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Pie slice height.
        /// </param>
        /// <param name="startAngle">
        ///   Starting angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="surfaceColor">
        ///   Color used to render pie slice surface.
        /// </param>
        /// <param name="shadowStyle">
        ///   Shadow style used in rendering.
        /// </param>
        /// <param name="edgeColorType">
        ///   Edge color type used for rendering.
        /// </param>
        public PieSlice(RectangleF boundingRect, float sliceHeight, float startAngle, float sweepAngle, Color surfaceColor,
            ShadowStyle shadowStyle, EdgeColorType edgeColorType)
            : this(
                boundingRect.X, boundingRect.Y, boundingRect.Width, boundingRect.Height, sliceHeight, startAngle, sweepAngle,
                surfaceColor, shadowStyle, edgeColorType)
        {
        }

        /// <summary>
        ///   Initializes a new instance of <c>PieSlice</c> class with given 
        ///   bounds and visual style.
        /// </summary>
        /// <param name="boundingRectX">
        ///   x-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectY">
        ///   y-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectWidth">
        ///   Width of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="boundingRectHeight">
        ///   Height of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Height of the pie slice.
        /// </param>
        /// <param name="startAngle">
        ///   Starting angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="surfaceColor">
        ///   Color used to render pie slice surface.
        /// </param>
        /// <param name="shadowStyle">
        ///   Shadow style used in rendering.
        /// </param>
        /// <param name="edgeColorType">
        ///   Edge color type used for rendering.
        /// </param>
        /// <param name="edgeLineWidth">
        ///   Edge line width.
        /// </param>
        public PieSlice(float boundingRectX, float boundingRectY, float boundingRectWidth, float boundingRectHeight,
            float sliceHeight, float startAngle, float sweepAngle, Color surfaceColor, ShadowStyle shadowStyle,
            EdgeColorType edgeColorType, float edgeLineWidth)
            : this(
                boundingRectX, boundingRectY, boundingRectWidth, boundingRectHeight, sliceHeight, startAngle, sweepAngle,
                surfaceColor, shadowStyle, edgeColorType)
        {
            m_pen.Width = edgeLineWidth;
        }

        /// <summary>
        ///   Initializes a new instance of <c>PieSlice</c> class with given 
        ///   bounds and visual style.
        /// </summary>
        /// <param name="boundingRect">
        ///   Bounding rectangle used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Pie slice height.
        /// </param>
        /// <param name="startAngle">
        ///   Starting angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle (in degrees) of the pie slice.
        /// </param>
        /// <param name="surfaceColor">
        ///   Color used to render pie slice surface.
        /// </param>
        /// <param name="shadowStyle">
        ///   Shadow style used in rendering.
        /// </param>
        /// <param name="edgeColorType">
        ///   Edge color type used for rendering.
        /// </param>
        /// <param name="edgeLineWidth">
        ///   Edge line width.
        /// </param>
        public PieSlice(Rectangle boundingRect, float sliceHeight, float startAngle, float sweepAngle, Color surfaceColor,
            ShadowStyle shadowStyle, EdgeColorType edgeColorType, float edgeLineWidth)
            : this(
                boundingRect.X, boundingRect.Y, boundingRect.Width, boundingRect.Height, sliceHeight, startAngle, sweepAngle,
                surfaceColor, shadowStyle, edgeColorType, edgeLineWidth)
        {
        }

        /// <summary>
        ///   <c>Finalize</c> implementation
        /// </summary>
        ~PieSlice()
        {
            Dispose(false);
        }

        /// <summary>
        ///   Implementation of <c>IDisposable</c> interface.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Disposes of all resources used by <c>PieSlice</c> object.
        /// </summary>
        /// <param name="disposing"></param>
        private void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
            {
                Debug.Assert(m_pen != null);
                m_pen.Dispose();
                DisposeBrushes();
                Debug.Assert(m_startSide != null);
                m_startSide.Dispose();
                Debug.Assert(m_endSide != null);
                m_endSide.Dispose();
            }
            m_disposed = true;
        }

        /// <summary>
        ///   Implementation of ICloneable interface.
        /// </summary>
        /// <returns>
        ///   A deep copy of this object.
        /// </returns>
        public object Clone()
            => new PieSlice(BoundingRectangle, SliceHeight, m_actualStartAngle, m_actualSweepAngle, m_surfaceColor, m_shadowStyle,
                m_edgeColorType);

        /// <summary>
        ///   Gets starting angle (in degrees) of the pie slice.
        /// </summary>
        public float StartAngle { get; private set; }

        /// <summary>
        ///   Gets sweep angle (in degrees) of the pie slice.
        /// </summary>
        public float SweepAngle { get; private set; }

        /// <summary>
        ///   Gets ending angle (in degrees) of the pie slice.
        /// </summary>
        public float EndAngle => (StartAngle + SweepAngle) % 360;

        /// <summary>
        /// Gets or sets the text.
        /// </summary>
        /// <value>The text.</value>
        public string? Text { get; set; }

        /// <summary>
        ///   Gets or sets the bounding rectangle.
        /// </summary>
        internal RectangleF BoundingRectangle { get; private set; }

        /// <summary>
        ///   Gets or sets the slice height.
        /// </summary>
        internal float SliceHeight { get; private set; }

        /// <summary>
        ///   Draws the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        public void Draw(Graphics graphics)
        {
            DrawBottom(graphics);
            DrawSides(graphics);
            DrawTop(graphics);
        }

        /// <summary>
        ///   Checks if given pie slice contains given point.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point given is contained within the slice.
        /// </returns>
        public bool Contains(PointF point) => PieSliceContainsPoint(point)
                                              || PeripheryContainsPoint(point)
                                              || m_startSide.Contains(point)
                                              || m_endSide.Contains(point);

        /// <summary>
        ///   Evaluates the point in the middle of the slice.
        /// </summary>
        /// <returns>
        ///   <c>PointF</c> in the middle of the pie top.
        /// </returns>
        public PointF GetTextPosition
        {
            get
            {
                if (SweepAngle >= 180)
                {
                    return PeripheralPoint(m_center.X, m_center.Y, BoundingRectangle.Width / 3, BoundingRectangle.Height / 3,
                        GetActualAngle(StartAngle) + SweepAngle / 2);
                }

                float x = (m_pointStart.X + m_pointEnd.X) / 2;
                float y = (m_pointStart.Y + m_pointEnd.Y) / 2;
                float angle = (float)(Math.Atan2(y - m_center.Y, x - m_center.X) * 180 / Math.PI);
                return PeripheralPoint(m_center.X, m_center.Y, BoundingRectangle.Width / 3, BoundingRectangle.Height / 3,
                    GetActualAngle(angle));
            }
        }

        /// <summary>
        ///   Evaluates the point in the middle of the slice.
        /// </summary>
        /// <returns>
        ///   <c>PointF</c> in the middle of the pie top.
        /// </returns>
        public PointF GetTextPositionOut(float div)
        {
            if (SweepAngle >= 180)
            {
                return PeripheralPoint(m_center.X, m_center.Y, BoundingRectangle.Width / div, BoundingRectangle.Height / div,
                    GetActualAngle(StartAngle) + SweepAngle / 2);
            }

            float x = (m_pointStart.X + m_pointEnd.X) / 2;
            float y = (m_pointStart.Y + m_pointEnd.Y) / 2;
            float angle = (float)(Math.Atan2(y - m_center.Y, x - m_center.X) * 180 / Math.PI);
            return PeripheralPoint(m_center.X, m_center.Y, BoundingRectangle.Width / div, BoundingRectangle.Height / div,
                GetActualAngle(angle));
        }

        /// <summary>
        ///   Draws pie slice sides.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        internal void DrawSides(Graphics graphics)
        {
            DrawHiddenPeriphery(graphics);
            // draw wegde sides
            if (StartAngle > 90 && StartAngle < 270)
            {
                DrawEndSide(graphics);
                DrawStartSide(graphics);
            }
            else
            {
                DrawStartSide(graphics);
                DrawEndSide(graphics);
            }
            DrawVisiblePeriphery(graphics);
        }

        /// <summary>
        ///   Splits a pie slice into two on the split angle.
        /// </summary>
        /// <param name="splitAngle">
        ///   Angle at which splitting is performed.
        /// </param>
        /// <returns>
        ///   An array of two pie  slices.
        /// </returns>
        internal PieSlice[] Split(float splitAngle)
        {
            // if split angle equals one of bounding angles, then nothing to split
            if (Math.Abs(StartAngle - splitAngle) < float.Epsilon || Math.Abs(EndAngle - splitAngle) < float.Epsilon)
                return new[] { (PieSlice)Clone() };

            float actualStartAngle = GetActualAngle(StartAngle);
            float newSweepAngle = (splitAngle - actualStartAngle + 360) % 360;
            using (PieSlice pieSlice1 = new PieSlice(BoundingRectangle, SliceHeight, actualStartAngle,
                newSweepAngle, m_surfaceColor, m_shadowStyle, m_edgeColorType))
            {
                pieSlice1.InitializeSides(true, false);

                newSweepAngle = GetActualAngle(EndAngle) - splitAngle;
                using (PieSlice pieSlice2 = new PieSlice(BoundingRectangle, SliceHeight, splitAngle, newSweepAngle,
                    m_surfaceColor, m_shadowStyle, m_edgeColorType))
                {
                    pieSlice2.InitializeSides(false);
                    return new[] { (PieSlice)pieSlice1.Clone(), (PieSlice)pieSlice2.Clone() };
                }
            }
        }

        /// <summary>
        ///   Readjusts the pie slice to fit new bounding rectangle provided.
        /// </summary>
        /// <param name="boundingRectX">
        ///   x-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectY">
        ///   y-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectWidth">
        ///   Width of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="boundingRectHeight">
        ///   Height of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Height of the pie slice.
        /// </param>
        internal void Readjust(float boundingRectX, float boundingRectY, float boundingRectWidth, float boundingRectHeight,
            float sliceHeight)
        {
            InitializePieSlice(boundingRectX, boundingRectY, boundingRectWidth, boundingRectHeight, sliceHeight);
        }

        /// <summary>
        ///   Draws visible start side.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        private void DrawStartSide(Graphics graphics)
        {
            if (m_startSide == null)
                return;

            // checks if the side is visible 
            if (StartAngle > 90 && StartAngle < 270)
                m_startSide.Draw(graphics, m_pen, m_brushStartSide);
            else
                m_startSide.Draw(graphics, m_pen, m_brushSurface);
        }

        /// <summary>
        ///   Draws visible end side.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        private void DrawEndSide(Graphics graphics)
        {
            if (m_endSide == null)
                return;

            // checks if the side is visible 
            if (EndAngle > 90 && EndAngle < 270)
                m_endSide.Draw(graphics, m_pen, m_brushSurface);
            else
                m_endSide.Draw(graphics, m_pen, m_brushEndSide);
        }

        /// <summary>
        ///   Draws visible outer periphery of the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        private void DrawVisiblePeriphery(Graphics graphics)
        {
            IEnumerable<PeripherySurfaceBounds> peripherySurfaceBounds = GetVisiblePeripherySurfaceBounds();
            foreach (PeripherySurfaceBounds surfaceBounds in peripherySurfaceBounds)
            {
                DrawCylinderSurfaceSection(graphics, m_pen, m_brushPeripherySurface, surfaceBounds.StartAngle,
                    surfaceBounds.EndAngle, surfaceBounds.StartPoint, surfaceBounds.EndPoint);
            }
        }

        /// <summary>
        ///   Draws hidden outer periphery of the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        private void DrawHiddenPeriphery(Graphics graphics)
        {
            IEnumerable<PeripherySurfaceBounds> peripherySurfaceBounds = GetHiddenPeripherySurfaceBounds();
            foreach (PeripherySurfaceBounds surfaceBounds in peripherySurfaceBounds)
            {
                DrawCylinderSurfaceSection(graphics, m_pen, m_brushSurface, surfaceBounds.StartAngle, surfaceBounds.EndAngle,
                    surfaceBounds.StartPoint, surfaceBounds.EndPoint);
            }
        }

        /// <summary>
        ///   Draws the bottom of the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        internal void DrawBottom(Graphics graphics)
        {
            graphics.FillPie(m_brushSurface, BoundingRectangle.X, BoundingRectangle.Y + SliceHeight,
                BoundingRectangle.Width, BoundingRectangle.Height, StartAngle, SweepAngle);
            graphics.DrawPie(m_pen, BoundingRectangle.X, BoundingRectangle.Y + SliceHeight, BoundingRectangle.Width,
                BoundingRectangle.Height, StartAngle, SweepAngle);
        }

        /// <summary>
        ///   Draws the top of the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> used to draw the pie slice.
        /// </param>
        internal void DrawTop(Graphics graphics)
        {
            graphics.FillPie(m_brushSurface, BoundingRectangle.X, BoundingRectangle.Y, BoundingRectangle.Width,
                BoundingRectangle.Height, StartAngle, SweepAngle);
            graphics.DrawPie(m_pen, BoundingRectangle, StartAngle, SweepAngle);
        }

        /// <summary>
        ///   Calculates the smallest rectangle into which this pie slice fits.
        /// </summary>
        /// <returns>
        ///   <c>RectangleF</c> into which this pie slice fits exactly.
        /// </returns>
        internal RectangleF GetFittingRectangle()
        {
            RectangleF boundingRectangle = new RectangleF(m_pointStart.X, m_pointStart.Y, 0, 0);
            if ((Math.Abs(StartAngle) < float.Epsilon) || (StartAngle + SweepAngle >= 360))
                boundingRectangle = GraphicsUtil.IncludePointX(boundingRectangle, BoundingRectangle.Right);

            if ((StartAngle <= 90) && (StartAngle + SweepAngle >= 90) || (StartAngle + SweepAngle >= 450))
                boundingRectangle = GraphicsUtil.IncludePointY(boundingRectangle, BoundingRectangle.Bottom + SliceHeight);

            if ((StartAngle <= 180) && (StartAngle + SweepAngle >= 180) || (StartAngle + SweepAngle >= 540))
                boundingRectangle = GraphicsUtil.IncludePointX(boundingRectangle, BoundingRectangle.Left);

            if ((StartAngle <= 270) && (StartAngle + SweepAngle >= 270) || (StartAngle + SweepAngle >= 630))
                boundingRectangle = GraphicsUtil.IncludePointY(boundingRectangle, BoundingRectangle.Top);

            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_center);
            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_centerBelow);
            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_pointStart);
            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_pointStartBelow);
            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_pointEnd);
            boundingRectangle = GraphicsUtil.IncludePoint(boundingRectangle, m_pointEndBelow);

            return boundingRectangle;
        }

        /// <summary>
        ///   Checks if given point is contained inside the pie slice.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check for.
        /// </param>
        /// <returns>
        ///   <c>true</c> if given point is inside the pie slice.
        /// </returns>
        internal bool PieSliceContainsPoint(PointF point)
            => PieSliceContainsPoint(point, BoundingRectangle.X, BoundingRectangle.Y, BoundingRectangle.Width,
                BoundingRectangle.Height, StartAngle, SweepAngle);

        /// <summary>
        ///   Checks if given point is contained by cylinder periphery.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check for.
        /// </param>
        /// <returns>
        ///   <c>true</c> if given point is inside the cylinder periphery.
        /// </returns>
        internal bool PeripheryContainsPoint(PointF point)
        {
            IEnumerable<PeripherySurfaceBounds> peripherySurfaceBounds = GetVisiblePeripherySurfaceBounds();
            return
                peripherySurfaceBounds.Any(
                    surfaceBounds => CylinderSurfaceSectionContainsPoint(point, surfaceBounds.StartPoint, surfaceBounds.EndPoint));
        }

        /// <summary>
        ///   Checks if point provided is inside pie slice start cut side.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point is inside the start side.
        /// </returns>
        internal bool StartSideContainsPoint(PointF point) => SliceHeight > 0 && m_startSide.Contains(point);

        /// <summary>
        ///   Checks if point provided is inside pie slice end cut side.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point is inside the end side.
        /// </returns>
        internal bool EndSideContainsPoint(PointF point) => SliceHeight > 0 && m_endSide.Contains(point);

        /// <summary>
        ///   Checks if bottom side of the pie slice contains the point.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point is inside the bottom of the pie slice.
        /// </returns>
        internal bool BottomSurfaceSectionContainsPoint(PointF point)
        {
            if (SliceHeight > 0)
            {
                return
                    PieSliceContainsPoint(point, BoundingRectangle.X, BoundingRectangle.Y + SliceHeight,
                        BoundingRectangle.Width, BoundingRectangle.Height, StartAngle, SweepAngle);
            }
            return false;
        }

        /// <summary>
        ///   Creates brushes used to render the pie slice.
        /// </summary>
        /// <param name="surfaceColor">
        ///   Color used for rendering.
        /// </param>
        /// <param name="shadowStyle">
        ///   Shadow style used for rendering.
        /// </param>
        private void CreateSurfaceBrushes(Color surfaceColor, ShadowStyle shadowStyle)
        {
            m_brushSurface = new SolidBrush(surfaceColor);
            m_brushSurfaceHighlighted =
                new SolidBrush(ColorUtil.CreateColorWithCorrectedLightness(surfaceColor, ColorUtil.BrightnessEnhancementFactor1));
            switch (shadowStyle)
            {
                case ShadowStyle.NoShadow:
                    m_brushStartSide = m_brushEndSide = m_brushPeripherySurface = new SolidBrush(surfaceColor);
                    break;
                case ShadowStyle.UniformShadow:
                    m_brushStartSide =
                        m_brushEndSide =
                            m_brushPeripherySurface =
                                new SolidBrush(ColorUtil.CreateColorWithCorrectedLightness(surfaceColor,
                                    -ColorUtil.BrightnessEnhancementFactor1));
                    break;
                case ShadowStyle.GradualShadow:
                    double angle = StartAngle - 180 - ShadowAngle;
                    if (angle < 0)
                        angle += 360;
                    m_brushStartSide = CreateBrushForSide(surfaceColor, angle);
                    angle = StartAngle + SweepAngle - ShadowAngle;
                    if (angle < 0)
                        angle += 360;
                    m_brushEndSide = CreateBrushForSide(surfaceColor, angle);
                    m_brushPeripherySurface = CreateBrushForPeriphery(surfaceColor);
                    break;
            }
        }

        /// <summary>
        ///   Disposes brush objects.
        /// </summary>
        private void DisposeBrushes()
        {
            Debug.Assert(m_brushSurface != null);
            Debug.Assert(m_brushStartSide != null);
            Debug.Assert(m_brushEndSide != null);
            Debug.Assert(m_brushPeripherySurface != null);
            Debug.Assert(m_brushSurfaceHighlighted != null);

            m_brushSurface.Dispose();
            m_brushStartSide.Dispose();
            m_brushEndSide.Dispose();
            m_brushPeripherySurface.Dispose();
            m_brushSurfaceHighlighted.Dispose();
        }

        /// <summary>
        ///   Creates a brush for start and end sides of the pie slice for 
        ///   gradual  shade.
        /// </summary>
        /// <param name="color">
        ///   Color used for pie slice rendering.
        /// </param>
        /// <param name="angle">
        ///   Angle of the surface.
        /// </param>
        /// <returns>
        ///   <c>Brush</c> object.
        /// </returns>
        private static Brush CreateBrushForSide(Color color, double angle)
            => new SolidBrush(ColorUtil.CreateColorWithCorrectedLightness(color,
                -(float)
                    (ColorUtil.BrightnessEnhancementFactor1 *
                     (1 - 0.8 * Math.Cos(angle * Math.PI / 180)))));

        /// <summary>
        ///   Creates a brush for outer periphery of the pie slice used for 
        ///   gradual shadow.
        /// </summary>
        /// <param name="color">
        ///   Color used for pie slice rendering.
        /// </param>
        /// <returns>
        ///   <c>Brush</c> object.
        /// </returns>
        private Brush CreateBrushForPeriphery(Color color)
        {
            ColorBlend colorBlend = new ColorBlend
            {
                Colors = new[]
                {
                    ColorUtil.CreateColorWithCorrectedLightness(color,
                        -ColorUtil.
                            BrightnessEnhancementFactor1 /
                        2),
                    color,
                    ColorUtil.CreateColorWithCorrectedLightness(color,
                        -ColorUtil.
                            BrightnessEnhancementFactor1)
                },
                Positions = new[] { 0F, 0.1F, 1.0F }
            };

            using (LinearGradientBrush brush = new LinearGradientBrush(BoundingRectangle, Color.Blue, Color.White,
                LinearGradientMode.Horizontal))
            {
                brush.InterpolationColors = colorBlend;
                return (LinearGradientBrush)brush.Clone();
            }
        }

        /// <summary>
        ///   Draws the outer periphery of the pie slice.
        /// </summary>
        /// <param name="graphics">
        ///   <c>Graphics</c> object used to draw the surface.
        /// </param>
        /// <param name="pen">
        ///   <c>Pen</c> used to draw outline.
        /// </param>
        /// <param name="brush">
        ///   <c>Brush</c> used to fill the quadrilateral.
        /// </param>
        /// <param name="startAngle">
        ///   Start angle (in degrees) of the periphery section.
        /// </param>
        /// <param name="endAngle">
        ///   End angle (in degrees) of the periphery section.
        /// </param>
        /// <param name="pointStart">
        ///   Point representing the start of the periphery.
        /// </param>
        /// <param name="pointEnd">
        ///   Point representing the end of the periphery.
        /// </param>
        private void DrawCylinderSurfaceSection(Graphics graphics, Pen pen, Brush brush, float startAngle, float endAngle,
            PointF pointStart, PointF pointEnd)
        {
            GraphicsPath path = CreatePathForCylinderSurfaceSection(startAngle, endAngle, pointStart, pointEnd);
            graphics.FillPath(brush, path);
            graphics.DrawPath(pen, path);
        }

        /// <summary>
        ///   Transforms actual angle to angle used for rendering. They are 
        ///   different because of perspective.
        /// </summary>
        /// <param name="angle">
        ///   Actual angle.
        /// </param>
        /// <returns>
        ///   Rendering angle.
        /// </returns>
        private float TransformAngle(float angle)
        {
            double x = BoundingRectangle.Width * Math.Cos(angle * Math.PI / 180);
            double y = BoundingRectangle.Height * Math.Sin(angle * Math.PI / 180);
            float result = (float)(Math.Atan2(y, x) * 180 / Math.PI);
            if (result < 0)
                return result + 360;
            return result;
        }

        /// <summary>
        ///   Gets the actual angle from the rendering angle.
        /// </summary>
        /// <param name="transformedAngle">
        ///   Transformed angle for which actual angle has to be evaluated.
        /// </param>
        /// <returns>
        ///   Actual angle.
        /// </returns>
        private float GetActualAngle(float transformedAngle)
        {
            double x = BoundingRectangle.Height * Math.Cos(transformedAngle * Math.PI / 180);
            double y = BoundingRectangle.Width * Math.Sin(transformedAngle * Math.PI / 180);
            float result = (float)(Math.Atan2(y, x) * 180 / Math.PI);
            if (result < 0)
                return result + 360;
            return result;
        }

        /// <summary>
        ///   Calculates the point on ellipse periphery for angle.
        /// </summary>
        /// <param name="xCenter">
        ///   x-coordinate of the center of the ellipse.
        /// </param>
        /// <param name="yCenter">
        ///   y-coordinate of the center of the ellipse.
        /// </param>
        /// <param name="semiMajor">
        ///   Horizontal semi-axis.
        /// </param>
        /// <param name="semiMinor">
        ///   Vertical semi-axis.
        /// </param>
        /// <param name="angleDegrees">
        ///   Angle (in degrees) for which corresponding periphery point has to 
        ///   be obtained.
        /// </param>
        /// <returns>
        ///   <c>PointF</c> on the ellipse.
        /// </returns>
        private static PointF PeripheralPoint(float xCenter, float yCenter, float semiMajor, float semiMinor, float angleDegrees)
        {
            double angleRadians = angleDegrees * Math.PI / 180;
            return new PointF(xCenter + (float)(semiMajor * Math.Cos(angleRadians)),
                yCenter + (float)(semiMinor * Math.Sin(angleRadians)));
        }

        /// <summary>
        ///   Initializes pie bounding rectangle, pie height, corners 
        ///   coordinates and brushes used for rendering.
        /// </summary>
        /// <param name="boundingRectX">
        ///   x-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectY">
        ///   y-coordinate of the upper-left corner of the rectangle that is 
        ///   used to draw the top surface of the pie slice.
        /// </param>
        /// <param name="boundingRectWidth">
        ///   Width of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="boundingRectHeight">
        ///   Height of the rectangle that is used to draw the top surface of 
        ///   the pie slice.
        /// </param>
        /// <param name="sliceHeight">
        ///   Height of the pie slice.
        /// </param>
        private void InitializePieSlice(float boundingRectX, float boundingRectY, float boundingRectWidth,
            float boundingRectHeight, float sliceHeight)
        {
            // stores bounding rectangle and pie slice height
            BoundingRectangle = new RectangleF(boundingRectX, boundingRectY, boundingRectWidth, boundingRectHeight);
            SliceHeight = sliceHeight;
            // recalculates start and sweep angle used for rendering
            StartAngle = TransformAngle(m_actualStartAngle);
            SweepAngle = m_actualSweepAngle;
            if (Math.Abs(SweepAngle % 180) > float.Epsilon)
                SweepAngle = TransformAngle(m_actualStartAngle + m_actualSweepAngle) - StartAngle;
            if (SweepAngle < 0)
                SweepAngle += 360;
            // creates brushes
            CreateSurfaceBrushes(m_surfaceColor, m_shadowStyle);
            // calculates center and end points on periphery
            float xCenter = boundingRectX + boundingRectWidth / 2;
            float yCenter = boundingRectY + boundingRectHeight / 2;
            m_center = new PointF(xCenter, yCenter);
            m_centerBelow = new PointF(xCenter, yCenter + sliceHeight);
            m_pointStart = PeripheralPoint(xCenter, yCenter, boundingRectWidth / 2, boundingRectHeight / 2, m_actualStartAngle);
            m_pointStartBelow = new PointF(m_pointStart.X, m_pointStart.Y + sliceHeight);
            m_pointEnd = PeripheralPoint(xCenter, yCenter, boundingRectWidth / 2, boundingRectHeight / 2,
                m_actualStartAngle + m_actualSweepAngle);
            m_pointEndBelow = new PointF(m_pointEnd.X, m_pointEnd.Y + sliceHeight);
            InitializeSides();
        }

        /// <summary>
        ///   Initializes start and end pie slice sides.
        /// </summary>
        /// <param name="startSideExists">
        ///   Does start side exists.
        /// </param>
        /// <param name="endSideExists">
        ///   Does end side exists.
        /// </param>
        private void InitializeSides(bool startSideExists = true, bool endSideExists = true)
        {
            m_startSide = startSideExists
                ? new Quadrilateral(m_center, m_pointStart, m_pointStartBelow, m_centerBelow,
                    Math.Abs(SweepAngle - 180) > float.Epsilon)
                : new Quadrilateral();
            m_endSide = endSideExists
                ? new Quadrilateral(m_center, m_pointEnd, m_pointEndBelow, m_centerBelow,
                    Math.Abs(SweepAngle - 180) > float.Epsilon)
                : new Quadrilateral();
        }

        /// <summary>
        ///   Gets an array of visible periphery bounds.
        /// </summary>
        /// <returns>
        ///   Array of <c>PeripherySurfaceBounds</c> objects.
        /// </returns>
        private IEnumerable<PeripherySurfaceBounds> GetVisiblePeripherySurfaceBounds()
        {
            ArrayList peripherySurfaceBounds = new ArrayList();
            // outer periphery side is visible only when startAngle or endAngle 
            // is between 0 and 180 degrees
            if (Math.Abs(SweepAngle) < float.Epsilon || (StartAngle >= 180 && StartAngle + SweepAngle <= 360))
                return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));

            // draws the periphery from start angle to the end angle or left
            // edge, whichever comes first
            PointF x1 = new PointF(m_pointStart.X, m_pointStart.Y);
            float fi2 = EndAngle;
            PointF x2 = new PointF(m_pointEnd.X, m_pointEnd.Y);
            if (StartAngle < 180)
            {
                if (StartAngle + SweepAngle > 180)
                {
                    fi2 = 180;
                    x2.X = BoundingRectangle.X;
                    x2.Y = m_center.Y;
                }
                peripherySurfaceBounds.Add(new PeripherySurfaceBounds(StartAngle, fi2, x1, x2));
            }

            // if lateral surface is visible from the right edge 
            if (!(StartAngle + SweepAngle > 360))
                return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));

            x1 = new PointF(BoundingRectangle.Right, m_center.Y);
            fi2 = EndAngle;
            x2 = new PointF(m_pointEnd.X, m_pointEnd.Y);
            if (fi2 > 180)
            {
                fi2 = 180;
                x2.X = BoundingRectangle.Left;
                x2.Y = m_center.Y;
            }
            peripherySurfaceBounds.Add(new PeripherySurfaceBounds(0, fi2, x1, x2));
            return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));
        }

        /// <summary>
        ///   Gets an array of hidden periphery bounds.
        /// </summary>
        /// <returns>
        ///   Array of <c>PeripherySurfaceBounds</c> objects.
        /// </returns>
        private IEnumerable<PeripherySurfaceBounds> GetHiddenPeripherySurfaceBounds()
        {
            ArrayList peripherySurfaceBounds = new ArrayList();
            // outer periphery side is not visible when startAngle or endAngle 
            // is between 180 and 360 degrees
            if (Math.Abs(SweepAngle) < float.Epsilon || (StartAngle >= 0 && StartAngle + SweepAngle <= 180))
                return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));

            // draws the periphery from start angle to the end angle or right
            // edge, whichever comes first
            if (!(StartAngle + SweepAngle > 180))
                return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));

            float fi1 = StartAngle;
            PointF x1 = new PointF(m_pointStart.X, m_pointStart.Y);
            float fi2 = StartAngle + SweepAngle;
            PointF x2 = new PointF(m_pointEnd.X, m_pointEnd.Y);
            if (fi1 < 180)
            {
                fi1 = 180;
                x1.X = BoundingRectangle.Left;
                x1.Y = m_center.Y;
            }
            if (fi2 > 360)
            {
                fi2 = 360;
                x2.X = BoundingRectangle.Right;
                x2.Y = m_center.Y;
            }
            peripherySurfaceBounds.Add(new PeripherySurfaceBounds(fi1, fi2, x1, x2));

            // if pie is crossing 360 & 180 deg. boundary, we have to 
            // invisible peripheries
            if (!(StartAngle < 360) || !(StartAngle + SweepAngle > 540))
                return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));

            fi1 = 180;
            x1 = new PointF(BoundingRectangle.Left, m_center.Y);
            fi2 = EndAngle;
            x2 = new PointF(m_pointEnd.X, m_pointEnd.Y);
            peripherySurfaceBounds.Add(new PeripherySurfaceBounds(fi1, fi2, x1, x2));
            return (PeripherySurfaceBounds[])peripherySurfaceBounds.ToArray(typeof(PeripherySurfaceBounds));
        }

        /// <summary>
        ///   Creates <c>GraphicsPath</c> for cylinder surface section. This 
        ///   path consists of two arcs and two vertical lines.
        /// </summary>
        /// <param name="startAngle">
        ///   Starting angle of the surface.
        /// </param>
        /// <param name="endAngle">
        ///   Ending angle of the surface.
        /// </param>
        /// <param name="pointStart">
        ///   Starting point on the cylinder surface.
        /// </param>
        /// <param name="pointEnd">
        ///   Ending point on the cylinder surface.
        /// </param>
        /// <returns>
        ///   <c>GraphicsPath</c> object representing the cylinder surface.
        /// </returns>
        private GraphicsPath CreatePathForCylinderSurfaceSection(float startAngle, float endAngle, PointF pointStart,
            PointF pointEnd)
        {
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(BoundingRectangle, startAngle, endAngle - startAngle);
                path.AddLine(pointEnd.X, pointEnd.Y, pointEnd.X, pointEnd.Y + SliceHeight);
                path.AddArc(BoundingRectangle.X, BoundingRectangle.Y + SliceHeight, BoundingRectangle.Width,
                    BoundingRectangle.Height, endAngle, startAngle - endAngle);
                path.AddLine(pointStart.X, pointStart.Y + SliceHeight, pointStart.X, pointStart.Y);
                return (GraphicsPath)path.Clone();
            }
        }

        /// <summary>
        ///   Checks if given point is contained within upper and lower pie 
        ///   slice surfaces or within the outer slice brink.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> structure to check for.
        /// </param>
        /// <param name="point1">
        ///   Starting point on the periphery.
        /// </param>
        /// <param name="point2">
        ///   Ending point on the periphery.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point given is contained.
        /// </returns>
        private bool CylinderSurfaceSectionContainsPoint(PointF point, PointF point1, PointF point2)
        {
            if (SliceHeight > 0)
            {
                return Quadrilateral.Contains(point,
                    new[]
                    {
                        point1, new PointF(point1.X, point1.Y + SliceHeight),
                        new PointF(point2.X, point2.Y + SliceHeight), point2
                    });
            }
            return false;
        }

        /// <summary>
        ///   Checks if point given is contained within the pie slice.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> to check for.
        /// </param>
        /// <param name="boundingRectXangle">
        ///   x-coordinate of the rectangle that bounds the ellipse from which
        ///   slice is cut.
        /// </param>
        /// <param name="boundingRectYangle">
        ///   y-coordinate of the rectangle that bounds the ellipse from which
        ///   slice is cut.
        /// </param>
        /// <param name="boundingRectWidthangle"> 
        ///   Width of the rectangle that bounds the ellipse from which
        ///   slice is cut.
        /// </param>
        /// <param name="boundingRectHeightangle">
        ///   Height of the rectangle that bounds the ellipse from which
        ///   slice is cut.
        /// </param>
        /// <param name="startAngle">
        ///   Start angle of the slice.
        /// </param>
        /// <param name="sweepAngle">
        ///   Sweep angle of the slice.
        /// </param>
        /// <returns>
        ///   <c>true</c> if point is contained within the slice.
        /// </returns>
        private bool PieSliceContainsPoint(PointF point, float boundingRectXangle, float boundingRectYangle,
            float boundingRectWidthangle, float boundingRectHeightangle, float startAngle,
            float sweepAngle)
        {
            double x = point.X - boundingRectXangle - boundingRectWidthangle / 2;
            double y = point.Y - boundingRectYangle - boundingRectHeightangle / 2;
            double angle = Math.Atan2(y, x);
            if (angle < 0)
                angle += 2 * Math.PI;
            double angleDegrees = angle * 180 / Math.PI;
            // point is inside the pie slice only if between start and end angle
            if ((!(angleDegrees >= startAngle) || !(angleDegrees <= startAngle + sweepAngle)) &&
                (!(startAngle + sweepAngle > 360) || !(angleDegrees + 360 <= startAngle + sweepAngle)))
                return false;

            // distance of the point from the ellipse centre
            double r = Math.Sqrt(y * y + x * x);
            return GetEllipseRadius(angle) > r;
        }

        /// <summary>
        ///   Evaluates the distance of an ellipse perimeter point for a
        ///   given angle.
        /// </summary>
        /// <param name="angle">
        ///   Angle for which distance has to be evaluated.
        /// </param>
        /// <returns>
        ///   Distance of the point from the ellipse centre.
        /// </returns>
        private double GetEllipseRadius(double angle)
        {
            double a = BoundingRectangle.Width / 2;
            double b = BoundingRectangle.Height / 2;
            double a2 = a * a;
            double b2 = b * b;
            double cosFi = Math.Cos(angle);
            double sinFi = Math.Sin(angle);
            // distance of the ellipse perimeter point
            return a * b / Math.Sqrt(b2 * cosFi * cosFi + a2 * sinFi * sinFi);
        }

        /// <summary>
        ///   Internal structure used to store periphery bounds data.
        /// </summary>
        private struct PeripherySurfaceBounds
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="PeripherySurfaceBounds"/> struct.
            /// </summary>
            /// <param name="startAngle">The start angle.</param>
            /// <param name="endAngle">The end angle.</param>
            /// <param name="startPoint">The start point.</param>
            /// <param name="endPoint">The end point.</param>
            public PeripherySurfaceBounds(float startAngle, float endAngle, PointF startPoint, PointF endPoint)
                : this()
            {
                StartAngle = startAngle;
                EndAngle = endAngle;
                StartPoint = startPoint;
                EndPoint = endPoint;
            }

            /// <summary>
            /// Gets or sets the start angle.
            /// </summary>
            /// <value>
            /// The start angle.
            /// </value>
            public float StartAngle { get; }

            /// <summary>
            /// Gets or sets the end angle.
            /// </summary>
            /// <value>
            /// The end angle.
            /// </value>
            public float EndAngle { get; }

            /// <summary>
            /// Gets or sets the start point.
            /// </summary>
            /// <value>
            /// The start point.
            /// </value>
            public PointF StartPoint { get; }

            /// <summary>
            /// Gets or sets the end point.
            /// </summary>
            /// <value>
            /// The end point.
            /// </value>
            public PointF EndPoint { get; }
        }
    }
}