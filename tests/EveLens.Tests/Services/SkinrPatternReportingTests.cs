// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Regression tests for the SKINR renderer's pattern-layer accounting.
    /// </summary>
    /// <remarks>
    /// <para><b>The bug these exist for.</b> A design's pattern layer is dropped when its pattern
    /// component is not in the catalog, because binding an empty texture path to a sampler paints
    /// nothing and reports no error. That drop is correct. What was not correct is that
    /// <c>SkinrSidecarHost.Interpret</c> then re-derived "how many layers did we expect" using the
    /// <em>same predicate that had done the dropping</em>. So when every layer dropped, expected
    /// became zero, "this hull supports patterns" became true, and a hull rendered with none of
    /// its design reported unqualified success — the one failure mode a user cannot detect for
    /// themselves.</para>
    ///
    /// <para>It shipped that way long enough for an Astero to be photographed side by side with
    /// the game, where a single red stripe runs the whole hull length crossing every mesh
    /// boundary, against our disconnected rectangular blocks that stop at each boundary. A stripe
    /// crossing boundaries is a projected pattern mask; blocks stopping at them are per-area
    /// coating tint. The nanocoatings were landing and the pattern was painting nothing, silently.
    /// </para>
    ///
    /// <para><b>What is asserted.</b> Not the wording of the warnings — that will change — but
    /// the facts a user needs to be told: that layers went missing, how many of how many, and
    /// which component IDs, since the ID is what makes a stale catalog fixable rather than
    /// mysterious. Plus the structural guarantee that makes the blind spot unreconstructable: one
    /// property decides drawability, and the count it is compared against comes from ESI's recipe.
    /// </para>
    /// </remarks>
    public sealed class SkinrPatternReportingTests
    {
        private const int KnownPatternId = 179;
        private const int UnknownPatternId = 9179;

        // res:/texture/projection/*.dds is what a real category-2 component carries; the value
        // only has to be non-empty for these tests, but a realistic one keeps the fixture honest
        // about what is being modelled.
        private static SkinrComponent PatternComponent(int id, string resourceFile) =>
            new(id, SkinrText.None, SkinrComponentCategory.Pattern, 1, "Matte", resourceFile,
                string.Empty, string.Empty, SkinrProjectionType.ClampToBorder,
                SkinrProjectionType.ClampToEdge, true, null, Array.Empty<SkinrAssociatedType>());

        private static SkinrComponent MaterialComponent(int id) =>
            new(id, SkinrText.None, SkinrComponentCategory.Material, 1, "Gloss",
                "res:/dx9/model/SpaceObjectFactory/materials/cosm_azure_gloss_000_030_100.red",
                "cosm_azure_gloss_000_030_100", string.Empty, SkinrProjectionType.Repeat,
                SkinrProjectionType.Repeat, true, null, Array.Empty<SkinrAssociatedType>());

        private static SkinrResolvedPattern Layer(int layerIndex, SkinrComponent? pattern,
            int patternComponentId) =>
            new(layerIndex, layerIndex == 0 ? SkinrSlot.Pattern : SkinrSlot.SecondaryPattern,
                layerIndex == 0 ? SkinrSlot.PatternMaterial : SkinrSlot.SecondaryPatternMaterial,
                pattern, MaterialComponent(205), patternComponentId, 205,
                new double[] { 0, 0, 0 }, new double[] { 0, 0, 0, 1 },
                new double[] { 1, 1, 1 }, false, new double[] { 1, 0, 0, 0 });

        private static SkinrResolvedDesign Design(params SkinrResolvedPattern[] patterns) =>
            new("test-skinr", "Test Design", "Test Line", 90000001L, 585, null, 1, "normal",
                "mf1_t1:material1", Array.Empty<SkinrResolvedMaterial>(), patterns,
                SkinrSlotConfiguration.Unknown, 0, 1, null, Array.Empty<string>());

        /// <summary>A build response that reports the shaders took both mask samplers.</summary>
        private static SkinrSidecarResponse Built(int rebound, int[]? unclaimed = null) =>
            new()
            {
                Ok = true,
                Radius = 60.0,
                TextureBinding = new SkinrSidecarTextureBinding
                { Rebound = rebound, Added = 0, Effects = 14 },
                Masks = new SkinrSidecarMaskReport
                {
                    Preexisting = 2, Overridden = rebound > 0 ? 2 : 0, Appended = 0, Failed = 0,
                    Unclaimed = unclaimed
                }
            };

        private static SkinrSidecarResponse Resolved() =>
            new() { Ok = true, HullKnown = true, DnaValid = true, Category = "ship" };

        // --- the model-level guarantee ----------------------------------------

        [Fact]
        public void APatternWithNoMaskTexture_IsNotDrawable()
        {
            Layer(0, null, UnknownPatternId).IsDrawable.Should().BeFalse(
                "a null pattern component yields an empty resource path, and binding an empty " +
                "path to a sampler paints nothing while reporting nothing");
        }

        [Fact]
        public void APatternWithAMaskTexture_IsDrawable()
        {
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");

            Layer(0, pattern, KnownPatternId).IsDrawable.Should().BeTrue();
        }

        [Fact]
        public void DrawableAndUndrawable_PartitionPatternsExactly()
        {
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");
            SkinrResolvedDesign design = Design(
                Layer(0, pattern, KnownPatternId),
                Layer(1, null, UnknownPatternId));

            // The partition is the whole point: every layer is in exactly one side, so a count
            // taken from either one can be compared against Patterns.Count without a third
            // predicate existing anywhere to disagree with.
            design.DrawablePatterns.Should().HaveCount(1);
            design.UndrawablePatterns.Should().HaveCount(1);
            design.DrawablePatterns.Concat(design.UndrawablePatterns)
                .Should().BeEquivalentTo(design.Patterns);
        }

        // --- the reporting the bug suppressed ---------------------------------

        [Fact]
        public void EveryPatternDropped_StillWarns()
        {
            // The exact shape of the Astero bug: ESI sent a pattern, nothing was sent to the
            // renderer, and the old code's `expected` collapsed to zero along with the warning.
            SkinrResolvedDesign design = Design(Layer(0, null, UnknownPatternId));

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                design, Resolved(), Built(rebound: 0), new List<string>());

            result.Ok.Should().BeTrue("the nanocoatings still render — this is a warning, not a " +
                                      "failure");
            result.Warnings.Should().NotBeEmpty(
                "a design that renders with none of its pattern layers must say so");
            result.Warnings.Should().Contain(w => w.Contains(UnknownPatternId.ToString()),
                "the unknown component ID is what makes a stale catalog actionable");
        }

        [Fact]
        public void EveryPatternDropped_DoesNotBlameTheHullsShaders()
        {
            // We never asked the shaders for a mask, so we cannot know whether they support one.
            // Claiming they do not would send the user hunting the wrong culprit — and would be
            // the same class of error as the bug: a confident report about an untaken measurement.
            SkinrResolvedDesign design = Design(Layer(0, null, UnknownPatternId));

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                design, Resolved(), Built(rebound: 0), new List<string>());

            result.Warnings.Should().NotContain(w => w.Contains("shaders do not support"));
        }

        [Fact]
        public void SomePatternsDropped_ReportsTheFractionOfTheRecipe()
        {
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");
            SkinrResolvedDesign design = Design(
                Layer(0, pattern, KnownPatternId),
                Layer(1, null, UnknownPatternId));

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                design, Resolved(), Built(rebound: 28), new List<string>());

            // "1 of 2" rather than "1": the denominator is ESI's recipe, and it is the number
            // whose absence made the bug possible.
            result.Warnings.Should().Contain(w => w.Contains("1 of") && w.Contains("2"));
            result.SupportsPatterns.Should().BeTrue("one layer did reach the shaders and rebound");
        }

        [Fact]
        public void NoPatternsDropped_WarnsAboutNothing()
        {
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");
            SkinrResolvedDesign design = Design(Layer(0, pattern, KnownPatternId));

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                design, Resolved(), Built(rebound: 14), new List<string>());

            result.Warnings.Should().BeEmpty("a clean build must stay quiet, or the warnings " +
                                             "stop meaning anything");
        }

        [Fact]
        public void ADesignWithNoPatterns_WarnsAboutNothing()
        {
            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(), Resolved(), Built(rebound: 0), new List<string>());

            result.Warnings.Should().BeEmpty();
            result.SupportsPatterns.Should().BeTrue();
        }

        // --- the field that arrived and was thrown away -----------------------

        [Fact]
        public void UnclaimedEngineMasks_ReachTheResult()
        {
            // The sidecar reports `unclaimed: [4, 5]` — both SKINR pattern carriers left inert,
            // still bound to res:/texture/global/black.dds. The field was parsed off the wire and
            // never read by anything, for the whole investigation.
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(Layer(0, pattern, KnownPatternId)), Resolved(),
                Built(rebound: 14, unclaimed: new[] { 4, 5 }), new List<string>());

            result.Masks!.Unclaimed.Should().BeEquivalentTo(new[] { 4, 5 });
            result.Warnings.Should().NotBeEmpty(
                "an inert mask with no dropped layer to explain it is a resolver signal");
        }

        [Fact]
        public void AOnePatternDesign_LeavesTheSecondEngineSlotIdleAndSaysNothing()
        {
            // The false alarm this closes, measured on a Charon: `patterns 1/1 sent, masks 1
            // overridden, unclaimed [5]` — and a warning that the design's paint might be
            // incomplete while it rendered exactly right.
            //
            // The engine seeds a FIXED TWO mask carriers on every SKINR-capable hull, materialIndex
            // 4 and 5 (SOURCE_PATTERN1 / _PATTERN2), capped by EVE_SPACEOBJECT_CUSTOWMASK_MAX and
            // pre-bound to res:/texture/global/black.dds. A design with one pattern can only ever
            // claim one of them, so slot 5 is idle BY CONSTRUCTION. `Unclaimed` counts engine slots;
            // reading it as a count of design layers accused every single-pattern design in the game
            // of being broken.
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(Layer(0, pattern, KnownPatternId)), Resolved(),
                Built(rebound: 6, unclaimed: new[] { 5 }), new List<string>());

            result.Masks!.Unclaimed.Should().BeEquivalentTo(new[] { 5 },
                because: "the raw indices stay on the result even when they are benign — the trace "
                       + "line still prints them");
            result.Warnings.Should().BeEmpty(
                because: "one layer sent into two seeded slots leaves exactly one idle, which is "
                       + "the correct outcome for every one-pattern design and not a loss");
        }

        [Fact]
        public void ATwoPatternDesign_WithASlotStillIdle_DoesWarn()
        {
            // The case the warning exists for, and the one the slot/layer confusion was hiding.
            // Both layers were sent, so the engine's two slots should both be claimed. One is not,
            // which means a layer we drove did not land.
            SkinrComponent first = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");
            SkinrComponent second = PatternComponent(
                KnownPatternId + 1, "res:/texture/projection/cosm_Chevron_01_2k.dds");

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(Layer(0, first, KnownPatternId), Layer(1, second, KnownPatternId + 1)),
                Resolved(), Built(rebound: 6, unclaimed: new[] { 5 }), new List<string>());

            result.Warnings.Should().ContainSingle()
                .Which.Should().Contain("1 pattern layer(s) inert");
        }

        [Fact]
        public void TheWarningCountsOnlyTheSlotsThatShouldHaveBeenClaimed()
        {
            // One layer sent, both slots idle: one of those is expected, the other is not. Reporting
            // two would overstate the loss, and the number is the whole point of the sentence.
            SkinrComponent pattern = PatternComponent(
                KnownPatternId, "res:/texture/projection/cosm_AngledStripe_01_2k.dds");

            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(Layer(0, pattern, KnownPatternId)), Resolved(),
                Built(rebound: 6, unclaimed: new[] { 4, 5 }), new List<string>());

            result.Warnings.Should().ContainSingle()
                .Which.Should().Contain("1 pattern layer(s) inert");
        }

        [Fact]
        public void UnclaimedMasks_AreNotReportedTwiceWhenADropExplainsThem()
        {
            // Dropping a layer necessarily leaves its engine mask unclaimed, so both signals fire
            // from one cause. Two warnings for one fact reads as two problems.
            SkinrLoadResult result = SkinrSidecarHost.Interpret(
                Design(Layer(0, null, UnknownPatternId)), Resolved(),
                Built(rebound: 0, unclaimed: new[] { 4 }), new List<string>());

            result.Warnings.Should().HaveCount(1);
            result.Warnings[0].Should().Contain(UnknownPatternId.ToString());
        }

        // --- the backdrop contract --------------------------------------------

        [Fact]
        public void SidecarArguments_RequestTheRoomBackdropByDefault()
        {
            // Measured, not preferred: A/B'd inside one sidecar process, the room lands at 1.03x
            // the game's backdrop luma with 5.6x the edge energy, against 1.43x and a flat fill
            // for `studio`. The default has to be the one that matches, and the host has to be
            // the side that says so — a renderer setting only the sidecar knows about is a
            // setting nobody can find when it is wrong.
            var options = new SkinrSidecarOptions();
            List<string> args = options.BuildArguments().ToList();

            options.Backdrop.Should().Be("room");
            args.Should().ContainInOrder("--backdrop", "room");
        }

        [Fact]
        public void SidecarArguments_CarryAnOverriddenBackdrop()
        {
            var options = new SkinrSidecarOptions { Backdrop = "nebula" };

            options.BuildArguments().ToList().Should().ContainInOrder("--backdrop", "nebula");
        }

        [Fact]
        public void SidecarArguments_TurnCcpsStarsOffByDefault()
        {
            // CCP's room brings a grey gradient AND per-faction nebula plates, and only the first
            // one is a studio backdrop. Their own SKINR Studio has no stars behind the ship, so
            // the plates were never parity — they were an in-space backdrop scored against a
            // studio reference, which is how a 1.02x luma match got read as a match.
            var options = new SkinrSidecarOptions();

            options.Stars.Should().BeFalse();
            options.BuildArguments().ToList().Should().ContainInOrder("--stars", "off");
        }

        [Fact]
        public void SidecarArguments_CanPutTheStarsBack()
        {
            var options = new SkinrSidecarOptions { Stars = true };

            options.BuildArguments().ToList().Should().ContainInOrder("--stars", "on");
        }

        [Fact]
        public void SidecarArguments_AnchorTheBackdropToTheOrbit()
        {
            // The backdrop vanishing when you zoom in is a RATIO bug with two ends, and only one
            // of them was ever suspected. CCP's room is authored at a fixed 300,000 units; the
            // near plane is max(0.05, distance * 0.005). So near/room is 1.5e-6 on an Astero and
            // 5.0e-5 on a Charon — a 33x swing in depth resolution at the backdrop's own depth,
            // which nobody chose. Anchoring the room to the orbit holds the ratio constant.
            //
            // 79.6 is the Charon's own 300,000/3770, so 80 reproduces the measured-good capital
            // framing and only moves the hulls that are currently broken. A fix that also moves
            // the case that already works is a trade, not a fix.
            var options = new SkinrSidecarOptions();

            options.RoomAnchor.Should().Be(80.0);
            options.BuildArguments().ToList().Should().ContainInOrder("--room-anchor", "80");
        }

        [Fact]
        public void SidecarArguments_WriteTheAnchorInvariantly()
        {
            // A comma decimal separator reaches the sidecar's float() as a hard parse failure at
            // boot, and this project ships to a Chinese and a Korean locale. The renderer's
            // argument vector is machine-to-machine and must never be culture-formatted.
            var options = new SkinrSidecarOptions { RoomAnchor = 62.5 };

            options.BuildArguments().ToList().Should().ContainInOrder("--room-anchor", "62.5");
        }
    }
}
