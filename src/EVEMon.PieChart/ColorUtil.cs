// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.Drawing;

namespace EVEMon.PieChart
{
    /// <summary>
    ///   Color utility structure.
    /// </summary>
    public struct ColorUtil
    {
        /// <summary>
        ///   Small brightness change factor.
        /// </summary>
        public const float BrightnessEnhancementFactor1 = 0.3F;

        /// <summary>
        ///   Large brightness change factor.
        /// </summary>
        public const float BrightnessEnhancementFactor2 = 0.5F;

        /// <summary>
        ///   Creates color with corrected lightness.
        /// </summary>
        /// <param name="color">
        ///   Color to correct.
        /// </param>
        /// <param name="correctionFactor">
        ///   Correction factor, with a value between -1 and 1. Negative values
        ///   create darker color, positive values lighter color. Zero value
        ///   returns the current color.
        /// </param>
        /// <returns>
        ///   Corrected <c>Color</c> structure.
        /// </returns>
        public static Color CreateColorWithCorrectedLightness(Color color, float correctionFactor)
        {
            Debug.Assert(correctionFactor <= 1 && correctionFactor >= -1);
            if (Math.Abs(correctionFactor) < float.Epsilon)
                return color;
            float red = color.R;
            float green = color.G;
            float blue = color.B;
            if (correctionFactor < 0)
            {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else
            {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }
            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }
    }
}