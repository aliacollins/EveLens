// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;

namespace EveLens.Common.Services
{
    /// <summary>
    /// How large a frame the 3D preview produces, independent of how many samples it spends on
    /// each pixel.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is separate from <see cref="SkinrRenderQuality"/>.</b> The two were one
    /// setting, and that was a defect with two visible symptoms. First, 1920×1080 was
    /// <em>unrequestable</em>: the only sizes that existed were the three baked into the quality
    /// tiers. Second, every one of those tiers was 4:3 — 1024×768, 1280×960, 1600×1200 — while the
    /// render pane is a 16:9-ish rectangle showing the frame with <c>Stretch="Uniform"</c>. So the
    /// renderer rasterised 1.92 megapixels at High and the user saw roughly three quarters of them
    /// inside a pillarbox. Paying for pixels and then letting the layout throw them away is a worse
    /// outcome than rendering fewer of them at the right shape.</para>
    ///
    /// <para><b>Why <see cref="MatchViewport"/> is the default.</b> It is the only option that
    /// cannot be wrong: one render pixel per screen pixel, no upscale blur, no wasted rasterisation,
    /// and the aspect is whatever the window actually is. The fixed sizes exist for the cases where
    /// a number matters more than a fit — comparing against an in-game screenshot, or exporting a
    /// shot at a size someone else asked for.</para>
    /// </remarks>
    public enum SkinrRenderResolution
    {
        /// <summary>
        /// One render pixel per pane pixel, at the pane's own aspect ratio. Follows the window as
        /// it resizes.
        /// </summary>
        MatchViewport,

        /// <summary>
        /// The user's primary display size, at its own aspect. What "match my desktop" means for
        /// someone who wants the preview to look like the game does full-screen.
        /// </summary>
        MatchDisplay,

        /// <summary>1280×720.</summary>
        Hd720,

        /// <summary>1920×1080.</summary>
        Fhd1080,

        /// <summary>2560×1440.</summary>
        Qhd1440,

        /// <summary>3840×2160. Above the supersampled budget, so it renders flat.</summary>
        Uhd2160
    }

    /// <summary>
    /// A concrete render size: output pixels plus the supersample factor to rasterise them at.
    /// </summary>
    /// <param name="Width">Output width in pixels.</param>
    /// <param name="Height">Output height in pixels.</param>
    /// <param name="Supersample">Rasterise at this multiple in each axis, then filter down.</param>
    public readonly record struct SkinrRenderSize(int Width, int Height, int Supersample)
    {
        /// <summary>Pixels actually rasterised per pass — the number frame time tracks.</summary>
        public long RenderPixels =>
            (long)Width * Height * Supersample * Supersample;

        /// <summary>e.g. <c>1920×1080 ×2</c>, or <c>1280×720</c> when not supersampling.</summary>
        public override string ToString()
        {
            string size = Width.ToString(CultureInfo.CurrentCulture) + "×" +
                          Height.ToString(CultureInfo.CurrentCulture);
            return Supersample > 1
                ? size + " ×" + Supersample.ToString(CultureInfo.CurrentCulture)
                : size;
        }
    }

    /// <summary>
    /// Turns a resolution choice plus a quality tier plus the pane's real size into the one
    /// <see cref="SkinrRenderSize"/> that satisfies all three.
    /// </summary>
    /// <remarks>
    /// The arithmetic lives here rather than in the view model because it has to agree exactly with
    /// the sidecar's own clamp — the sidecar applies the same ceiling, in the same order, and
    /// reports what it did. Two copies of a clamp that disagree produce a status strip stating a
    /// resolution the renderer is not using, which is the kind of quiet lie this project keeps
    /// finding at the bottom of its bugs.
    /// </remarks>
    public static class SkinrRenderResolutionPresets
    {
        /// <summary>
        /// Nominal width for a fixed tier, or 0 for the two that are measured rather than chosen.
        /// </summary>
        public static int Width(SkinrRenderResolution resolution) => resolution switch
        {
            SkinrRenderResolution.Hd720 => 1280,
            SkinrRenderResolution.Fhd1080 => 1920,
            SkinrRenderResolution.Qhd1440 => 2560,
            SkinrRenderResolution.Uhd2160 => 3840,
            _ => 0
        };

        /// <inheritdoc cref="Width"/>
        public static int Height(SkinrRenderResolution resolution) => resolution switch
        {
            SkinrRenderResolution.Hd720 => 720,
            SkinrRenderResolution.Fhd1080 => 1080,
            SkinrRenderResolution.Qhd1440 => 1440,
            SkinrRenderResolution.Uhd2160 => 2160,
            _ => 0
        };

        /// <summary>Localisation key for the picker.</summary>
        public static string NameKey(SkinrRenderResolution resolution) => resolution switch
        {
            SkinrRenderResolution.MatchDisplay => "Skinr.ResMatchDisplay",
            SkinrRenderResolution.Hd720 => "Skinr.Res720",
            SkinrRenderResolution.Fhd1080 => "Skinr.Res1080",
            SkinrRenderResolution.Qhd1440 => "Skinr.Res1440",
            SkinrRenderResolution.Uhd2160 => "Skinr.Res2160",
            _ => "Skinr.ResMatchViewport"
        };

        /// <summary>
        /// The largest render size — pixels <em>after</em> supersampling — that any tier may ask
        /// for.
        /// </summary>
        /// <remarks>
        /// 8.3 Mpix is 4K flat, or 1080p at ×2. Beyond it the driver's eight-or-so internal targets
        /// stop being slow and start being an allocation failure inside a subprocess capped at 3 GB:
        /// 4K at ×2 is 33 Mpix and roughly 800 MB of buffers. The sidecar enforces the same number
        /// so a caller that skips this helper still cannot crash the renderer.
        /// </remarks>
        public const long MaxRenderPixels = 3840L * 2160L;

        /// <summary>
        /// Resolves the final render size, clamping to the tier's budget and the hardware ceiling
        /// while preserving aspect.
        /// </summary>
        /// <param name="resolution">What the user picked.</param>
        /// <param name="quality">How many samples per pixel that tier allows.</param>
        /// <param name="viewportWidth">The pane's width in device pixels, or 0 if not laid out.</param>
        /// <param name="viewportHeight">The pane's height in device pixels, or 0.</param>
        /// <param name="displayWidth">Primary display width in pixels, or 0 if unknown.</param>
        /// <param name="displayHeight">Primary display height in pixels, or 0.</param>
        /// <remarks>
        /// <para>Supersampling is surrendered before resolution, deliberately. Someone who picked
        /// 1080p asked for 1080p; handing them 1080p with TAA alone is a far better answer than
        /// 810p with four samples a pixel, because the thing they can see is the size.</para>
        ///
        /// <para>Both axes end on a multiple of four. Several of the driver's passes run at half and
        /// quarter resolution — bloom's mip chain, SSAO's downsample — and an odd dimension makes
        /// those divisions lossy in a way that shows as a one-pixel seam at the frame edge. The pane
        /// is whatever size the window manager says, so the rounding has to happen somewhere.</para>
        /// </remarks>
        public static SkinrRenderSize Fit(
            SkinrRenderResolution resolution,
            SkinrRenderQuality quality,
            int viewportWidth = 0, int viewportHeight = 0,
            int displayWidth = 0, int displayHeight = 0)
        {
            (int width, int height) = resolution switch
            {
                SkinrRenderResolution.MatchViewport => (viewportWidth, viewportHeight),
                SkinrRenderResolution.MatchDisplay => (displayWidth, displayHeight),
                _ => (Width(resolution), Height(resolution))
            };

            // A pane that has not been laid out yet reports zero, and so does a display we could
            // not query. Falling back to 1280×720 rather than refusing keeps the caller from having
            // to special-case "not measured yet" — which it would get wrong, because the first
            // resize request arrives before the first layout pass on some platforms.
            if (width < 64 || height < 64)
                (width, height) = (1280, 720);

            int supersample = SkinrRenderQualityPresets.Supersample(quality);
            long budget = Math.Min(MaxRenderPixels,
                                   SkinrRenderQualityPresets.MaxRenderPixels(quality));

            while (supersample > 1 &&
                   (long)width * height * supersample * supersample > budget)
            {
                supersample /= 2;
            }

            // Only now the resolution, and by scaling both axes together so aspect survives. The
            // loop halves rather than solving for the exact ratio because the supersample resolve
            // wants a power-of-two relationship and a hand-computed scale factor does not give one.
            while ((long)width * height * supersample * supersample > budget &&
                   width > 320 && height > 240)
            {
                width /= 2;
                height /= 2;
            }

            return new SkinrRenderSize(width - (width % 4), height - (height % 4), supersample);
        }
    }
}
