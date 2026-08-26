// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// The Paragon Hub discovery pane's grouping and search contracts: listings
    /// collapse into one entry per design, dead listings don't count as buyable,
    /// and unknown state strings fail open rather than hiding the marketplace.
    /// </summary>
    public sealed class SkinrMarketViewModelTests
    {
        private static EsiSkinrListing Listing(string skinrId, string state, long plex) =>
            new()
            {
                Id = skinrId + "-" + state + "-" + plex,
                SkinrId = skinrId,
                State = state,
                Price = new EsiSkinrPrice { Plex = plex }
            };

        [Fact]
        public void GroupListings_CollapsesPerDesign_CountingOnlyBuyable()
        {
            var listings = new List<EsiSkinrListing>
            {
                Listing("design-a", "active", 500),
                Listing("design-a", "active", 350),
                Listing("design-a", "sold", 100),      // history, not stock
                Listing("design-a", "expired", 90),
                Listing("design-b", "cancelled", 40),
                Listing("design-b", "active", 800)
            };

            List<SkinrMarketEntry> entries = SkinrMarketViewModel.GroupListings(listings);

            entries.Should().HaveCount(2);
            entries[0].SkinrId.Should().Be("design-a");   // feed order preserved
            entries[0].ActiveListings.Should().Be(2);
            entries[0].MinPlex.Should().Be(350);          // sold 100 must NOT win
            entries[1].ActiveListings.Should().Be(1);
            entries[1].MinPlex.Should().Be(800);
        }

        [Fact]
        public void GroupListings_UnknownState_FailsOpenAsBuyable()
        {
            // CCP's state vocabulary is not documented; a new value must not make the
            // whole marketplace look sold out.
            var listings = new List<EsiSkinrListing>
            {
                Listing("design-x", "some_future_state", 220)
            };

            List<SkinrMarketEntry> entries = SkinrMarketViewModel.GroupListings(listings);
            entries.Should().ContainSingle().Which.ActiveListings.Should().Be(1);
            entries[0].MinPlex.Should().Be(220);
        }

        [Fact]
        public void GroupListings_TolerantOfJunk()
        {
            var entries = SkinrMarketViewModel.GroupListings(new List<EsiSkinrListing>
            {
                new() { Id = "no-skinr-id", State = "active" },
                new() { SkinrId = "priceless", State = "active", Price = null },
                null!
            });

            entries.Should().ContainSingle();
            entries[0].SkinrId.Should().Be("priceless");
            entries[0].ActiveListings.Should().Be(1);
            entries[0].MinPlex.Should().Be(0);   // unknown ask, not a zero-PLEX bargain

            SkinrMarketViewModel.GroupListings(null!).Should().BeEmpty();
        }

        [Fact]
        public void BuildSections_OrdersClassThenFactionThenShipThenName_PendingLast()
        {
            SkinrMarketEntry Resolved(string id, string name) =>
                new(id) { Recipe = new EsiSkinrRecipe { Id = id, Name = name } };

            var cruiserVexorB = Resolved("1", "Bravo");
            var cruiserVexorA = Resolved("2", "Alpha");
            var cruiserThorax = Resolved("3", "Zulu");
            var shuttleAmarr = Resolved("4", "Omega");
            var classless = Resolved("5", "Quiet");
            var pending = new SkinrMarketEntry("6");

            var classes = new Dictionary<string, string>
            {
                ["1"] = "Cruiser", ["2"] = "Cruiser", ["3"] = "Cruiser",
                ["4"] = "Shuttle", ["5"] = ""
            };
            var factions = new Dictionary<string, string>
            {
                ["1"] = "GALLENTE", ["2"] = "GALLENTE", ["3"] = "GALLENTE",
                ["4"] = "AMARR", ["5"] = ""
            };
            var hulls = new Dictionary<string, string>
            {
                ["1"] = "Vexor", ["2"] = "Vexor", ["3"] = "Thorax",
                ["4"] = "Amarr Shuttle", ["5"] = "Mystery"
            };

            var sections = SkinrMarketViewModel.BuildSections(
                new[] { cruiserVexorB, cruiserVexorA, cruiserThorax,
                        shuttleAmarr, classless, pending },
                e => classes.GetValueOrDefault(e.SkinrId, string.Empty),
                e => factions.GetValueOrDefault(e.SkinrId, string.Empty),
                e => hulls.GetValueOrDefault(e.SkinrId, string.Empty));

            sections.Should().HaveCount(4);
            // Class alphabetical: Cruiser before Shuttle; classless after; "…" last.
            (sections[0].Group, sections[0].Faction).Should().Be(("Cruiser", "GALLENTE"));
            // Ship first (Thorax before Vexor), then design name (Alpha before Bravo).
            sections[0].Designs.Select(d => d.SkinrId).Should().Equal("3", "2", "1");
            (sections[1].Group, sections[1].Faction).Should().Be(("Shuttle", "AMARR"));
            sections[2].Group.Should().Be("");
            sections[3].Group.Should().Be("…");
            sections[3].Designs.Should().ContainSingle()
                .Which.SkinrId.Should().Be("6");
        }

        [Fact]
        public void ApplyRecipeCache_FillsOnlyUnresolvedEntries()
        {
            var cached = new EsiSkinrRecipe { Id = "a", Name = "Cached Gold", ShipTypeId = 587 };
            var fresh = new EsiSkinrRecipe { Id = "b", Name = "Fresh Silver" };
            var entryA = new SkinrMarketEntry("a");
            var entryB = new SkinrMarketEntry("b") { Recipe = fresh };
            var cache = new Dictionary<string, EsiSkinrRecipe>
            {
                ["a"] = cached,
                ["b"] = new EsiSkinrRecipe { Id = "b", Name = "Stale" }
            };

            SkinrMarketViewModel.ApplyRecipeCache(new[] { entryA, entryB }, cache);

            entryA.Recipe.Should().BeSameAs(cached);
            entryA.ShipTypeId.Should().Be(587);   // hull filter works straight off disk
            entryB.Recipe.Should().BeSameAs(fresh);   // live data never downgraded
        }

        [Fact]
        public void HubPreferences_RoundTrip_AndCorruptFileReAsks()
        {
            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "evelens-test-" + System.Guid.NewGuid().ToString("N") + ".json");
            try
            {
                // Never asked: null — this is what makes the consent banner appear.
                SkinrHubPreferences.Load(path).CommunityPreviews.Should().BeNull();

                new SkinrHubPreferences { CommunityPreviews = true }.Save(path);
                SkinrHubPreferences.Load(path).CommunityPreviews.Should().BeTrue();

                new SkinrHubPreferences { CommunityPreviews = false }.Save(path);
                SkinrHubPreferences.Load(path).CommunityPreviews.Should().BeFalse();

                // A mangled file re-asks the question instead of failing the pane.
                System.IO.File.WriteAllText(path, "{not json");
                SkinrHubPreferences.Load(path).CommunityPreviews.Should().BeNull();
            }
            finally
            {
                System.IO.File.Delete(path);
            }
        }

        [Fact]
        public void Matches_SearchesNameAndFallsBackToShortId()
        {
            var entry = new SkinrMarketEntry("abcdef0123456789-opaque");
            SkinrMarketViewModel.Matches(entry, "abcdef").Should().BeTrue();

            entry.Recipe = new EsiSkinrRecipe { Name = "Crimson Edge", ShipTypeId = 0 };
            SkinrMarketViewModel.Matches(entry, "crimson").Should().BeTrue();
            SkinrMarketViewModel.Matches(entry, "CRIMSON").Should().BeTrue();
            SkinrMarketViewModel.Matches(entry, "azure").Should().BeFalse();
        }
    }
}
