// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Diagnostics;
using System.Drawing;

namespace EVEMon.PieChart
{
    /// <summary>
    ///   Structure representing edge color used for rendering.
    /// </summary>
    public struct EdgeColor
    {
        /// <summary>
        ///   Gets the actual color used for rendering.
        /// </summary>
        public static Color GetRenderingColor(EdgeColorType edgeColorType, Color color)
        {
            Debug.Assert(color != Color.Empty);
            if (edgeColorType == EdgeColorType.Contrast || edgeColorType == EdgeColorType.EnhancedContrast)
                edgeColorType = GetContrastColorType(color, edgeColorType);
            float correctionFactor = 0;
            switch (edgeColorType)
            {
                case EdgeColorType.SystemColor:
                    return SystemColors.WindowText;
                case EdgeColorType.SurfaceColor:
                    return color;
                case EdgeColorType.FullContrast:
                    return GetFullContrastColor(color);
                case EdgeColorType.DarkerThanSurface:
                    correctionFactor = -ColorUtil.BrightnessEnhancementFactor1;
                    break;
                case EdgeColorType.DarkerDarkerThanSurface:
                    correctionFactor = -ColorUtil.BrightnessEnhancementFactor2;
                    break;
                case EdgeColorType.LighterThanSurface:
                    correctionFactor = +ColorUtil.BrightnessEnhancementFactor1;
                    break;
                case EdgeColorType.LighterLighterThanSurface:
                    correctionFactor = +ColorUtil.BrightnessEnhancementFactor2;
                    break;
                case EdgeColorType.NoEdge:
                    return Color.Transparent;
            }
            return ColorUtil.CreateColorWithCorrectedLightness(color, correctionFactor);
        }

        /// <summary>
        /// Gets the type of the contrast color.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <param name="colorType">Type of the color.</param>
        /// <returns></returns>
        private static EdgeColorType GetContrastColorType(Color color, EdgeColorType colorType)
        {
            Debug.Assert(colorType == EdgeColorType.Contrast || colorType == EdgeColorType.EnhancedContrast);
            if (color.GetBrightness() > BrightnessThreshold)
            {
                return colorType == EdgeColorType.Contrast
                    ? EdgeColorType.DarkerThanSurface
                    : EdgeColorType.DarkerDarkerThanSurface;
            }

            return colorType == EdgeColorType.Contrast
                ? EdgeColorType.LighterThanSurface
                : EdgeColorType.LighterLighterThanSurface;
        }

        /// <summary>
        /// Gets the full color of the contrast.
        /// </summary>
        /// <param name="color">The color.</param>
        /// <returns></returns>
        private static Color GetFullContrastColor(Color color)
            => color.GetBrightness() > BrightnessThreshold ? Color.Black : Color.White;

        private const float BrightnessThreshold = 0.4F;
    }
}