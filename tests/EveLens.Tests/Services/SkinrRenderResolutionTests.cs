// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// The SKINR viewer's render-size arithmetic: which pixels get rasterised, at what sampling,
    /// and what happens when the answer would be too large.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this arithmetic is worth a test file of its own.</b> It exists in two places by
    /// necessity — here, so the picker and the status strip can say what will happen, and again
    /// inside the Python sidecar, which enforces its own ceiling so a caller that skips the helper
    /// still cannot crash the renderer. Two copies of a clamp that disagree do not fail loudly; they
    /// produce a status strip stating a resolution the renderer is not using. These tests pin the
    /// host copy to the same rules the sidecar's <c>resize</c> op was measured against: supersample
    /// surrendered before resolution, both axes a multiple of four, 8.29 Mpix after supersampling.
    /// </para>
    ///
    /// <para><b>The defects this replaced.</b> Resolution used to be a property of the quality tier,
    /// which had two consequences: 1920×1080 was literally unrequestable, and all three tiers were
    /// 4:3 while the render pane is 16:9 — so the High tier rasterised 1.92 megapixels and the
    /// layout pillarboxed a large fraction of them away. Both are regressions worth a permanent
    /// guard, hence <see cref="Fit_never_exceeds_the_ceiling_for_any_combination"/> and the aspect
    /// assertions.</para>
    /// </remarks>
    public sealed class SkinrRenderResolutionTests
    {
        private static readonly SkinrRenderResolution[] AllResolutions =
            (SkinrRenderResolution[])Enum.GetValues(typeof(SkinrRenderResolution));

        private static readonly SkinrRenderQuality[] AllQualities =
            (SkinrRenderQuality[])Enum.GetValues(typeof(SkinrRenderQuality));

        // --- the fixed tiers say exactly what they are named ------------------

        [Theory]
        [InlineData(SkinrRenderResolution.Hd720, 1280, 720)]
        [InlineData(SkinrRenderResolution.Fhd1080, 1920, 1080)]
        [InlineData(SkinrRenderResolution.Qhd1440, 2560, 1440)]
        [InlineData(SkinrRenderResolution.Uhd2160, 3840, 2160)]
        public void Fixed_tiers_resolve_to_their_nominal_size(
            SkinrRenderResolution resolution, int width, int height)
        {
            SkinrRenderResolutionPresets.Width(resolution).Should().Be(width);
            SkinrRenderResolutionPresets.Height(resolution).Should().Be(height);

            // At Balanced there is no clamping to muddy it: what the tier is named is what it is.
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                resolution, SkinrRenderQuality.Balanced);
            size.Width.Should().Be(width);
            size.Height.Should().Be(height);
            size.Supersample.Should().Be(1);
        }

        /// <summary>
        /// The two measured options report zero rather than a guess. A caller that treats zero as a
        /// size gets a visibly wrong frame; a caller that treats a plausible-looking 1920 as
        /// measured gets a subtly wrong one, which is worse.
        /// </summary>
        [Theory]
        [InlineData(SkinrRenderResolution.MatchViewport)]
        [InlineData(SkinrRenderResolution.MatchDisplay)]
        public void Measured_modes_have_no_nominal_size(SkinrRenderResolution resolution)
        {
            SkinrRenderResolutionPresets.Width(resolution).Should().Be(0);
            SkinrRenderResolutionPresets.Height(resolution).Should().Be(0);
        }

        // --- the measured modes read the thing they are named after ----------

        [Fact]
        public void MatchViewport_uses_the_viewport_and_ignores_the_display()
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.MatchViewport, SkinrRenderQuality.Balanced,
                viewportWidth: 1024, viewportHeight: 640,
                displayWidth: 3840, displayHeight: 2160);

            size.Width.Should().Be(1024);
            size.Height.Should().Be(640);
        }

        [Fact]
        public void MatchDisplay_uses_the_display_and_ignores_the_viewport()
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.MatchDisplay, SkinrRenderQuality.Balanced,
                viewportWidth: 640, viewportHeight: 480,
                displayWidth: 2560, displayHeight: 1440);

            size.Width.Should().Be(2560);
            size.Height.Should().Be(1440);
        }

        /// <summary>
        /// A pane that has not been laid out yet, and a display that could not be queried, both
        /// report zero. Falling back rather than refusing matters because the first size request
        /// genuinely can arrive before the first layout pass.
        /// </summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(-1, -1)]
        [InlineData(40, 800)]
        public void Unmeasured_or_absurd_sizes_fall_back_to_720p(int width, int height)
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.MatchViewport, SkinrRenderQuality.Balanced,
                viewportWidth: width, viewportHeight: height);

            size.Width.Should().Be(1280);
            size.Height.Should().Be(720);
        }

        // --- rounding --------------------------------------------------------

        /// <summary>
        /// Several of the driver's passes run at half and quarter resolution — bloom's mip chain,
        /// SSAO's downsample — and an odd dimension makes those divisions lossy in a way that shows
        /// as a one-pixel seam at the frame edge. A window is whatever size the user dragged it to,
        /// so the rounding has to happen here.
        /// </summary>
        [Theory]
        [InlineData(1017, 763, 1016, 760)]
        [InlineData(1921, 1081, 1920, 1080)]
        [InlineData(1279, 719, 1276, 716)]
        [InlineData(1280, 720, 1280, 720)]
        public void Both_axes_end_on_a_multiple_of_four(
            int inWidth, int inHeight, int outWidth, int outHeight)
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.MatchViewport, SkinrRenderQuality.Balanced,
                viewportWidth: inWidth, viewportHeight: inHeight);

            size.Width.Should().Be(outWidth);
            size.Height.Should().Be(outHeight);
        }

        // --- which axis gives way --------------------------------------------

        /// <summary>
        /// 1920×1080 at ×2 is 8,294,400 rasterised pixels, which is exactly the ceiling. It must be
        /// allowed: an off-by-one in the comparison here would silently halve the supersample on the
        /// single most likely configuration the feature has.
        /// </summary>
        [Fact]
        public void High_at_1080p_sits_exactly_on_the_ceiling_and_keeps_its_supersample()
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.Fhd1080, SkinrRenderQuality.High);

            size.Width.Should().Be(1920);
            size.Height.Should().Be(1080);
            size.Supersample.Should().Be(2);
            size.RenderPixels.Should().Be(SkinrRenderResolutionPresets.MaxRenderPixels);
        }

        /// <summary>
        /// Over the ceiling, sampling is what gives way and never the size. Someone who picked 1440p
        /// asked for 1440p; handing them 1440p with TAA alone is a far better answer than 720p with
        /// four samples a pixel, because the thing they can see is the size.
        /// </summary>
        [Theory]
        [InlineData(SkinrRenderResolution.Qhd1440, 2560, 1440)]
        [InlineData(SkinrRenderResolution.Uhd2160, 3840, 2160)]
        public void Above_the_ceiling_the_supersample_is_surrendered_not_the_resolution(
            SkinrRenderResolution resolution, int width, int height)
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                resolution, SkinrRenderQuality.High);

            size.Width.Should().Be(width);
            size.Height.Should().Be(height);
            size.Supersample.Should().Be(1);
        }

        /// <summary>
        /// Preview's whole point is a pixel ceiling, so this is the one case where resolution does
        /// drop — and it drops by halving both axes together, so the aspect survives.
        /// </summary>
        [Fact]
        public void Preview_caps_the_pixel_count_and_keeps_the_aspect()
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.Fhd1080, SkinrRenderQuality.Preview);

            size.RenderPixels.Should().BeLessThanOrEqualTo(
                SkinrRenderQualityPresets.MaxRenderPixels(SkinrRenderQuality.Preview));
            (size.Width / (double)size.Height).Should().BeApproximately(1920.0 / 1080.0, 0.01);
            size.Supersample.Should().Be(1);
        }

        [Fact]
        public void Preview_leaves_a_size_under_its_ceiling_alone()
        {
            SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                SkinrRenderResolution.Hd720, SkinrRenderQuality.Preview);

            size.Width.Should().Be(1280);
            size.Height.Should().Be(720);
        }

        /// <summary>
        /// The invariant the sidecar depends on. It applies the same ceiling itself, so a host that
        /// exceeds it gets its request quietly reduced and the two sides then disagree about what is
        /// on screen. Swept across every combination rather than spot-checked, because the whole
        /// class of bug is a combination nobody thought to try.
        /// </summary>
        [Fact]
        public void Fit_never_exceeds_the_ceiling_for_any_combination()
        {
            foreach (SkinrRenderResolution resolution in AllResolutions)
            {
                foreach (SkinrRenderQuality quality in AllQualities)
                {
                    SkinrRenderSize size = SkinrRenderResolutionPresets.Fit(
                        resolution, quality,
                        viewportWidth: 3840, viewportHeight: 2160,
                        displayWidth: 3840, displayHeight: 2160);

                    size.RenderPixels.Should().BeLessThanOrEqualTo(
                        SkinrRenderQualityPresets.MaxRenderPixels(quality),
                        "{0} at {1} must fit the tier's budget", resolution, quality);
                    size.Width.Should().BeGreaterThanOrEqualTo(64);
                    size.Height.Should().BeGreaterThanOrEqualTo(64);
                    (size.Width % 4).Should().Be(0);
                    (size.Height % 4).Should().Be(0);
                    size.Supersample.Should().BeOneOf(1, 2);
                }
            }
        }

        // --- the size record -------------------------------------------------

        [Fact]
        public void RenderPixels_counts_rasterised_pixels_not_output_pixels()
        {
            new SkinrRenderSize(1920, 1080, 1).RenderPixels.Should().Be(2_073_600);
            new SkinrRenderSize(1920, 1080, 2).RenderPixels.Should().Be(8_294_400);
        }

        [Fact]
        public void ToString_mentions_the_supersample_only_when_there_is_one()
        {
            new SkinrRenderSize(1280, 720, 1).ToString().Should().Be("1280×720");
            new SkinrRenderSize(1920, 1080, 2).ToString().Should().Be("1920×1080 ×2");
        }

        // --- the quality tier is now purely about sampling -------------------

        [Theory]
        [InlineData(SkinrRenderQuality.Preview, 1)]
        [InlineData(SkinrRenderQuality.Balanced, 1)]
        [InlineData(SkinrRenderQuality.High, 2)]
        public void Supersample_is_a_power_of_two(SkinrRenderQuality quality, int expected)
        {
            SkinrRenderQualityPresets.Supersample(quality).Should().Be(expected);
        }

        /// <summary>
        /// Cost per pass is the square of the supersample factor, and the measured numbers agree:
        /// on this project's GPU a 1920×1080 interactive frame is ~16 ms flat and ~62 ms at ×2.
        /// </summary>
        [Theory]
        [InlineData(SkinrRenderQuality.Balanced, 1.0)]
        [InlineData(SkinrRenderQuality.High, 4.0)]
        public void RelativeCost_is_the_square_of_the_supersample(
            SkinrRenderQuality quality, double expected)
        {
            SkinrRenderQualityPresets.RelativeCost(quality).Should().Be(expected);
        }

        [Fact]
        public void Only_Preview_caps_the_pixel_count_below_the_hardware_ceiling()
        {
            SkinrRenderQualityPresets.MaxRenderPixels(SkinrRenderQuality.Preview)
                .Should().BeLessThan(SkinrRenderResolutionPresets.MaxRenderPixels);
            SkinrRenderQualityPresets.MaxRenderPixels(SkinrRenderQuality.Balanced)
                .Should().Be(SkinrRenderResolutionPresets.MaxRenderPixels);
            SkinrRenderQualityPresets.MaxRenderPixels(SkinrRenderQuality.High)
                .Should().Be(SkinrRenderResolutionPresets.MaxRenderPixels);
        }

        /// <summary>
        /// The tier description must not quote a resolution any more. It used to, back when the tier
        /// decided the size, and a picker reading "High (1600×1200 ×2)" beside a resolution picker
        /// set to 1920×1080 states something untrue about the frame on screen.
        /// </summary>
        [Fact]
        public void Quality_descriptions_no_longer_quote_a_resolution()
        {
            foreach (SkinrRenderQuality quality in AllQualities)
            {
                string described = SkinrRenderQualityPresets.Describe(quality);
                described.Should().NotBeNullOrWhiteSpace();
                described.Should().NotContain("×1", "a resolution would read like 1600×1200");
                described.Should().NotContain("1080");
                described.Should().NotContain("768");
            }
        }

        // --- localisation ----------------------------------------------------

        /// <summary>
        /// <see cref="Loc.Get"/> returns the key itself when a key is missing, so a picker whose
        /// keys were never added reads "Skinr.Res1080" to the user rather than throwing. Asserting
        /// the returned text differs from the key is what catches that.
        /// </summary>
        [Fact]
        public void Every_resolution_has_a_localisation_key_that_actually_resolves()
        {
            foreach (SkinrRenderResolution resolution in AllResolutions)
            {
                string key = SkinrRenderResolutionPresets.NameKey(resolution);
                key.Should().StartWith("Skinr.");
                Loc.Get(key).Should().NotBe(key, "{0} needs a string in ui-strings-en.txt", key);
            }
        }

        [Fact]
        public void Every_quality_tier_has_a_localisation_key_that_actually_resolves()
        {
            foreach (SkinrRenderQuality quality in AllQualities)
            {
                string key = SkinrRenderQualityPresets.NameKey(quality);
                key.Should().StartWith("Skinr.");
                Loc.Get(key).Should().NotBe(key, "{0} needs a string in ui-strings-en.txt", key);
            }
        }

        /// <summary>Distinct keys, or two entries in the picker read identically.</summary>
        [Fact]
        public void Resolution_name_keys_are_distinct()
        {
            var seen = new System.Collections.Generic.HashSet<string>(StringComparer.Ordinal);
            foreach (SkinrRenderResolution resolution in AllResolutions)
                seen.Add(SkinrRenderResolutionPresets.NameKey(resolution)).Should()
                    .BeTrue("{0} reuses another entry's key", resolution);
        }

        // --- the boot options agree with the helper --------------------------

        /// <summary>
        /// <c>ApplyQuality</c> writes the sidecar's BOOT size. It has to produce exactly what
        /// <see cref="SkinrRenderResolutionPresets.Fit"/> produces, because the view model then
        /// tracks the same numbers to decide whether a resize is needed at all — a disagreement here
        /// shows up as a resize that fires on every frame or one that never fires.
        /// </summary>
        [Theory]
        [InlineData(SkinrRenderQuality.Preview, SkinrRenderResolution.Hd720)]
        [InlineData(SkinrRenderQuality.Balanced, SkinrRenderResolution.Fhd1080)]
        [InlineData(SkinrRenderQuality.High, SkinrRenderResolution.Fhd1080)]
        [InlineData(SkinrRenderQuality.High, SkinrRenderResolution.MatchViewport)]
        public void ApplyQuality_matches_Fit(
            SkinrRenderQuality quality, SkinrRenderResolution resolution)
        {
            var options = new SkinrSidecarOptions();
            options.ApplyQuality(quality, resolution);

            SkinrRenderSize expected = SkinrRenderResolutionPresets.Fit(resolution, quality);
            options.Width.Should().Be(expected.Width);
            options.Height.Should().Be(expected.Height);
            options.Supersample.Should().Be(expected.Supersample);
        }
    }
}
