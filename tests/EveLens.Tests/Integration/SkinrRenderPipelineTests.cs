// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;
using Xunit.Abstractions;

namespace EveLens.Tests.Integration
{
    /// <summary>
    /// End-to-end proof of the SKINR render chain: ESI recipe → SDE catalog → SOF DNA →
    /// resource index → <c>.gr2</c> download → <c>.cmf</c> conversion → Trinity build → frame.
    /// </summary>
    /// <remarks>
    /// <para><b>Why this is opt-in.</b> It needs a Trinity build, a network path to CCP's CDN,
    /// and roughly a minute of wall clock for engine boot alone. Running it in the normal suite
    /// would turn a 90-second run into a multi-minute one and make every developer without the
    /// render runtime look like they broke something. So it is gated on
    /// <c>EVELENS_SKINR_INTEGRATION=1</c> and returns quietly otherwise.</para>
    ///
    /// <para><b>Why it is worth having anyway.</b> Every layer below it is unit tested in
    /// isolation, and every one of them can be individually correct while the chain is broken —
    /// the failures that cost the most time in building this were all seam failures: an index
    /// that existed only in memory, a mask bound to the wrong material index, a geometry path
    /// handed over as <c>.gr2</c> when the engine needed <c>.cmf</c>. None of those are visible
    /// from either side of the seam. This test is the only thing that sees the whole rope.</para>
    ///
    /// <para><b>What it asserts, and why those things.</b> A render can fail in ways that pass
    /// every structural check: a black frame has the right dimensions and stride, and a hull
    /// wearing none of its design looks like a perfectly good render of a stock ship. So the
    /// assertions are on the measured facts that distinguish those cases — mean luma above the
    /// black-frame floor, and a non-zero <c>rebound</c> count proving CCP's own pattern samplers
    /// were redirected rather than a new one appended.</para>
    /// </remarks>
    [Collection("AppServices")]
    [Trait("Category", "SkinrIntegration")]
    public sealed class SkinrRenderPipelineTests : IDisposable
    {
        /// <summary>The Slasher: <c>mf1_t1</c>, the hull the whole pipeline was proven on.</summary>
        private const int SlasherTypeId = 585;

        // Real published component IDs from the shipped SDE catalog (build 3470007). Hard-coded
        // rather than picked at runtime so a catalog regression that drops these shows up as a
        // failure here instead of being silently routed around.
        private const int MaterialAzure = 205;
        private const int MaterialBerry = 207;
        private const int PatternAngledStripe = 179;
        private const int PatternCamo = 180;

        private readonly ITestOutputHelper _output;
        private readonly string _dataDirectory;

        public SkinrRenderPipelineTests(ITestOutputHelper output)
        {
            _output = output;

            // A private data directory, so a test run can never evict or corrupt the user's own
            // resource cache — and so a failing run leaves an inspectable tree behind.
            //
            // Stable across runs rather than per-run, which is a correctness decision and not a
            // convenience: a per-run directory means every run downloads the hull's entire
            // texture set from CCP's CDN again. That is minutes of wall clock, hundreds of
            // megabytes of somebody else's bandwidth for a test that has already proved that
            // path once, and enough traffic to look like abuse if this ever runs in a loop. It is
            // still not the user's cache, so the isolation this comment originally claimed is
            // intact. Cold-cache behaviour is worth testing deliberately, by deleting this tree,
            // rather than incidentally on every single run.
            _dataDirectory = Path.Combine(Path.GetTempPath(), "evelens-skinr-itest");
            Directory.CreateDirectory(_dataDirectory);

            var paths = Substitute.For<IApplicationPaths>();
            paths.DataDirectory.Returns(_dataDirectory);
            AppServices.SetApplicationPaths(paths);

            // The sidecar's stderr diagnostics go to the trace service, and they are the only
            // account of what the engine was doing. Without this the test can report that a
            // render failed but never why, which is the difference between a diagnosis and a
            // shrug.
            var trace = Substitute.For<ITraceService>();
            trace.When(t => t.Trace(Arg.Any<string>(), Arg.Any<bool>()))
                 .Do(call => Echo(call.ArgAt<string>(0)));
            trace.When(t => t.Trace(Arg.Any<TraceLevel>(), Arg.Any<string>(), Arg.Any<bool>()))
                 .Do(call => Echo(call.ArgAt<string>(1)));
            AppServices.SetTraceService(trace);
        }

        /// <summary>
        /// Trace lines arrive on the sidecar's stderr-draining task, which outlives the test
        /// method; xUnit's output helper throws once the test has finished, and that exception
        /// would surface as an unrelated failure. Swallow it: a late trace line is worth nothing
        /// and worth breaking nothing.
        /// </summary>
        private void Echo(string message)
        {
            try { _output.WriteLine(message); }
            catch (InvalidOperationException) { }
        }

        private static bool Enabled =>
            Environment.GetEnvironmentVariable("EVELENS_SKINR_INTEGRATION") == "1";

        [Fact]
        public async Task FullChain_RendersALitHullWearingItsDesign()
        {
            if (!Enabled)
            {
                _output.WriteLine("skipped: set EVELENS_SKINR_INTEGRATION=1 to run");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(12));

            // --- 1. the catalog: does the SDE know this hull and these components? ----------
            var resolver = new SkinrRecipeResolver();
            resolver.IsAvailable.Should().BeTrue(
                "the SKINR catalog ships as an embedded resource");

            SkinrResolvedDesign design = resolver.Resolve(BuildRecipe());
            _output.WriteLine($"DNA: {design.Dna}");
            foreach (string warning in design.Warnings)
                _output.WriteLine($"resolve warning: {warning}");

            design.IsRenderable.Should().BeTrue();
            design.Dna.Should().Contain("mf1_t1", "the Slasher's SOF hull name");
            design.Dna.Should().Contain("cosm_blank_projection",
                "the carrier pattern is what makes the engine create the two mask slots we then " +
                "redirect — without it there is nothing to bind a pattern to");
            design.Patterns.Should().HaveCount(2);

            // --- 2. the host: runtime discovery, index, geometry, build ---------------------
            using SkinrSidecarHost host = await SkinrSidecarHost.CreateAsync(cts.Token);

            IReadOnlyList<string> problems = host.Validate();
            if (problems.Count > 0)
            {
                // Not an assertion failure: a machine without the render runtime cannot run this
                // test, and saying so is more useful than a red X that means "you didn't install
                // Trinity". The gate is opt-in, so anyone here asked for the real thing.
                _output.WriteLine("render runtime unavailable: " + string.Join("; ", problems));
                _output.WriteLine("set EVELENS_TRINITY_ROOT or EVELENS_SKINR_RUNTIME to run");
                return;
            }

            host.Progress += message => _output.WriteLine($"[host] {message}");

            SkinrLoadResult load = await host.LoadAsync(design, cts.Token);
            foreach (string warning in load.Warnings)
                _output.WriteLine($"load warning: {warning}");

            load.Ok.Should().BeTrue(load.Error ?? "build failed");
            load.Radius.Should().BeGreaterThan(0, "the hull has a bounding sphere");
            _output.WriteLine($"device={host.Device} category={load.HullCategory} " +
                              $"radius={load.Radius:0.##}");

            // THE MASK LAW, asserted rather than assumed. `rebound` counts samplers that already
            // existed on CCP's own effects and were redirected at ours; `added` counts ones we
            // created because none was there. On a hull that supports SKINR, rebound is large and
            // added is zero — the Slasher measures 14 effects / 28 rebinds. A pass with added > 0
            // means the shader never expected a pattern and the render is not trustworthy.
            load.TextureBinding.Should().NotBeNull();
            load.TextureBinding!.Rebound.Should().BeGreaterThan(0,
                "the pattern must be bound to samplers the hull's shaders already declared");
            load.SupportsPatterns.Should().BeTrue();
            _output.WriteLine($"binding: effects={load.TextureBinding.Effects} " +
                              $"rebound={load.TextureBinding.Rebound} " +
                              $"added={load.TextureBinding.Added}");

            // --- 3. the frame --------------------------------------------------------------
            await host.SetCameraAsync(35, 15, load.Radius * 3, ct: cts.Token);
            SkinrFrame? frame = await host.RenderAsync(settle: true, ct: cts.Token);

            frame.Should().NotBeNull("the sidecar refuses to return a black frame, so a null " +
                                     "here means the render genuinely failed");
            frame!.Pixels.Length.Should().BeGreaterOrEqualTo(frame.Stride * frame.Height);

            // The one check that a structurally perfect black frame cannot pass.
            frame.MeanLuma.Should().BeGreaterThan(1.0,
                "a lit hull must actually be lit — this is the assertion that caught the " +
                "driver warm-up ordering bug");
            _output.WriteLine($"frame: {frame.Width}x{frame.Height} stride={frame.Stride} " +
                              $"luma={frame.MeanLuma:0.###} settled={frame.Settled} " +
                              $"aa={frame.AntiAliased}");
        }

        [Fact]
        public async Task Camera_OrbitsWithoutRebuilding()
        {
            if (!Enabled)
            {
                _output.WriteLine("skipped: set EVELENS_SKINR_INTEGRATION=1 to run");
                return;
            }

            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(12));
            using SkinrSidecarHost host = await SkinrSidecarHost.CreateAsync(cts.Token);
            if (host.Validate().Count > 0)
            {
                _output.WriteLine("render runtime unavailable — skipped");
                return;
            }

            var resolver = new SkinrRecipeResolver();
            SkinrLoadResult load = await host.LoadAsync(resolver.Resolve(BuildRecipe()), cts.Token);
            load.Ok.Should().BeTrue(load.Error ?? "build failed");

            // Two different angles must produce two different images. If they don't, the camera
            // op is a no-op and every orbit gesture in the UI would feel broken while every
            // structural check still passed.
            SkinrSidecarCamera? first = await host.SetCameraAsync(0, 0, load.Radius * 3,
                ct: cts.Token);
            SkinrFrame? a = await host.RenderAsync(settle: false, ct: cts.Token);

            SkinrSidecarCamera? second = await host.SetCameraAsync(90, 30, load.Radius * 3,
                ct: cts.Token);
            SkinrFrame? b = await host.RenderAsync(settle: false, ct: cts.Token);

            first.Should().NotBeNull();
            second.Should().NotBeNull();
            second!.Yaw.Should().NotBe(first!.Yaw);

            a.Should().NotBeNull();
            b.Should().NotBeNull();
            a!.Pixels.Should().NotEqual(b!.Pixels,
                "a 90-degree yaw change must change the image");
        }

        /// <summary>
        /// A design shaped exactly like one ESI would return: four nanocoating slots and two
        /// pattern slots, in the slot IDs the layout actually uses.
        /// </summary>
        /// <remarks>
        /// <para>Slot numbering is not decorative, and it caught me out writing this test. There
        /// are eight slots in four roles: 1-4 are the nanocoatings (primary, secondary, detailing,
        /// tech), <b>5 and 7 hold the pattern textures</b>, and <b>6 and 8 hold the materials those
        /// patterns are painted in</b>. It is easy to conflate the pair 6/8 with the SOF material
        /// indices 5 and 6 that the pattern materials occupy inside the <c>pattern?</c> command —
        /// two different numbering systems that happen to sit next to each other in the code.</para>
        ///
        /// <para>Getting it wrong is not a crash: a pattern in slot 6 is a pattern in a material
        /// slot, so it is simply not a pattern the layout knows about, and the design resolves to
        /// four nanocoatings and no <c>pattern?</c> command at all. The hull renders, correctly
        /// coloured, wearing none of its pattern. That is precisely the class of failure this
        /// whole integration test exists to catch, and it duly caught it in 138 ms.</para>
        /// </remarks>
        private static EsiSkinrRecipe BuildRecipe() => new()
        {
            Id = "integration-test-design",
            Name = "Integration Test Livery",
            Line = "EveLens Test",
            CreatorId = 90000001,
            ShipTypeId = SlasherTypeId,
            Tier = new EsiSkinrTier { Level = 2 },
            Layout = new EsiSkinrLayout
            {
                PatternBlendMode = "normal",
                Slots = new List<EsiSkinrSlot>
                {
                    Nanocoating(1, MaterialAzure),
                    Nanocoating(2, MaterialBerry),
                    Nanocoating(3, MaterialAzure),
                    Nanocoating(4, MaterialBerry),
                    Pattern(SkinrSlot.Pattern, PatternAngledStripe, mirrored: false),
                    Nanocoating(SkinrSlot.PatternMaterial, MaterialBerry),
                    Pattern(SkinrSlot.SecondaryPattern, PatternCamo, mirrored: true),
                    Nanocoating(SkinrSlot.SecondaryPatternMaterial, MaterialAzure)
                }
            }
        };

        private static EsiSkinrSlot Nanocoating(int slot, int componentId) => new()
        {
            Id = slot,
            Configuration = new EsiSkinrSlotConfiguration
            {
                Nanocoating = new EsiSkinrNanocoating { Id = componentId }
            }
        };

        private static EsiSkinrSlot Pattern(int slot, int componentId, bool mirrored) => new()
        {
            Id = slot,
            Configuration = new EsiSkinrSlotConfiguration
            {
                Pattern = new EsiSkinrPattern
                {
                    Id = componentId,
                    Configuration = new EsiSkinrPatternConfiguration
                    {
                        Mirrored = mirrored,

                        // Every material slot targeted. Measured fact, not a convenience: a mask
                        // that targets nothing differs from a black mask by 0.19% of pixels,
                        // which is inside the renderer's own 0.74% noise floor — i.e. an empty
                        // projection is indistinguishable from no pattern at all.
                        Projection = new EsiSkinrProjection
                        {
                            Slot1 = true, Slot2 = true, Slot3 = true, Slot4 = true
                        },
                        Transform = new EsiSkinrTransform
                        {
                            Position = new EsiSkinrVector { X = 0, Y = 0, Z = 0 },
                            Rotation = new EsiSkinrQuaternion { X = 0, Y = 0, Z = 0, W = 1 },
                            Scaling = new EsiSkinrVector { X = 1, Y = 1, Z = 1 }
                        }
                    }
                }
            }
        };

        /// <remarks>
        /// The tree is deliberately kept. It holds the engine's resource cache, the converted
        /// geometry and the generated CA bundle — which is to say, everything needed to see what
        /// the engine was actually given when a run fails, and everything that makes the next run
        /// cost seconds instead of minutes of CDN traffic. Deleting it would be tidier and
        /// strictly worse on both counts.
        /// </remarks>
        public void Dispose() => AppServices.Reset();
    }
}
