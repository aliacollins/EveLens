// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Services
{
    /// <summary>
    /// How many samples the 3D preview is allowed to spend on each pixel, and how many pixels it
    /// may rasterise per pass. <em>Not</em> the frame's size — that is
    /// <see cref="SkinrRenderResolution"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>This used to also decide the resolution, and that was wrong twice over.</b> The
    /// tiers were 1024×768, 1280×960 and 1600×1200 — all 4:3, all shown inside a 16:9-ish pane with
    /// <c>Stretch="Uniform"</c>, so a High frame's 1.92 megapixels arrived pillarboxed and a good
    /// fraction of them were thrown away by the layout. And because size lived here, there was no
    /// way to ask for 1920×1080 at all. Sampling and size are independent choices and are now
    /// modelled as such.</para>
    ///
    /// <para><b>It also used to be a boot-time choice, and that was wrong too.</b> The claim was
    /// that Trinity's render target is created with the device and cannot be resized without one.
    /// Reading the engine's own source settles it: <c>EveSpaceSceneRenderDriver::Execute</c> takes
    /// its <c>displaySize</c> from the destination target's descriptor <em>every frame</em> and
    /// pulls every internal buffer — colour, depth, normals, velocity, distortion, stencil — out of
    /// a size-keyed <c>Tr2GpuResourcePool</c>, which <c>TriDevice</c>'s tick trims automatically.
    /// So the whole chain follows the target. The sidecar's <c>resize</c> op recreates the target,
    /// repoints the render job, invalidates the readback bitmap and re-runs the warm-up frames, and
    /// the device is never touched. Changing tier or size no longer costs a cold boot.</para>
    ///
    /// <para><b>Why supersampling rather than MSAA.</b> The hull is rasterised into the driver's own
    /// non-multisampled HDR buffer and tonemapped out of it, so multisampling our destination target
    /// costs memory and changes no pixel. Edge quality comes from TAA, and beyond that from
    /// rendering larger and box-filtering down — which <c>Tr2HostBitmap.Downsample2x2</c> does
    /// in-engine for any power of two, so a supersampled tier costs no extra bytes on the wire.
    /// </para>
    ///
    /// <para><b>Cost is quadratic in the supersample factor.</b> Measured on this project's software
    /// rasteriser (WARP), a settled 1024×768 frame is about 430 ms per pass and converges in five
    /// passes; ×2 is four times that. On a GPU it is unremarkable. That is why the tier is the
    /// user's choice rather than ours: only they know whether they are waiting for a hero shot or
    /// spinning a hull to look at it.</para>
    /// </remarks>
    public enum SkinrRenderQuality
    {
        /// <summary>
        /// No supersampling, and a 1.5-megapixel ceiling on the render so a weak GPU or the
        /// software rasteriser stays responsive under an orbit drag. Large resolutions are scaled
        /// down to fit, keeping their aspect.
        /// </summary>
        Preview,

        /// <summary>
        /// No supersampling, full requested resolution. TAA is still doing edge work. The viewer's
        /// default: it renders exactly what the pane can show and nothing more.
        /// </summary>
        Balanced,

        /// <summary>
        /// ×2 supersampling — four samples per output pixel on top of TAA — up to the hardware
        /// ceiling. 1920×1080 at ×2 is exactly that ceiling; above it the factor drops back to 1
        /// rather than the resolution dropping, because size is the thing the user can see.
        /// </summary>
        High
    }

    /// <summary>
    /// Turns a <see cref="SkinrRenderQuality"/> into the sampling settings that implement it, and
    /// into words for the UI. Separate from the enum so the numbers live in one place rather than
    /// being spread across whoever configures a sidecar.
    /// </summary>
    public static class SkinrRenderQualityPresets
    {
        /// <summary>
        /// Supersample factor. Kept a power of two on purpose: <c>Downsample2x2</c> resolves those
        /// inside the engine, so the host still receives exactly output-size pixels. A factor of 3
        /// would arrive at 3× the bytes needing a resample the sidecar's interpreter cannot do.
        /// </summary>
        public static int Supersample(SkinrRenderQuality quality)
            => quality == SkinrRenderQuality.High ? 2 : 1;

        /// <summary>
        /// The tier's own ceiling on rasterised pixels per pass, applied on top of
        /// <see cref="SkinrRenderResolutionPresets.MaxRenderPixels"/>.
        /// </summary>
        /// <remarks>
        /// Only <see cref="SkinrRenderQuality.Preview"/> sets one below the hardware limit, and it
        /// is the tier's entire point: something has to stay smooth on a machine that fell back to
        /// WARP, and the honest way to be fast is to rasterise fewer pixels rather than to pretend
        /// a 4K frame will arrive in time. 1.5 Mpix is about 1600×900.
        /// </remarks>
        public static long MaxRenderPixels(SkinrRenderQuality quality) => quality switch
        {
            SkinrRenderQuality.Preview => 1_500_000L,
            _ => SkinrRenderResolutionPresets.MaxRenderPixels
        };

        /// <summary>
        /// Cost per pass relative to <see cref="SkinrRenderQuality.Balanced"/> at the same
        /// resolution — which is to say, the square of the supersample factor. Exposed because it is
        /// the honest answer to "why is this slower" and it belongs next to the number that causes
        /// it rather than in a comment in the UI.
        /// </summary>
        public static double RelativeCost(SkinrRenderQuality quality)
        {
            int ss = Supersample(quality);
            return (double)ss * ss;
        }

        /// <summary>Localisation key for a tier's name, for the viewer's quality picker.</summary>
        public static string NameKey(SkinrRenderQuality quality) => quality switch
        {
            SkinrRenderQuality.High => "Skinr.QualityHigh",
            SkinrRenderQuality.Balanced => "Skinr.QualityBalanced",
            _ => "Skinr.QualityPreview"
        };

        /// <summary>
        /// What the tier does, in words, e.g. <c>×2 samples</c>. Shown next to the name because
        /// "High" means nothing without something attached to it.
        /// </summary>
        /// <remarks>
        /// This deliberately no longer quotes a resolution. It used to, and that number stopped
        /// being true the moment resolution became its own setting — a picker reading
        /// "High (1600×1200 ×2)" while the renderer produced 1920×1080 is worse than no number.
        /// </remarks>
        public static string Describe(SkinrRenderQuality quality) => quality switch
        {
            SkinrRenderQuality.High => "×2 samples",
            SkinrRenderQuality.Preview => "fast, capped size",
            _ => "1 sample"
        };
    }
}
