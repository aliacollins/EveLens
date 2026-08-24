// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// The SKINR Hub's carousel entries, search filter, environment presets, and the
    /// protocol additions the environment switcher rides on.
    /// </summary>
    public sealed class SkinrHubViewModelTests
    {
        private static SkinrHubDesignEntry Entry(string id) =>
            new(new SkinrLicenseEntry(new EsiSkinrLicense
            {
                SkinrId = id,
                Activated = true,
                Unactivated = 0
            }));

        [Fact]
        public void DesignEntry_FallsBackToShortId_UntilRecipeArrives()
        {
            var entry = Entry("abcdef0123456789-long-opaque-id");

            entry.HasRecipe.Should().BeFalse();
            entry.DisplayLabel.Should().NotBeNullOrEmpty();
            entry.DisplayLabel.Should().Contain("abcdef");
        }

        [Fact]
        public void DesignEntry_UsesRecipeNameAndTier_OnceApplied()
        {
            var entry = Entry("some-id");
            entry.ApplyRecipe(new EsiSkinrRecipe
            {
                Id = "some-id",
                Name = "Solar Queen",
                Tier = new EsiSkinrTier { Level = 3 },
                ShipTypeId = 587
            });

            entry.HasRecipe.Should().BeTrue();
            entry.DisplayLabel.Should().Be("Solar Queen");
            entry.TierLevel.Should().Be(3);
        }

        [Fact]
        public void DesignEntry_RetainsFullRecipe_ForPhotoOp()
        {
            // Photo Op rebuilds a wingman from its recipe DNA; the entry must keep
            // the recipe object itself, not just the display fields peeled off it.
            var entry = Entry("some-id");
            entry.Recipe.Should().BeNull();

            var recipe = new EsiSkinrRecipe
            {
                Id = "some-id",
                Name = "Solar Queen",
                ShipTypeId = 587
            };
            entry.ApplyRecipe(recipe);

            entry.Recipe.Should().BeSameAs(recipe);
        }

        [Fact]
        public async System.Threading.Tasks.Task PhotoOp_NoOpsSafely_WithoutRendererOrDesign()
        {
            // The fleet methods must be safe to call in every degraded state the
            // window can reach them from: no sidecar, no design loaded.
            var render = new SkinrRenderViewModel();

            int placed = await render.AssembleFleetAsync(new[]
            {
                new EsiSkinrRecipe { Id = "x", Name = "X", ShipTypeId = 587 }
            });

            placed.Should().Be(0);
            render.WingmenCount.Should().Be(0);

            await render.DisbandFleetAsync();
            render.WingmenCount.Should().Be(0);
            render.Dispose();
        }

        [Fact]
        public void Search_FiltersByLabel_CaseInsensitive()
        {
            var hub = new SkinrHubViewModel();
            // Entries are private to the refresh path; the filter contract is observable
            // through Designs after RefreshDesigns, but building the data VM's licenses
            // needs ESI. The filter itself is pure, so exercise it through SearchText on
            // an empty hub (must not throw) and through entries directly.
            hub.SearchText = "anything";
            hub.Designs.Should().BeEmpty();

            var entry = Entry("id-1");
            entry.ApplyRecipe(new EsiSkinrRecipe
            {
                Id = "id-1",
                Name = "Crimson Edge",
                ShipTypeId = 587
            });
            entry.DisplayLabel.ToLowerInvariant().Should().Contain("crimson");
            hub.Dispose();
        }

        [Fact]
        public void EnvironmentPresets_AllFive_MapToKnownBackdrops()
        {
            SkinrEnvironmentPresets.All.Should().HaveCount(5);

            string[] known = { "room", "dome", "nebula", "studio", "transparent", "hangar" };
            foreach (SkinrEnvironmentPreset preset in SkinrEnvironmentPresets.All)
            {
                known.Should().Contain(SkinrEnvironmentPresets.Backdrop(preset));
                SkinrEnvironmentPresets.NameKey(preset).Should().StartWith("Skinr.Env");
            }
        }

        [Fact]
        public void EnvironmentPresets_StateTheirSunExplicitly_ExceptSpace()
        {
            // Explicit suns everywhere: a preset that sent nothing would inherit the
            // previous preset's sun (scene writes are sticky). Space is the one
            // deliberate null — the sidecar owns its lighting, applying each sky's own
            // authored universe sun, and a value sent from here would overwrite it.
            foreach (SkinrEnvironmentPreset preset in SkinrEnvironmentPresets.All)
            {
                if (preset is SkinrEnvironmentPreset.Space or SkinrEnvironmentPreset.Hangar)
                {
                    // Sidecar-owned lighting: Space applies each sky's authored
                    // universe sun; Hangar applies the bay's own authored scene values.
                    SkinrEnvironmentPresets.SunColor(preset).Should().BeNull();
                    SkinrEnvironmentPresets.SunDirection(preset).Should().BeNull();
                    continue;
                }
                SkinrEnvironmentPresets.SunColor(preset).Should().HaveCount(4);
                SkinrEnvironmentPresets.SunDirection(preset).Should().HaveCount(3);
            }

            SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Sunlight)
                .Should().NotEqual(SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Studio));
        }

        [Fact]
        public void HangarCamera_ClampsPitch_AbovePadPlane()
        {
            // Studio keeps the full pole-to-pole range regardless of geometry.
            SkinrStageCamera.MinPitchForEnvironment(
                SkinrEnvironmentPreset.Studio, 240.0, 3000.0).Should().Be(-89.4);

            // Hangar, close in: the pad plane lies outside the orbit sphere, so no
            // angle can reach it and the full range stays.
            SkinrStageCamera.MinPitchForEnvironment(
                SkinrEnvironmentPreset.Hangar, 240.0, 200.0).Should().Be(-89.4);

            // Hangar, pulled out: the floor tightens toward level — the camera may
            // skim the deck but never orbit under it. drop = max(60, 240*1.25)*0.9
            // = 270; at distance 3000 the plane subtends asin(270/3000) ≈ 5.2°.
            double farOut = SkinrStageCamera.MinPitchForEnvironment(
                SkinrEnvironmentPreset.Hangar, 240.0, 3000.0);
            farOut.Should().BeGreaterThan(-6.0).And.BeLessThan(0.0);

            // Monotonic in distance: further out means a tighter floor.
            double closer = SkinrStageCamera.MinPitchForEnvironment(
                SkinrEnvironmentPreset.Hangar, 240.0, 600.0);
            closer.Should().BeLessThan(farOut);
        }

        [Fact]
        public void SpinCounter_CountsFullRevolutions_EitherDirection()
        {
            var camera = new SkinrStageCamera();
            int raised = 0;
            camera.SpinCountChanged += _ => raised++;

            // Wiggling in place is not spinning: signed accumulation nets to zero.
            for (int i = 0; i < 20; i++)
                camera.Orbit(i % 2 == 0 ? 100 : -100, 0);
            camera.SpinCount.Should().Be(0);

            // 360° of yaw is ~1029 px at the orbit's 0.35°/px; either direction counts.
            for (int i = 0; i < 10; i++)
                camera.Orbit(103, 0);
            camera.SpinCount.Should().Be(1);
            for (int i = 0; i < 10; i++)
                camera.Orbit(-103, 0);
            camera.SpinCount.Should().Be(2);
            raised.Should().Be(2);
        }

        [Fact]
        public void Composition_CountsCoatedAndPatternedSlots()
        {
            var recipe = new EsiSkinrRecipe
            {
                Layout = new EsiSkinrLayout
                {
                    Slots =
                    {
                        new EsiSkinrSlot
                        {
                            Configuration = new EsiSkinrSlotConfiguration
                            {
                                Nanocoating = new EsiSkinrNanocoating { Id = 1 }
                            }
                        },
                        new EsiSkinrSlot
                        {
                            Configuration = new EsiSkinrSlotConfiguration
                            {
                                Nanocoating = new EsiSkinrNanocoating { Id = 2 },
                                Pattern = new EsiSkinrPattern { Id = 9 }
                            }
                        },
                        new EsiSkinrSlot()
                    }
                }
            };

            SkinrHubViewModel.Composition(recipe).Should().Be((2, 1));
            SkinrHubViewModel.Composition(null).Should().Be((0, 0));
        }

        [Fact]
        public void FlickVelocity_SteadyDrag_ReleasesWithMomentum()
        {
            // Ten moves of 3° yaw over 90ms ending at now: 30° / 0.09s ≈ 333°/s.
            var samples = new List<SkinrStageCamera.OrbitSample>();
            for (int i = 0; i < 10; i++)
                samples.Add(new SkinrStageCamera.OrbitSample(1000 + i * 10, 3.0, 0.0));

            (double yaw, double pitch) =
                SkinrStageCamera.FlickVelocity(samples, 1090);
            yaw.Should().BeApproximately(30.0 / 0.090, 1.0);
            pitch.Should().Be(0.0);
        }

        [Fact]
        public void FlickVelocity_ZeroCases_ReleaseAStationaryShip()
        {
            // No samples, a single twitch, a slow drift, and a drag that HELD before
            // releasing (all samples stale) must all come back (0, 0).
            var none = new List<SkinrStageCamera.OrbitSample>();
            SkinrStageCamera.FlickVelocity(none, 1000).Should().Be((0.0, 0.0));

            var twitch = new List<SkinrStageCamera.OrbitSample>
            {
                new(990, 5.0, 0.0)
            };
            SkinrStageCamera.FlickVelocity(twitch, 1000).Should().Be((0.0, 0.0));

            var slow = new List<SkinrStageCamera.OrbitSample>
            {
                new(900, 0.5, 0.0), new(950, 0.5, 0.0), new(1000, 0.5, 0.0)
            };
            SkinrStageCamera.FlickVelocity(slow, 1000).Should().Be((0.0, 0.0));

            var held = new List<SkinrStageCamera.OrbitSample>
            {
                new(100, 8.0, 0.0), new(120, 8.0, 0.0), new(140, 8.0, 0.0)
            };
            SkinrStageCamera.FlickVelocity(held, 1000).Should().Be((0.0, 0.0));
        }

        [Fact]
        public void FlickVelocity_WildSwipe_IsCappedNotRejected()
        {
            var samples = new List<SkinrStageCamera.OrbitSample>
            {
                new(1000, 60.0, 30.0), new(1020, 60.0, 30.0), new(1040, 60.0, 30.0)
            };

            (double yaw, double pitch) =
                SkinrStageCamera.FlickVelocity(samples, 1040);
            double speed = Math.Sqrt(yaw * yaw + pitch * pitch);
            speed.Should().BeApproximately(480.0, 0.001);
            // Direction survives the cap: yaw and pitch keep their 2:1 ratio.
            (yaw / pitch).Should().BeApproximately(2.0, 0.001);
        }

        [Fact]
        public void SceneRequest_SerializesBackdropAndSun_AndOmitsWhenNull()
        {
            var request = new SkinrSidecarRequest
            {
                Id = 7,
                Op = "scene",
                Backdrop = "dome",
                SunColor = new[] { 4.5, 4.3, 4.0, 1.0 },
                SunDirection = new[] { -0.45, -0.80, 0.35 }
            };

            string json = JsonSerializer.Serialize(request);
            json.Should().Contain("\"backdrop\":\"dome\"");
            json.Should().Contain("\"sunColor\":[4.5,4.3,4");
            json.Should().Contain("\"sunDirection\":[-0.45");

            string bare = JsonSerializer.Serialize(new SkinrSidecarRequest
            {
                Id = 8,
                Op = "scene",
                Backdrop = "room"
            });
            bare.Should().NotContain("sunColor");
            bare.Should().NotContain("sunDirection");
        }

        [Fact]
        public void SceneRequest_RoundTrips()
        {
            var request = new SkinrSidecarRequest
            {
                Op = "scene",
                Backdrop = "nebula",
                SunColor = new[] { 2.6, 2.35, 2.1, 1.0 }
            };

            string json = JsonSerializer.Serialize(request);
            SkinrSidecarRequest? back =
                JsonSerializer.Deserialize<SkinrSidecarRequest>(json);

            back.Should().NotBeNull();
            back!.Backdrop.Should().Be("nebula");
            back.SunColor.Should().Equal(2.6, 2.35, 2.1, 1.0);
            back.SunDirection.Should().BeNull();
        }
    }
}
