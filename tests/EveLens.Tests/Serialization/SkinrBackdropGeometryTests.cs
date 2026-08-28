// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Serialization
{
    /// <summary>
    /// The data contract that makes the SKINR viewport's backdrop reachable.
    /// </summary>
    /// <remarks>
    /// <para><b>The bug these exist for, and it is the third time the same one.</b> CCP's SKINR
    /// studio shows a ship against a grey gradient. Ours showed it against black, through five
    /// attempted fixes: a generated cubemap, three different CCP cubemaps swapped into the scene's
    /// background effect, host-side alpha compositing, host-side luma keying, and a raised clear
    /// colour. Every one of them was aimed at the wrong thing.</para>
    ///
    /// <para>The gradient is not a shader, a cubemap or a clear colour. It is a
    /// <c>cylinder_01a_ds</c> primitive named BackgroundGradient, authored at scale 3e7 inside
    /// <c>skinrenv_holographic_01a</c> — and, like everything else in CCP's art pipeline, it is
    /// Granny <c>.gr2</c> geometry that Trinity cannot read. The room loaded. It appended to the
    /// scene's render list. It registered as a secondary light source. It reported
    /// <c>attached=true</c> with eight child objects, and it drew nothing, because a mesh with no
    /// vertices is indistinguishable from a healthy one on every field except the one nobody was
    /// reading.</para>
    ///
    /// <para>Measured on the app's own configuration, empty viewport, before and after conversion:
    /// mean luma 1.08 with a top-to-bottom ramp of −0.15, versus 22.55 with a ramp of +17.52 —
    /// against CCP's own viewport at 22.02. A 0.05× match became a 1.02× match by converting four
    /// files.</para>
    ///
    /// <para><b>What is asserted.</b> The two structural facts that make the fix possible rather
    /// than the render itself, which needs a GPU and a Trinity build. First, that the room's
    /// unmapped-geometry list survives the wire, since it is the only place the host can learn the
    /// backdrop is broken and it cannot be known before the scene boots. Second, that the host asks
    /// for the room by default — because the generated dome that replaced it was a workaround for
    /// this bug, and leaving the workaround as the default would have hidden the fix.</para>
    /// </remarks>
    public sealed class SkinrBackdropGeometryTests
    {
        private static readonly JsonSerializerOptions s_json = new();

        /// <summary>
        /// A build response as the sidecar really emits it, including the fields the host does not
        /// model. Extra members must be skipped, not fatal — the sidecar reports more than the host
        /// consumes on purpose, so that a diagnostic can be added on one side alone.
        /// </summary>
        private const string BuildResponse = """
        {
          "ok": true,
          "radius": 34.5,
          "shipGeometry": {
            "repointed": 0, "native": 2, "meshes": 20, "loaded": 1,
            "unmapped": [
              "res:/dx9/model/ship/minmatar/frigate/mf1/mf1_t1_exhaust_geo_01a.gr2",
              "res:/graphics/generic/unitplane/unitplane_offset_plusy_01a.gr2"
            ]
          },
          "lightEnv": {
            "attached": true,
            "path": "res:/dx9/scene/skinr/skinrenv_holographic_01a.black",
            "list": "objects",
            "listGrewBy": 1,
            "shManager": true,
            "secondaryLightSource": true,
            "objects": 8,
            "geometry": {
              "repointed": 0, "native": 0, "meshes": 8, "loaded": 0,
              "reason": "no geometry map supplied",
              "unmapped": [
                "res:/graphics/generic/cylinder/cylinder_01a_ds.gr2",
                "res:/graphics/generic/unitcube/unitcube.gr2",
                "res:/graphics/generic/unitplane/unitplane_doublesided_01a.gr2",
                "res:/graphics/generic/unitsphere/unitsphere_4k_01a.gr2"
              ]
            }
          }
        }
        """;

        [Fact]
        public void BuildResponse_CarriesTheRoomsUnmappedGeometry()
        {
            SkinrSidecarResponse? response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(BuildResponse, s_json);

            response.Should().NotBeNull();
            response!.LightEnv.Should().NotBeNull();
            response.LightEnv!.Attached.Should().BeTrue();
            response.LightEnv.Objects.Should().Be(8);

            // The whole point of the type. `attached=true` with eight objects is what the old
            // report said while the viewport was black; this list is the fact that explains it.
            response.LightEnv.Geometry.Should().NotBeNull();
            response.LightEnv.Geometry!.Unmapped.Should().HaveCount(4);
            response.LightEnv.Geometry.Unmapped.Should().Contain(
                "res:/graphics/generic/cylinder/cylinder_01a_ds.gr2",
                because: "cylinder_01a_ds is CCP's BackgroundGradient — it IS the grey gradient, " +
                         "and an unmapped entry here is a black viewport rather than a lost detail");
        }

        [Fact]
        public void TheSecondPassConvertsTheRoomAndTheShipTogether()
        {
            SkinrSidecarResponse response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(BuildResponse, s_json)!;

            // What CompleteShipGeometryAsync builds its conversion list from. Both lists or
            // neither: converting only the ship leaves the backdrop black, and converting only the
            // room leaves eighteen meshes invisible. They are one defect at two scopes.
            List<string> ship = response.ShipGeometry!.Unmapped;
            List<string> room = response.LightEnv!.Geometry!.Unmapped;
            List<string> union = ship.Concat(room).Distinct().ToList();

            union.Should().HaveCount(6);
            union.Should().OnlyContain(p => p.EndsWith(".gr2"),
                because: "every entry is a Granny file, which is exactly why none of them draw");
        }

        [Fact]
        public void GeometryMapResponse_ReportsWhatTheRoomRepointedTo()
        {
            // The op re-repoints the LIVE room, and it has to, because the room attaches once at
            // boot and a rebuild never walks it. Without this the backdrop would only appear on
            // hulls that happened to need a rebuild for their own meshes — which the Astero, whose
            // unmapped list comes back empty, does not.
            const string json = """
            {
              "ok": true, "added": 6, "total": 6,
              "lightEnvGeometry": {
                "repointed": 8, "native": 0, "meshes": 8, "loaded": 8, "unmapped": []
              }
            }
            """;

            SkinrSidecarResponse response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(json, s_json)!;

            response.LightEnvGeometry.Should().NotBeNull();
            response.LightEnvGeometry!.Repointed.Should().Be(8);
            response.LightEnvGeometry.Loaded.Should().Be(8,
                because: "`repointed` only says a string was written to a property, and a string " +
                         "is not vertices; `loaded` is the count that proves geometry arrived");
            response.LightEnvGeometry.Unmapped.Should().BeEmpty();
        }

        [Fact]
        public void ARoomThatFailedToAttachIsNotAnEmptyRoom()
        {
            // Two opposite diagnoses that must never read the same. A null Geometry means we do not
            // know; an empty Unmapped means we do know, and everything draws.
            const string json = """
            {
              "ok": true,
              "lightEnv": { "attached": false, "reason": "load failed",
                            "path": "res:/dx9/scene/skinr/skinrenv_holographic_01a.black" }
            }
            """;

            SkinrSidecarResponse response =
                JsonSerializer.Deserialize<SkinrSidecarResponse>(json, s_json)!;

            response.LightEnv!.Attached.Should().BeFalse();
            response.LightEnv.Reason.Should().Be("load failed");
            response.LightEnv.Geometry.Should().BeNull();
        }

        [Fact]
        public void TheHostAsksForCcpsOwnRoomRatherThanOurGeneratedDome()
        {
            // The dome was a cubemap we generated because the room appeared not to work. It did
            // work. Keeping the dome as the default after fixing the room would mean shipping an
            // imitation of a backdrop we can draw for real — and would have quietly hidden that
            // this bug was ever fixed.
            var options = new SkinrSidecarOptions();

            options.Backdrop.Should().Be("room");
            options.BuildArguments().ToList().Should().ContainInOrder("--backdrop", "room");
        }
    }
}
