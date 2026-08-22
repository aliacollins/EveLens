// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The SKINR Hub's environment switcher: five named looks, each a backdrop plus a
    /// lighting spec the sidecar applies in one <c>scene</c> round trip.
    /// </summary>
    public enum SkinrEnvironmentPreset
    {
        /// <summary>CCP's own SKINR studio room — the game-parity default.</summary>
        Studio,

        /// <summary>The generated grey softbox drawn as the sky — product-shot neutral.</summary>
        Hangar,

        /// <summary>A nebula scene backdrop — the ship as it flies.</summary>
        Space,

        /// <summary>The studio with a hard bright key — detail inspection.</summary>
        Sunlight,

        /// <summary>The studio with a warm angled key — glamour framing.</summary>
        Beauty
    }

    /// <summary>
    /// Maps each <see cref="SkinrEnvironmentPreset"/> onto the sidecar's <c>scene</c> op
    /// payload. Kept as data rather than behaviour so the view model can enumerate the
    /// switcher and the test suite can assert every preset resolves to a real backdrop.
    /// </summary>
    public static class SkinrEnvironmentPresets
    {
        /// <summary>Every preset, in the order the switcher displays them.</summary>
        public static readonly IReadOnlyList<SkinrEnvironmentPreset> All = new[]
        {
            SkinrEnvironmentPreset.Studio,
            SkinrEnvironmentPreset.Hangar,
            SkinrEnvironmentPreset.Space,
            SkinrEnvironmentPreset.Sunlight,
            SkinrEnvironmentPreset.Beauty
        };

        /// <summary>Localization key for the preset's switcher label.</summary>
        public static string NameKey(SkinrEnvironmentPreset preset) => preset switch
        {
            SkinrEnvironmentPreset.Hangar => "Skinr.EnvHangar",
            SkinrEnvironmentPreset.Space => "Skinr.EnvSpace",
            SkinrEnvironmentPreset.Sunlight => "Skinr.EnvSunlight",
            SkinrEnvironmentPreset.Beauty => "Skinr.EnvBeauty",
            _ => "Skinr.EnvStudio"
        };

        /// <summary>
        /// The sidecar backdrop mode this preset renders against. Values are the sidecar's
        /// own <c>BACKDROPS</c> names; <c>room</c> is CCP's studio.
        /// </summary>
        public static string Backdrop(SkinrEnvironmentPreset preset) => preset switch
        {
            SkinrEnvironmentPreset.Hangar => "dome",
            SkinrEnvironmentPreset.Space => "nebula",
            _ => "room"
        };

        // CCP's authored studio key light, read from the live scene: a top light at
        // sunDiffuseColor (2,2,2). Every preset states its sun EXPLICITLY — a preset that
        // sent nothing would inherit whatever the previous preset left behind, and
        // Studio-after-Sunlight would keep the ×2 key forever (scene writes are sticky;
        // the sidecar has no "authored" reset).
        private static readonly double[] s_studioSunColor = { 2.0, 2.0, 2.0, 1.0 };
        private static readonly double[] s_studioSunDirection = { -0.15, -1.0, 0.0 };

        /// <summary>
        /// The sun colour for this preset, RGBA in linear light, matching the sidecar's
        /// <c>sunColor</c> spec key. Never null — see <see cref="s_studioSunColor"/>.
        /// </summary>
        public static IReadOnlyList<double> SunColor(SkinrEnvironmentPreset preset) =>
            preset switch
            {
                // A hard bright key for reading panel lines and seam detail.
                SkinrEnvironmentPreset.Sunlight => new[] { 4.5, 4.3, 4.0, 1.0 },
                // A warm key a stop above authored, angled by SunDirection below.
                SkinrEnvironmentPreset.Beauty => new[] { 2.6, 2.35, 2.1, 1.0 },
                _ => s_studioSunColor
            };

        /// <summary>
        /// The sun direction for this preset — XYZ, the direction the light travels.
        /// Never null, for the same stickiness reason as <see cref="SunColor"/>.
        /// </summary>
        public static IReadOnlyList<double> SunDirection(SkinrEnvironmentPreset preset) =>
            preset switch
            {
                SkinrEnvironmentPreset.Sunlight => new[] { -0.45, -0.80, 0.35 },
                SkinrEnvironmentPreset.Beauty => new[] { -0.30, -0.75, 0.55 },
                _ => s_studioSunDirection
            };
    }
}
