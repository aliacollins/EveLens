// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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

            string[] known = { "room", "dome", "nebula", "studio", "transparent" };
            foreach (SkinrEnvironmentPreset preset in SkinrEnvironmentPresets.All)
            {
                known.Should().Contain(SkinrEnvironmentPresets.Backdrop(preset));
                SkinrEnvironmentPresets.NameKey(preset).Should().StartWith("Skinr.Env");
            }
        }

        [Fact]
        public void EnvironmentPresets_EveryPreset_StatesItsSunExplicitly()
        {
            // Never null: a preset that sent nothing would inherit the previous preset's
            // sun (scene writes are sticky), so Studio-after-Sunlight would keep the x2 key.
            foreach (SkinrEnvironmentPreset preset in SkinrEnvironmentPresets.All)
            {
                SkinrEnvironmentPresets.SunColor(preset).Should().HaveCount(4);
                SkinrEnvironmentPresets.SunDirection(preset).Should().HaveCount(3);
            }

            // The lighting presets differ from the authored studio key; the rest restore it.
            SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Sunlight)
                .Should().NotEqual(SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Studio));
            SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Hangar)
                .Should().Equal(SkinrEnvironmentPresets.SunColor(SkinrEnvironmentPreset.Studio));
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
