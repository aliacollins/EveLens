// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Drawing;

namespace EVEMon.PieChart
{
    /// <summary>
    ///   Structure with graphics utility methods.
    /// </summary>
    public struct GraphicsUtil
    {
        /// <summary>
        ///   Checks if point is contained within <c>RectangleF</c> structure 
        ///   and extends rectangle bounds if neccessary to include the point.
        /// </summary>
        /// <param name="rect">
        ///   Reference to <c>RectangleF</c> to check.
        /// </param>
        /// <param name="pointToInclude">
        ///   <c>PontF</c> object to include.
        /// </param>
        public static RectangleF IncludePoint(RectangleF rect, PointF pointToInclude)
        {
            rect = IncludePointX(rect, pointToInclude.X);
            rect = IncludePointY(rect, pointToInclude.Y);
            return rect;
        }

        /// <summary>
        ///   Checks if x-coordinate is contained within the <c>RectangleF</c> 
        ///   structure and extends rectangle bounds if neccessary to include 
        ///   the point.
        /// </summary>
        /// <param name="rect">
        ///   <c>RectangleF</c> to check.
        /// </param>
        /// <param name="pointXToInclude">
        ///   x-coordinate to include.
        /// </param>
        public static RectangleF IncludePointX(RectangleF rect, float pointXToInclude)
        {
            if (pointXToInclude < rect.X)
            {
                rect.Width = rect.Right - pointXToInclude;
                rect.X = pointXToInclude;
            }
            else if (pointXToInclude > rect.Right)
                rect.Width = pointXToInclude - rect.X;

            return rect;
        }

        /// <summary>
        ///   Checks if y-coordinate is contained within the <c>RectangleF</c> 
        ///   structure and extends rectangle bounds if neccessary to include 
        ///   the point.
        /// </summary>
        /// <param name="rect">
        ///   <c>RectangleF</c> to check.
        /// </param>
        /// <param name="pointYToInclude">
        ///   y-coordinate to include.
        /// </param>
        public static RectangleF IncludePointY(RectangleF rect, float pointYToInclude)
        {
            if (pointYToInclude < rect.Y)
            {
                rect.Height = rect.Bottom - pointYToInclude;
                rect.Y = pointYToInclude;
            }
            else if (pointYToInclude > rect.Bottom)
                rect.Height = pointYToInclude - rect.Y;

            return rect;
        }
    }
}