// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using EVEMon.Common.Extensions;

namespace EVEMon.PieChart
{
    /// <summary>
    ///  Quadrilateral object.
    /// </summary>
    public class Quadrilateral : IDisposable
    {
        /// <summary>
        ///   <c>GraphicsPath</c> representing the quadrilateral.
        /// </summary>
        private readonly GraphicsPath m_path = new GraphicsPath();

        private bool m_disposed;

        /// <summary>
        ///   <c>PathPointType</c>s decribing the <c>GraphicsPath</c> points.
        /// </summary>
        private static readonly byte[] s_quadrilateralPointTypes = new byte[]
                                                                       {
                                                                           (byte)PathPointType.Start,
                                                                           (byte)PathPointType.Line,
                                                                           (byte)PathPointType.Line,
                                                                           (byte)PathPointType.Line |
                                                                           (byte)PathPointType.CloseSubpath
                                                                       };

        /// <summary>
        ///   Creates empty <c>Quadrilateral</c> object
        /// </summary>
        internal Quadrilateral()
        {
        }

        /// <summary>
        ///   Initilizes <c>Quadrilateral</c> object with given corner points.
        /// </summary>
        /// <param name="point1">
        ///   First <c>PointF</c>.
        /// </param>
        /// <param name="point2">
        ///   Second <c>PointF</c>.
        /// </param>
        /// <param name="point3">
        ///   Third <c>PointF</c>.
        /// </param>
        /// <param name="point4">
        ///   Fourth <c>PointF</c>.
        /// </param>
        /// <param name="toClose">
        ///   Indicator should the quadrilateral be closed by the line.
        /// </param>
        public Quadrilateral(PointF point1, PointF point2, PointF point3, PointF point4, bool toClose)
        {
            byte[] pointTypes = (byte[])s_quadrilateralPointTypes.Clone();
            if (toClose)
                pointTypes[3] |= (byte)PathPointType.CloseSubpath;
            m_path = new GraphicsPath(new[] { point1, point2, point3, point4 }, pointTypes);
        }

        /// <summary>
        ///   <c>Finalize</c> method.
        /// </summary>
        ~Quadrilateral()
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
        ///   Disposes of all pie slices.
        /// </summary>
        private void Dispose(bool disposing)
        {
            if (m_disposed)
                return;

            if (disposing)
                m_path.Dispose();
            m_disposed = true;
        }

        /// <summary>
        /// Draws the <c>Quadrilateral</c> with <c>Graphics</c> provided.
        /// </summary>
        /// <param name="graphics"><c>Graphics</c> used to draw.</param>
        /// <param name="pen"><c>Pen</c> used to draw outline.</param>
        /// <param name="brush"><c>Brush</c> used to fill the inside.</param>
        /// <exception cref="System.ArgumentNullException">graphics</exception>
        public void Draw(Graphics graphics, Pen pen, Brush brush)
        {
            graphics.ThrowIfNull(nameof(graphics));

            graphics.FillPath(brush, m_path);
            graphics.DrawPath(pen, m_path);
        }

        /// <summary>
        ///   Checks if the given <c>PointF</c> is contained within the 
        ///   quadrilateral.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> structure to check for.
        /// </param>
        /// <returns>
        ///   <c>true</c> if the point is contained within the quadrilateral.
        /// </returns>
        public bool Contains(PointF point)
        {
            if (m_path.PointCount == 0 || m_path.PathPoints.Length == 0)
                return false;
            return Contains(point, m_path.PathPoints);
        }

        /// <summary>
        /// Checks if given <c>PointF</c> is contained within quadrilateral
        /// defined by <c>cornerPoints</c> provided.
        /// </summary>
        /// <param name="point"><c>PointF</c> to check.</param>
        /// <param name="cornerPoints">Array of <c>PointF</c> structures defining corners of the
        /// quadrilateral.</param>
        /// <returns>
        ///   <c>true</c> if the point is contained within the quadrilateral.
        /// </returns>
        /// <exception cref="System.ArgumentNullException">point or cornerPoints</exception>
        public static bool Contains(PointF point, PointF[] cornerPoints)
        {
            point.ThrowIfNull(nameof(point));

            cornerPoints.ThrowIfNull(nameof(cornerPoints));

            int intersections = 0;
            for (int i = 1; i < cornerPoints.Length; ++i)
            {
                if (DoesIntersect(point, cornerPoints[i], cornerPoints[i - 1]))
                    ++intersections;
            }
            if (DoesIntersect(point, cornerPoints[cornerPoints.Length - 1], cornerPoints[0]))
                ++intersections;
            return intersections % 2 != 0;
        }

        /// <summary>
        ///   Checks if the line coming out of the <c>point</c> downwards 
        ///   intersects with a line through <c>point1</c> and <c>point2</c>.
        /// </summary>
        /// <param name="point">
        ///   <c>PointF</c> from which vertical line is drawn downwards.
        /// </param>
        /// <param name="point1">
        ///   First <c>PointF</c> through which line is drawn.
        /// </param>
        /// <param name="point2">
        ///   Second <c>PointF</c> through which line is drawn.
        /// </param>
        /// <returns>
        ///   <c>true</c> if lines intersect.
        /// </returns>
        private static bool DoesIntersect(PointF point, PointF point1, PointF point2)
        {
            float x2 = point2.X;
            float y2 = point2.Y;
            float x1 = point1.X;
            float y1 = point1.Y;
            if ((!(x2 < point.X) || !(x1 >= point.X)) && (!(x2 >= point.X) || !(x1 < point.X)))
                return false;

            float y = (y2 - y1) / (x2 - x1) * (point.X - x1) + y1;
            return y > point.Y;
        }
    }
}