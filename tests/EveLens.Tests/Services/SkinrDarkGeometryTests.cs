// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Text.Json;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    /// <summary>
    /// Which meshes the host treats as dark, and why one list was never enough.
    /// </summary>
    /// <remarks>
    /// <para><b>The blind spot these close.</b> The renderer's geometry report grew an
    /// <c>unmapped</c> list to catch the defect that had kept eighteen of a Rifter's nineteen meshes
    /// invisible: a mesh pointing at a Granny <c>.gr2</c> Trinity cannot read, which loads,
    /// registers, resolves a shader, reports <c>display=True</c> and draws nothing. That list can
    /// only ever name a <c>.gr2</c>, because that is the extension the repointer looks for.</para>
    ///
    /// <para>So the same defect one step later is invisible to it. A mesh already pointing at a
    /// <c>.cmf</c> that is <em>not in the resource tree</em> is counted as <c>native</c> — "nothing
    /// to do" — contributes to no unmapped list, and draws nothing. An Astero found it: two native
    /// meshes, an empty unmapped list, every count healthy, and a viewport containing nothing but
    /// its spotlights. <c>notLoaded</c> is the complement, and it is measured rather than inferred:
    /// a mesh either holds a live geometry resource after the loads are pumped or it does not.</para>
    ///
    /// <para>This is not a hypothetical failure. EveLens LRU-prunes its resource cache at 2 GB, so a
    /// converted <c>.cmf</c> disappearing from under a design that is still on screen is a case that
    /// will happen in the field — which is why the host asks the converter for the Granny original
    /// again instead of only logging the loss.</para>
    /// </remarks>
    public sealed class SkinrDarkGeometryTests
    {
        private static readonly JsonSerializerOptions s_json = new();

        [Fact]
        public void NotLoadedSurvivesTheWire()
        {
            // The Astero's real shape: two native meshes, nothing unmapped, and no hull on screen.
            const string json = """
            {
              "ok": true,
              "shipGeometry": {
                "repointed": 0, "native": 2, "meshes": 2, "loaded": 0, "unmapped": [],
                "notLoaded": [
                  "res:/dx9/model/ship/gallente/frigate/soef1/soef1_t1.cmf"
                ]
              }
            }
            """;

            SkinrSidecarResponse response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(json, s_json)!;

            response.ShipGeometry!.Unmapped.Should().BeEmpty(
                because: "an unmapped list can only name a .gr2, so it is silent on this defect");
            response.ShipGeometry.Native.Should().Be(2);
            response.ShipGeometry.NotLoaded.Should().ContainSingle()
                .Which.Should().EndWith(".cmf");
        }

        [Fact]
        public void AnOlderRendererWithoutTheFieldIsNotAShipFullOfHoles()
        {
            // Absent and empty must read the same here, and only here: the field is new, so a
            // sidecar that predates it reports no notLoaded at all. Defaulting that to "everything
            // is dark" would send the host converting a hull that renders perfectly well.
            const string json = """
            { "ok": true, "shipGeometry": { "repointed": 18, "native": 2, "meshes": 20,
                                            "loaded": 20, "unmapped": [] } }
            """;

            SkinrSidecarResponse response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(json, s_json)!;

            response.ShipGeometry!.NotLoaded.Should().BeEmpty();
            SkinrSidecarHost.Dark(response.ShipGeometry).Should().BeEmpty();
        }

        [Fact]
        public void DarkIsTheUnionOfBothListsWithNoDuplicates()
        {
            // A .gr2 with no conversion legitimately appears in both — it is unmapped, and it also
            // failed to load. One conversion attempt, not two.
            var report = new SkinrSidecarGeometryReport
            {
                Unmapped = new List<string>
                {
                    "res:/dx9/model/ship/minmatar/frigate/mf1/mf1_t1_exhaust_geo_01a.gr2"
                },
                NotLoaded = new List<string>
                {
                    "res:/dx9/model/ship/minmatar/frigate/mf1/mf1_t1_exhaust_geo_01a.gr2",
                    "res:/dx9/model/ship/gallente/frigate/soef1/soef1_t1.cmf",
                    "   "
                }
            };

            List<string> dark = SkinrSidecarHost.Dark(report);

            dark.Should().HaveCount(2);
            dark.Should().Contain("res:/dx9/model/ship/gallente/frigate/soef1/soef1_t1.cmf");
            dark.Should().NotContain("   ",
                because: "a blank path is not a mesh, and asking the converter for one wastes a "
                       + "subprocess launch to fail");
        }

        [Fact]
        public void ANullReportIsNotADarkShip()
        {
            // The room's Geometry is null when the environment never attached at all. "We do not
            // know" must not become "convert nothing", and it must not become "convert everything"
            // either — there is no list to act on, so there is no work.
            SkinrSidecarHost.Dark(null).Should().BeEmpty();
        }

        [Theory]
        // The recovery direction, and the only one that is safe to invert. The converter's input is
        // always a res:/ .gr2 because that is the only form CCP publish; its OUTPUT location is its
        // own choice and one CCP tree flattens a directory level, so rewriting anything but the
        // extension would ask for a file that was never written.
        [InlineData("res:/dx9/model/ship/gallente/frigate/soef1/soef1_t1.cmf",
                    "res:/dx9/model/ship/gallente/frigate/soef1/soef1_t1.gr2")]
        [InlineData("res:/graphics/generic/cylinder/cylinder_01a_ds.CMF",
                    "res:/graphics/generic/cylinder/cylinder_01a_ds.gr2")]
        // Already Granny: identity, so the ordinary unmapped case is unaffected by the recovery.
        [InlineData("res:/graphics/generic/unitcube/unitcube.gr2",
                    "res:/graphics/generic/unitcube/unitcube.gr2")]
        // Neither: left alone rather than guessed at. A .wbg or a path we have never seen is not
        // something to invent a source for.
        [InlineData("res:/dx9/model/ship/mystery.wbg", "res:/dx9/model/ship/mystery.wbg")]
        public void GrannySourceInvertsTheExtensionAndNothingElse(string path, string expected)
            => SkinrSidecarHost.GrannySourceOf(path).Should().Be(expected);
    }
}
