// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia;

namespace EveLens.Avalonia.Services
{
    /// <summary>
    /// Manages font scaling across the application. Computes 7 font size tiers
    /// from a base percentage (80-150%) and writes them to Application.Resources
    /// as DynamicResource values. All AXAML bindings update automatically.
    /// </summary>
    public static class FontScaleService
    {
        // Base sizes at 100% scale
        private const double BaseTiny = 8;
        private const double BaseCaption = 9;
        private const double BaseSmall = 10;
        private const double BaseBody = 11;
        private const double BaseSubheading = 12;
        private const double BaseHeading = 13;
        private const double BaseTitle = 15;

        /// <summary>Current scale factor (1.0 = 100%).</summary>
        private static double s_factor = 1.0;

        /// <summary>Gets the current Tiny font size.</summary>
        public static double Tiny => Math.Round(BaseTiny * s_factor);

        /// <summary>Gets the current Caption font size.</summary>
        public static double Caption => Math.Round(BaseCaption * s_factor);

        /// <summary>Gets the current Small font size.</summary>
        public static double Small => Math.Round(BaseSmall * s_factor);

        /// <summary>Gets the current Body font size.</summary>
        public static double Body => Math.Round(BaseBody * s_factor);

        /// <summary>Gets the current Subheading font size.</summary>
        public static double Subheading => Math.Round(BaseSubheading * s_factor);

        /// <summary>Gets the current Heading font size.</summary>
        public static double Heading => Math.Round(BaseHeading * s_factor);

        /// <summary>Gets the current Title font size.</summary>
        public static double Title => Math.Round(BaseTitle * s_factor);

        /// <summary>
        /// Applies a scale percentage (80-150) to all font size resources.
        /// All DynamicResource bindings in AXAML update automatically.
        /// </summary>
        public static void Apply(int scalePercent)
        {
            scalePercent = Math.Clamp(scalePercent, 80, 150);
            s_factor = scalePercent / 100.0;

            var resources = Application.Current?.Resources;
            if (resources == null) return;

            resources["EveFontTiny"] = Tiny;
            resources["EveFontCaption"] = Caption;
            resources["EveFontSmall"] = Small;
            resources["EveFontBody"] = Body;
            resources["EveFontSubheading"] = Subheading;
            resources["EveFontHeading"] = Heading;
            resources["EveFontTitle"] = Title;
        }
    }
}
