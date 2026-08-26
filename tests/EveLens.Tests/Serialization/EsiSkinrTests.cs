// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using EveLens.Common;
using EveLens.Common.Serialization.Esi;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Serialization
{
    /// <summary>
    /// SKINR DTOs against payloads captured live from ESI on 2026-08-19
    /// (X-Compatibility-Date: 2026-08-18). If CCP changes the shapes under a new
    /// compatibility date, these tests document what we were built against.
    /// </summary>
    public class EsiSkinrTests
    {
        // Trimmed but structurally faithful capture of GET /cosmetics/skinr/{id}
        private const string RecipeJson = """
        {
            "id": "e6e891e65851f6f048ac34f9dc0183fc4b10db3bc7e44bb03ae60c7182552699",
            "name": "Test Design",
            "line": "Ontologic Fiction",
            "creator_id": 2029528905,
            "ship_type_id": 28710,
            "tier": { "level": 12 },
            "layout": {
                "slots": [
                    { "id": 1, "configuration": { "nanocoating": { "id": 1591 } } },
                    { "id": 5, "configuration": { "pattern": { "id": 1477, "configuration": {
                        "projection": { "slot1": true, "slot2": true, "slot3": true, "slot4": false },
                        "transform": {
                            "position": { "x": 14.664155960083008, "y": 496.7437744140625, "z": 0.9019012451171875 },
                            "rotation": { "x": 0.2706418037414551, "y": -0.27262234687805176, "z": 0.6524357199668884, "w": -0.653252363204956 },
                            "scaling": { "x": 290.9547598266603, "y": 290.9547598266603, "z": 290.9547598266603 }
                        },
                        "mirrored": false
                    } } } }
                ]
            }
        }
        """;

        // Trimmed capture of GET /paragon-hub/skinr
        private const string ListingsJson = """
        {
            "cursor": { "before": "1.MTc4NzEyODM4NjgxNC0w", "after": "1.MTc4NzEzMTY4ODA4MS0w" },
            "listings": [
                { "id": "e7d29b77-4c45-4c35-8578-f9e7817f92de", "state": "removed",
                  "last_modified": "2026-08-19T08:33:06Z", "seller_id": 2029528905,
                  "skinr_id": "08267c2019331ec3695d0f694e60e3ab5bbf9f6f3b9fc00c28263e63a05c1d45",
                  "created": "2026-08-18T10:51:22Z", "expires": "2026-11-16T10:51:22Z",
                  "quantity": 1, "price": { "plex": 200 } }
            ]
        }
        """;

        [Fact]
        public void Recipe_Deserializes_WithNanocoatingAndPattern()
        {
            var recipe = Util.DeserializeJson<EsiSkinrRecipe>(RecipeJson);

            recipe.Should().NotBeNull();
            recipe!.ShipTypeId.Should().Be(28710);
            recipe.Line.Should().Be("Ontologic Fiction");
            recipe.Tier.Level.Should().Be(12);
            recipe.Layout.Slots.Should().HaveCount(2);

            var coating = recipe.Layout.Slots[0].Configuration.Nanocoating;
            coating.Should().NotBeNull();
            coating!.Id.Should().Be(1591);
            recipe.Layout.Slots[0].Configuration.Pattern.Should().BeNull();

            var pattern = recipe.Layout.Slots[1].Configuration.Pattern;
            pattern.Should().NotBeNull();
            pattern!.Id.Should().Be(1477);
            pattern.Configuration.Projection.Slot4.Should().BeFalse();
            pattern.Configuration.Transform.Position.Y.Should().BeApproximately(496.74377, 0.001);
            pattern.Configuration.Transform.Rotation.W.Should().BeApproximately(-0.65325, 0.001);
            pattern.Configuration.Transform.Scaling.X.Should().BeApproximately(290.95476, 0.001);
            pattern.Configuration.Mirrored.Should().BeFalse();
        }

        [Fact]
        public void ListingsPage_Deserializes_WithCursorAndFinalState()
        {
            var page = Util.DeserializeJson<EsiSkinrListingsPage>(ListingsJson);

            page.Should().NotBeNull();
            page!.Cursor.After.Should().Be("1.MTc4NzEzMTY4ODA4MS0w");
            var listing = page.Listings.Single();
            listing.State.Should().Be("removed", "final states linger — that is the price-history contract");
            listing.Price.Plex.Should().Be(200);
            listing.SellerId.Should().Be(2029528905);
        }

        /// <summary>
        /// Round-trips the recipe through the same serializer that reads it off the wire.
        /// </summary>
        /// <remarks>
        /// Law 13: the DTO is the data contract. What this actually protects is the
        /// <c>[DataMember(Name = ...)]</c> spelling of every field — a rename to
        /// <c>PatternBlendMode</c> without its attribute would still compile, still
        /// deserialize, and silently produce <c>null</c>, which the resolver would then
        /// dutifully default to <c>"normal"</c>. Two designs that differ only in blend mode do
        /// not look alike, so that failure surfaces as "the render is wrong" months later
        /// rather than as a broken build now.
        /// </remarks>
        [Theory]
        [InlineData("normal")]
        [InlineData("subtract")]
        [InlineData("exclusion")]
        [InlineData("nested")]
        [InlineData("nested_inverted")]
        public void Recipe_RoundTrips_PreservingBlendModeAndProjection(string blendMode)
        {
            EsiSkinrRecipe original = Util.DeserializeJson<EsiSkinrRecipe>(RecipeJson)!;
            original.Layout.PatternBlendMode = blendMode;

            EsiSkinrRecipe restored = RoundTrip(original);

            restored.Layout.PatternBlendMode.Should().Be(blendMode);
            restored.Id.Should().Be(original.Id);
            restored.Name.Should().Be(original.Name);
            restored.Line.Should().Be(original.Line);
            restored.CreatorId.Should().Be(original.CreatorId);
            restored.ShipTypeId.Should().Be(original.ShipTypeId);
            restored.Tier.Level.Should().Be(original.Tier.Level);

            // Every slot, in order, with its discriminated configuration intact.
            restored.Layout.Slots.Select(s => s.Id)
                .Should().Equal(original.Layout.Slots.Select(s => s.Id));
            restored.Layout.Slots[0].Configuration.Nanocoating!.Id.Should().Be(1591);
            restored.Layout.Slots[0].Configuration.Pattern.Should().BeNull();

            EsiSkinrPatternConfiguration config = restored.Layout.Slots[1].Configuration.Pattern!.Configuration;
            config.Projection.Slot1.Should().BeTrue();
            config.Projection.Slot4.Should().BeFalse();
            config.Mirrored.Should().BeFalse();

            // The transform is what places the pattern on the hull; doubles must survive
            // exactly, not approximately, or a design drifts every time we persist it.
            config.Transform.Position.Y.Should().Be(
                original.Layout.Slots[1].Configuration.Pattern!.Configuration.Transform.Position.Y);
            config.Transform.Rotation.W.Should().Be(
                original.Layout.Slots[1].Configuration.Pattern!.Configuration.Transform.Rotation.W);
            config.Transform.Scaling.X.Should().Be(
                original.Layout.Slots[1].Configuration.Pattern!.Configuration.Transform.Scaling.X);
        }

        [Fact]
        public void Recipe_MissingBlendModeIsNullSoTheResolverCanDefaultIt()
        {
            // The route marks it required, but the DTO must not invent a value: the resolver
            // owns the "normal" default and warns nowhere else can.
            EsiSkinrRecipe recipe = Util.DeserializeJson<EsiSkinrRecipe>(RecipeJson)!;

            recipe.Layout.PatternBlendMode.Should().BeNull();
        }

        private static EsiSkinrRecipe RoundTrip(EsiSkinrRecipe recipe)
        {
            var serializer = new DataContractJsonSerializer(typeof(EsiSkinrRecipe),
                new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });

            using var stream = new MemoryStream();
            serializer.WriteObject(stream, recipe);
            stream.Position = 0;

            return (EsiSkinrRecipe)serializer.ReadObject(stream)!;
        }

        [Fact]
        public void ComponentRuns_HandlesBothVariants()
        {
            var limited = Util.DeserializeJson<EsiSkinrComponentInventory>(
                """{ "licenses": [ { "component_id": 53, "type": "nanocoating", "runs": { "remaining": 4 } } ] }""");
            limited!.Licenses.Single().Runs.Remaining.Should().Be(4);
            limited.Licenses.Single().Runs.Unlimited.Should().BeNull();

            var unlimited = Util.DeserializeJson<EsiSkinrComponentInventory>(
                """{ "licenses": [ { "component_id": 1477, "type": "pattern", "runs": { "unlimited": true } } ] }""");
            unlimited!.Licenses.Single().Runs.Unlimited.Should().BeTrue();
            unlimited.Licenses.Single().Runs.Remaining.Should().BeNull();
        }
    }
}
