// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Esi;
using EveLens.Common.Serialization.Skinr;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Data
{
    /// <summary>
    /// Covers the SKINR recipe → renderable design join.
    /// </summary>
    /// <remarks>
    /// The assertions worth reading are the remap ones. Every value in the render contract
    /// below was established from CCP's engine source, and every one of them fails silently
    /// when wrong — a design renders the right colours in the wrong places, or a pattern paints
    /// over the wrong panels, and it looks like a design you don't happen to like rather than a
    /// bug. So these tests pin exact positions and exact indices, not just "something came out".
    ///
    /// The fixture faction (500002, Minmatar Republic) has the real remap CCP ships:
    /// slot 1 → material 4, 2 → 3, 3 → 2, 4 → 1. A full reversal, chosen deliberately: an
    /// identity-mapped faction would let a missing remap pass every test.
    /// </remarks>
    public class SkinrRecipeResolverTests
    {
        private const int RifterTypeId = 587;
        private const int MinmatarFactionId = 500002;

        // Component IDs are arbitrary but distinct, so a swapped pair is visible.
        private const int MatPrimary = 101;
        private const int MatSecondary = 102;
        private const int MatTertiary = 103;
        private const int MatTech = 104;
        private const int PatternPrimary = 201;
        private const int PatternMatPrimary = 301;
        private const int PatternSecondary = 202;
        private const int PatternMatSecondary = 302;

        private static SkinrCatalog BuildCatalog() => SkinrCatalog.FromJson("""
        {
          "schemaVersion": 1,
          "sdeBuild": "3470007",
          "generatedUtc": "2026-08-19T00:00:00Z",
          "slots": {
            "1": { "name": "primary_nanocoating",        "category": 1, "allowedComponents": [1,3], "displayName": { "en": "Primary Slot" } },
            "2": { "name": "secondary_nanocoating",      "category": 1, "allowedComponents": [1,3], "displayName": { "en": "Secondary Slot" } },
            "3": { "name": "tertiary_nanocoating",       "category": 1, "allowedComponents": [1,3], "displayName": { "en": "Detailing Slot" } },
            "4": { "name": "tech_area",                  "category": 1, "allowedComponents": [1,3], "displayName": { "en": "Tech Slot" } },
            "5": { "name": "pattern",                    "category": 2, "allowedComponents": [2],   "displayName": { "en": "Pattern Slot" } },
            "6": { "name": "pattern_material",           "category": 3, "allowedComponents": [1,3], "displayName": { "en": "Pattern Material Slot" } },
            "7": { "name": "secondary_pattern",          "category": 2, "allowedComponents": [2],   "displayName": { "en": "Secondary Pattern Slot" } },
            "8": { "name": "secondary_pattern_material", "category": 3, "allowedComponents": [1,3], "displayName": { "en": "Secondary Pattern Material Slot" } }
          },
          "slotConfigurations": {
            "5": { "name": "Default configuration",   "priority": 3, "allowAllShips": true,  "slots": [1,2,3,4,5,6,7,8], "ships": [] },
            "6": { "name": "No custom skins allowed", "priority": 0, "allowAllShips": false, "slots": [],                "ships": [670] },
            "7": { "name": "NoSecondarySlot",         "priority": 1, "allowAllShips": false, "slots": [1,3,4,5,6,7,8],   "ships": [588] }
          },
          "slotsToMaterials": {
            "500002": { "1": 4, "2": 3, "3": 2, "4": 1 }
          },
          "componentCategories": { "1": "Material", "2": "Pattern", "3": "Metallic" },
          "componentRarities": {
            "1": { "rank": 1, "name": { "en": "Standard" } },
            "3": { "rank": 3, "name": { "en": "Elite" } }
          },
          "componentPointValues": {
            "1": { "1": 25,  "3": 100 },
            "2": { "1": 75,  "3": 250 },
            "3": { "1": 100, "3": 300 }
          },
          "tierThresholds": {
            "4": { "1": 125, "2": 175, "3": 250, "4": 375 }
          },
          "components": {
            "101": { "name": { "en": "Azure Matte" },   "category": 1, "rarity": 1, "dnaToken": "cosm_azure",  "resourceFile": "res:/mat/azure.red",  "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true },
            "102": { "name": { "en": "Blue Metallic" }, "category": 3, "rarity": 1, "dnaToken": "cosm_blue",   "resourceFile": "res:/mat/blue.red",   "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true },
            "103": { "name": { "en": "Brass Rough" },   "category": 1, "rarity": 3, "dnaToken": "cosm_brass",  "resourceFile": "res:/mat/brass.red",  "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true },
            "104": { "name": { "en": "Copper" },        "category": 1, "rarity": 1, "dnaToken": "cosm_copper", "resourceFile": "res:/mat/copper.red", "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true },
            "201": { "name": { "en": "Division" },      "category": 2, "rarity": 1, "dnaToken": "cosm_stripe_2k.dds", "resourceFile": "res:/texture/projection/cosm_stripe_2k.dds", "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-edge",   "published": true },
            "202": { "name": { "en": "Chevron" },       "category": 2, "rarity": 3, "dnaToken": "cosm_chev_2k.dds",   "resourceFile": "res:/texture/projection/cosm_chev_2k.dds",   "projectionTypeU": "clamp-to-edge",   "projectionTypeV": "repeat",         "published": true },
            "301": { "name": { "en": "Gold Leaf" },     "category": 3, "rarity": 3, "dnaToken": "cosm_gold",   "resourceFile": "res:/mat/gold.red",   "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true },
            "302": { "name": { "en": "Onyx" },          "category": 1, "rarity": 1, "dnaToken": "cosm_onyx",   "resourceFile": "res:/mat/onyx.red",   "projectionTypeU": "clamp-to-border", "projectionTypeV": "clamp-to-border", "published": true }
          },
          "hulls": {
            "587": { "name": { "en": "Rifter" }, "groupID": 25, "groupName": { "en": "Frigate" }, "graphicID": 20038,
                     "factionID": 500002, "shipTreeGroupID": 4, "raceID": 2, "radius": 34.0, "published": true,
                     "sofHullName": "mf4_t1", "sofFactionName": "minmatarbase", "sofRaceName": "minmatar" },
            "588": { "name": { "en": "Reaper" }, "groupID": 25, "groupName": { "en": "Frigate" }, "graphicID": 20039,
                     "factionID": 500002, "shipTreeGroupID": 4, "raceID": 2, "radius": 30.0, "published": true,
                     "sofHullName": "mf1_t1", "sofFactionName": "minmatarbase", "sofRaceName": "minmatar" },
            "670": { "name": { "en": "Capsule" }, "groupID": 29, "groupName": { "en": "Capsule" }, "graphicID": 0,
                     "factionID": null, "shipTreeGroupID": null, "raceID": 2, "radius": 5.0, "published": true,
                     "sofHullName": "", "sofFactionName": "", "sofRaceName": "" }
          }
        }
        """);

        private static SkinrRecipeResolver BuildResolver() => new(BuildCatalog());

        private static EsiSkinrSlot Nanocoating(int slotId, int componentId) =>
            new()
            {
                Id = slotId,
                Configuration = new EsiSkinrSlotConfiguration
                {
                    Nanocoating = new EsiSkinrNanocoating { Id = componentId }
                }
            };

        private static EsiSkinrSlot Pattern(int slotId, int componentId,
            bool slot1 = false, bool slot2 = false, bool slot3 = false, bool slot4 = false,
            bool mirrored = false) =>
            new()
            {
                Id = slotId,
                Configuration = new EsiSkinrSlotConfiguration
                {
                    Pattern = new EsiSkinrPattern
                    {
                        Id = componentId,
                        Configuration = new EsiSkinrPatternConfiguration
                        {
                            Mirrored = mirrored,
                            Projection = new EsiSkinrProjection
                            {
                                Slot1 = slot1, Slot2 = slot2, Slot3 = slot3, Slot4 = slot4
                            },
                            Transform = new EsiSkinrTransform
                            {
                                Position = new EsiSkinrVector { X = 1.5, Y = -2.5, Z = 3.5 },
                                Rotation = new EsiSkinrQuaternion { X = 0, Y = 0, Z = 0.7071068, W = 0.7071067 },
                                Scaling = new EsiSkinrVector { X = 17.5, Y = 17.5, Z = 17.5 }
                            }
                        }
                    }
                }
            };

        private static EsiSkinrRecipe FullRecipe(string blendMode = "normal") =>
            new()
            {
                Id = "abc-123",
                Name = "Warchief's Reckoning",
                Line = "Tribal",
                CreatorId = 90000001L,
                ShipTypeId = RifterTypeId,
                Tier = new EsiSkinrTier { Level = 3 },
                Layout = new EsiSkinrLayout
                {
                    PatternBlendMode = blendMode,
                    Slots = new List<EsiSkinrSlot>
                    {
                        Nanocoating(SkinrSlot.PrimaryNanocoating, MatPrimary),
                        Nanocoating(SkinrSlot.SecondaryNanocoating, MatSecondary),
                        Nanocoating(SkinrSlot.TertiaryNanocoating, MatTertiary),
                        Nanocoating(SkinrSlot.TechArea, MatTech),
                        Pattern(SkinrSlot.Pattern, PatternPrimary, slot1: true, slot4: true),
                        Nanocoating(SkinrSlot.PatternMaterial, PatternMatPrimary),
                        Pattern(SkinrSlot.SecondaryPattern, PatternSecondary, slot2: true, mirrored: true),
                        Nanocoating(SkinrSlot.SecondaryPatternMaterial, PatternMatSecondary)
                    }
                }
            };

        // ------------------------------------------------------------------
        // The catalog itself
        // ------------------------------------------------------------------

        [Fact]
        public void Catalog_ReadsEveryTable()
        {
            SkinrCatalog catalog = BuildCatalog();

            catalog.IsAvailable.Should().BeTrue();
            catalog.SchemaVersion.Should().Be(1);
            catalog.SdeBuild.Should().Be("3470007");
            catalog.Slots.Should().HaveCount(8);
            catalog.Components.Should().HaveCount(8);
            catalog.Hulls.Should().HaveCount(3);
            catalog.Rarities.Should().ContainKey(3);
            catalog.ComponentCategories[2].English.Should().Be("Pattern");
        }

        [Fact]
        public void Catalog_RejectsNewerSchemaRatherThanReadingItPartially()
        {
            System.Action read = () => SkinrCatalog.FromJson("""{ "schemaVersion": 99 }""");

            read.Should().Throw<System.NotSupportedException>()
                .WithMessage("*newer than the supported*");
        }

        [Fact]
        public void Catalog_MissingFileYieldsUnavailableRatherThanThrowing()
        {
            SkinrCatalog.Empty.IsAvailable.Should().BeFalse();
            SkinrCatalog.Empty.GetHull(RifterTypeId).Should().BeNull();
            SkinrCatalog.Empty.GetSlotConfiguration(RifterTypeId).Should().NotBeNull();
        }

        [Fact]
        public void Catalog_LowestPriorityConfigurationWins()
        {
            SkinrCatalog catalog = BuildCatalog();

            // 670 is in configuration 6 (priority 0) as well as the allow-all default.
            SkinrSlotConfiguration capsule = catalog.GetSlotConfiguration(670);
            capsule.Id.Should().Be(6);
            capsule.ForbidsCustomization.Should().BeTrue();

            // 588 is in configuration 7 (priority 1) — more specific than the default.
            catalog.GetSlotConfiguration(588).Id.Should().Be(7);
            catalog.GetSlotConfiguration(588).Allows(SkinrSlot.SecondaryNanocoating).Should().BeFalse();

            // Anything unclaimed falls to the priority-3 allow-all default.
            catalog.GetSlotConfiguration(RifterTypeId).Id.Should().Be(5);
        }

        [Fact]
        public void Catalog_AbsentFactionMeansIdentityNotUnknown()
        {
            SkinrCatalog catalog = BuildCatalog();

            // CCP only authors a map for factions that ship their own hulls.
            IReadOnlyDictionary<int, int> unmapped = catalog.GetSlotToMaterialMap(500099);
            unmapped.Should().Equal(new Dictionary<int, int> { [1] = 1, [2] = 2, [3] = 3, [4] = 4 });

            catalog.GetSlotToMaterialMap(null).Should().HaveCount(4);
            catalog.GetSlotToMaterialMap(MinmatarFactionId)
                .Should().Equal(new Dictionary<int, int> { [1] = 4, [2] = 3, [3] = 2, [4] = 1 });
        }

        [Fact]
        public void Catalog_TierThresholdsAreKeyedByShipTreeGroupNotInventoryGroup()
        {
            SkinrCatalog catalog = BuildCatalog();

            // shipTreeGroupID 4, not groupID 25.
            catalog.GetTierThresholds(4).Should().NotBeEmpty();
            catalog.GetTierThresholds(25).Should().BeEmpty();

            catalog.GetTierForPoints(4, 124).Should().Be(0);
            catalog.GetTierForPoints(4, 125).Should().Be(1);
            catalog.GetTierForPoints(4, 300).Should().Be(3);
            catalog.GetPointsToNextTier(4, 125).Should().Be(50);
        }

        // ------------------------------------------------------------------
        // The remap — the part that fails silently
        // ------------------------------------------------------------------

        [Fact]
        public void Resolve_OrdersNanocoatingsByDnaPositionNotEsiSlot()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            design.Nanocoatings.Should().HaveCount(4);
            design.Nanocoatings.Select(m => m.MaterialPosition).Should().Equal(1, 2, 3, 4);

            // Minmatar reverses: ESI slot 4 is the shader's first material.
            design.Nanocoatings.Select(m => m.SlotId).Should().Equal(
                SkinrSlot.TechArea, SkinrSlot.TertiaryNanocoating,
                SkinrSlot.SecondaryNanocoating, SkinrSlot.PrimaryNanocoating);
        }

        [Fact]
        public void Resolve_BuildsDnaWithFourMaterialsInRemappedOrder()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            design.IsRenderable.Should().BeTrue();
            design.Dna.Should().Be(
                "mf4_t1:minmatarbase:minmatar" +
                ":material?cosm_copper;cosm_brass;cosm_blue;cosm_azure" +
                ":pattern?cosm_blank_projection;cosm_gold;cosm_onyx");
        }

        [Fact]
        public void Resolve_TargetMaterialsAreRemappedToo()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            SkinrResolvedPattern primary = design.Patterns.Single(p => p.LayerIndex == 0);

            // ESI said slots 1 and 4. Minmatar maps 1→4 and 4→1, so the shader vector is
            // (1,0,0,1) — which happens to be symmetric here, so also assert the asymmetric case.
            primary.TargetMaterials.Should().Equal(1d, 0d, 0d, 1d);

            SkinrResolvedPattern secondary = design.Patterns.Single(p => p.LayerIndex == 1);

            // ESI said slot 2 only. 2→3, so element index 2 is set, not index 1.
            secondary.TargetMaterials.Should().Equal(0d, 0d, 1d, 0d);
        }

        // ------------------------------------------------------------------
        // The mask contract
        // ------------------------------------------------------------------

        [Fact]
        public void Resolve_MaterialIndexIsFourForLayerOneAndFiveForLayerTwo()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            // EveSOFDataPatternLayer::MaterialSource — SOURCE_PATTERN1 = 4, SOURCE_PATTERN2 = 5.
            design.Patterns.Single(p => p.LayerIndex == 0).MaterialIndex.Should().Be(4);
            design.Patterns.Single(p => p.LayerIndex == 1).MaterialIndex.Should().Be(5);

            design.Patterns.Single(p => p.LayerIndex == 0).TextureName.Should().Be("PatternMask1Map");
            design.Patterns.Single(p => p.LayerIndex == 1).TextureName.Should().Be("PatternMask2Map");
        }

        // ------------------------------------------------------------------
        // Blend modes — a shader permutation, measured in the lab
        // ------------------------------------------------------------------
        //
        // `pattern_blend_mode` arrived on the wire, was parsed, stored, and read by NOTHING for
        // the whole investigation — the same fate as the mask report's `unclaimed`. It is why a
        // Tristan's exclusion swirl rendered dark and an Astero's nested_inverted stripes
        // stacked into a blob. The compositing math lives in CCP's compiled shaders as
        // BLEND_MODE_* permutations (strings-scanned out of the live quadv5.sm_hi), selected by
        // an options entry the sidecar writes per hull effect; the host's whole job is to put
        // the mode on the build request. An interim fix remapped the secondary mask's material
        // source instead ("the eraser") and was measured producing exactly HALF of exclusion —
        // brass swirl on the black panel, no black swirl on the orange side — because XOR needs
        // both mask values per pixel and sequential source lerps only ever see one. These tests
        // pin the two facts that outlived it.

        [Theory]
        [InlineData("normal")]
        [InlineData("subtract")]
        [InlineData("exclusion")]
        [InlineData("nested")]
        [InlineData("nested_inverted")]
        public void Resolve_MaterialIndexStaysThePatternSourceInEveryBlendMode(string mode)
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe(mode));

            // The masks are carriers; the shader permutation does the compositing. Any mode
            // that changes these indices is repeating the eraser mistake.
            design.Patterns.Single(p => p.LayerIndex == 0).MaterialIndex.Should().Be(4);
            design.Patterns.Single(p => p.LayerIndex == 1).MaterialIndex.Should().Be(5);
        }

        [Fact]
        public void Resolve_TheBlendModeReachesTheBuildRequest()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe("exclusion"));

            SkinrSidecarRequest request = SkinrSidecarHost.BuildBuildRequest(design, "geo.cmf");

            // The field was deserialized and read by nothing for the entire investigation; this
            // is the assertion that keeps it wired.
            request.PatternBlendMode.Should().Be("exclusion");
        }

        [Fact]
        public void Resolve_SamplerAddressesUseTheEngineEnumNotTheClientPythons()
        {
            // Tr2RenderContextEnum::TextureAddressMode: WRAP=1, CLAMP=3, BORDER=4. The
            // decompiled studio python maps CLAMP→4 / BORDER→3 — swapped relative to the
            // engine — and copying it cut the Astero's spine stripe at its projection box.
            // With the engine values the stripe extends nose to tail, matching the game.
            // These numbers were settled by pixels, not by whose constant table sounded
            // more authoritative.
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            // Component 201 is (clamp-to-border, clamp-to-edge); 202 is (clamp-to-edge, repeat).
            SkinrResolvedPattern primary = design.Patterns.Single(p => p.LayerIndex == 0);
            primary.SamplerAddressU.Should().Be(4);
            primary.SamplerAddressV.Should().Be(3);

            SkinrResolvedPattern secondary = design.Patterns.Single(p => p.LayerIndex == 1);
            secondary.SamplerAddressU.Should().Be(3);
            secondary.SamplerAddressV.Should().Be(1);

            SkinrSidecarRequest request = SkinrSidecarHost.BuildBuildRequest(design, "geo.cmf");
            request.Masks![0].SamplerU.Should().Be(4);
            request.Masks[0].SamplerV.Should().Be(3);
            request.Masks[1].SamplerU.Should().Be(3);
            request.Masks[1].SamplerV.Should().Be(1);
        }

        [Fact]
        public void Resolve_TheDarkhullOrderCarriesTheTechCoating()
        {
            // cosmeticsManager.SetModel + SetMaterialOnDarkhull: darkhull areas — skipped by
            // ordinary material application by design — get their MaterialMap swapped to the
            // constant-index texture of wherever the faction remap sent the TECH slot, plus the
            // tech coating's constants. Minmatar maps tech (slot 4) to position 1 → black.dds.
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            SkinrSidecarRequest request = SkinrSidecarHost.BuildBuildRequest(design, "geo.cmf");

            request.Darkhull.Should().NotBeNull();
            request.Darkhull!.MaterialMap.Should().Be("res:/texture/Global/black.dds");
            request.Darkhull.Material.Should().Be("res:/mat/copper.red");
        }

        [Fact]
        public void Resolve_AnEmptyTechSlotStillSwapsTheDarkhullMapButCopiesNothing()
        {
            // CCP's SetModel swaps the MaterialMap unconditionally; only the parameter copy
            // needs an actual coating. An empty tech slot must not fabricate a material path.
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id != SkinrSlot.TechArea).ToList();

            SkinrSidecarRequest request = SkinrSidecarHost.BuildBuildRequest(
                BuildResolver().Resolve(recipe), "geo.cmf");

            request.Darkhull.Should().NotBeNull();
            request.Darkhull!.Material.Should().BeNull();
        }

        [Theory]
        [InlineData("subtract")]
        [InlineData("exclusion")]
        [InlineData("nested_inverted")]
        public void Resolve_ACompositedSecondaryWithoutItsOwnMaterialIsNotAccusedOfMissingOne(
            string mode)
        {
            // The exact shape ESI ships: the Tristan "TwoFace" is `exclusion` with slot 8
            // legitimately absent — the shader composites the secondary against the primary and
            // never paints its material. Warning about the empty slot accused every such design
            // of being broken.
            EsiSkinrRecipe recipe = FullRecipe(mode);
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id != SkinrSlot.SecondaryPatternMaterial).ToList();

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Warnings.Should().NotContain(w => w.Contains("no material"));
            design.Patterns.Should().HaveCount(2, "the composited layer must still reach a mask");
        }

        [Theory]
        [InlineData("normal")]
        [InlineData("nested")]
        public void Resolve_APaintingSecondaryWithoutItsOwnMaterialStillWarns(string mode)
        {
            // Under normal and nested the secondary layer's own material shows, so an empty
            // slot 8 really does degrade the render and must keep saying so.
            EsiSkinrRecipe recipe = FullRecipe(mode);
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id != SkinrSlot.SecondaryPatternMaterial).ToList();

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Warnings.Should().Contain(w => w.Contains("no material"));
        }

        [Fact]
        public void Resolve_ClampIsSetOnlyForClampToEdge()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            // CCP collapses a three-value enum with `== TA_CLAMP`, so clamp-to-border is false,
            // exactly like repeat. Component 201 is (clamp-to-border, clamp-to-edge).
            SkinrResolvedPattern primary = design.Patterns.Single(p => p.LayerIndex == 0);
            primary.ClampU.Should().BeFalse();
            primary.ClampV.Should().BeTrue();

            // Component 202 is (clamp-to-edge, repeat).
            SkinrResolvedPattern secondary = design.Patterns.Single(p => p.LayerIndex == 1);
            secondary.ClampU.Should().BeTrue();
            secondary.ClampV.Should().BeFalse();
        }

        [Fact]
        public void Resolve_CarriesTransformAndMirrorVerbatim()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            SkinrResolvedPattern primary = design.Patterns.Single(p => p.LayerIndex == 0);
            primary.Position.Should().Equal(1.5, -2.5, 3.5);
            primary.Rotation.Should().Equal(0d, 0d, 0.7071068, 0.7071067);
            primary.Scaling.Should().Equal(17.5, 17.5, 17.5);
            primary.IsMirrored.Should().BeFalse();

            design.Patterns.Single(p => p.LayerIndex == 1).IsMirrored.Should().BeTrue();
        }

        [Fact]
        public void Resolve_PairsEachPatternWithItsOwnMaterialSlot()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            SkinrResolvedPattern primary = design.Patterns.Single(p => p.LayerIndex == 0);
            primary.PatternSlotId.Should().Be(SkinrSlot.Pattern);
            primary.MaterialSlotId.Should().Be(SkinrSlot.PatternMaterial);
            primary.Pattern!.Name.English.Should().Be("Division");
            primary.Material!.Name.English.Should().Be("Gold Leaf");
            primary.TextureResourcePath.Should().Be("res:/texture/projection/cosm_stripe_2k.dds");

            SkinrResolvedPattern secondary = design.Patterns.Single(p => p.LayerIndex == 1);
            secondary.PatternSlotId.Should().Be(SkinrSlot.SecondaryPattern);
            secondary.MaterialSlotId.Should().Be(SkinrSlot.SecondaryPatternMaterial);
            secondary.Material!.Name.English.Should().Be("Onyx");
        }

        // ------------------------------------------------------------------
        // Partial and degenerate designs
        // ------------------------------------------------------------------

        [Fact]
        public void Resolve_EmptySlotEmitsTheEngineRecognizedSentinel()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = new List<EsiSkinrSlot>
            {
                Nanocoating(SkinrSlot.PrimaryNanocoating, MatPrimary)
            };

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            // material? validates at arity four and nothing else, so the three empty slots still
            // have to emit a token — and "none" is what the engine accepts.
            design.Dna.Should().Be(
                "mf4_t1:minmatarbase:minmatar:material?none;none;none;cosm_azure");
            design.Nanocoatings.Where(m => m.IsEmpty).Should().HaveCount(3);
            design.Nanocoatings.Where(m => m.IsEmpty).Should().OnlyContain(m => m.DnaToken == "none");
        }

        [Fact]
        public void Resolve_NoPatternsOmitsThePatternCommandEntirely()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id <= SkinrSlot.TechArea).ToList();

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Patterns.Should().BeEmpty();
            design.Dna.Should().NotContain("pattern?");
        }

        [Fact]
        public void Resolve_SecondaryPatternWithoutPrimaryKeepsItsArgumentPosition()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id != SkinrSlot.Pattern && s.Id != SkinrSlot.PatternMaterial)
                .ToList();

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Patterns.Should().HaveCount(1);
            design.Patterns[0].LayerIndex.Should().Be(1);
            design.Patterns[0].MaterialIndex.Should().Be(5);

            // The second pattern material must stay the second argument.
            design.Dna.Should().EndWith(":pattern?cosm_blank_projection;none;cosm_onyx");
        }

        [Fact]
        public void Resolve_PatternWithNoMaterialWarnsRatherThanDroppingTheLayer()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = recipe.Layout.Slots
                .Where(s => s.Id != SkinrSlot.PatternMaterial).ToList();

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Patterns.Should().HaveCount(2);
            design.Warnings.Should().Contain(w => w.Contains("no material"));
            design.Dna.Should().Contain("pattern?cosm_blank_projection;none;cosm_onyx");
        }

        [Fact]
        public void Resolve_UnknownComponentWarnsInsteadOfBecomingABlankSlot()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.Layout.Slots = new List<EsiSkinrSlot>
            {
                Nanocoating(SkinrSlot.PrimaryNanocoating, 999999)
            };

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.Warnings.Should().Contain(w => w.Contains("999999"));
            design.Nanocoatings.Single(m => m.SlotId == SkinrSlot.PrimaryNanocoating)
                .Component.Should().BeNull();
        }

        [Fact]
        public void Resolve_HullWithoutSofIdentityIsDescribableButNotRenderable()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.ShipTypeId = 670;

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.IsRenderable.Should().BeFalse();
            design.Dna.Should().BeEmpty();
            design.Name.Should().Be("Warchief's Reckoning");
            design.Hull!.Name.English.Should().Be("Capsule");
            design.Warnings.Should().Contain(w => w.Contains("SpaceObjectFactory identity"));
        }

        [Fact]
        public void Resolve_SlotFilledOutsideTheHullsConfigurationIsFlagged()
        {
            EsiSkinrRecipe recipe = FullRecipe();
            recipe.ShipTypeId = 588; // configuration 7 — no secondary slot

            SkinrResolvedDesign design = BuildResolver().Resolve(recipe);

            design.SlotConfiguration.Id.Should().Be(7);
            design.Warnings.Should().Contain(w => w.Contains("does not offer it"));
        }

        [Fact]
        public void Resolve_NullRecipeYieldsAnEmptyDesignRatherThanThrowing()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(null);

            design.IsRenderable.Should().BeFalse();
            design.Warnings.Should().NotBeEmpty();
            design.Nanocoatings.Should().BeEmpty();
        }

        [Fact]
        public void Resolve_WithoutACatalogStillReturnsSomethingUsable()
        {
            var resolver = new SkinrRecipeResolver(SkinrCatalog.Empty);

            SkinrResolvedDesign design = resolver.Resolve(FullRecipe());

            design.IsRenderable.Should().BeFalse();
            design.Name.Should().Be("Warchief's Reckoning");
            design.Warnings.Should().Contain(w => w.Contains("static data is unavailable"));
            // No catalog means no remap, so slots stay in identity order.
            design.Nanocoatings.Select(m => m.SlotId).Should().Equal(1, 2, 3, 4);
        }

        // ------------------------------------------------------------------
        // Points and tier
        // ------------------------------------------------------------------

        [Fact]
        public void Resolve_SumsPointsAcrossAllSixMaterialsAndBothPatterns()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            // 101 mat/std 25 + 102 metallic/std 100 + 103 mat/elite 100 + 104 mat/std 25
            // + 201 pattern/std 75 + 301 metallic/elite 300
            // + 202 pattern/elite 250 + 302 mat/std 25
            design.DesignPoints.Should().Be(900);
        }

        [Fact]
        public void Resolve_ComputesTierAndFlagsDisagreementWithEsi()
        {
            SkinrResolvedDesign design = BuildResolver().Resolve(FullRecipe());

            // 900 points is past the top threshold in the fixture (375 → tier 4).
            design.ComputedTier.Should().Be(4);
            design.TierLevel.Should().Be(3);
            design.TierMismatch.Should().BeTrue();
            design.PointsToNextTier.Should().BeNull();
        }

        [Fact]
        public void GetDesignPoints_IgnoresUnknownIdsWithoutFailing()
        {
            SkinrRecipeResolver resolver = BuildResolver();

            resolver.GetDesignPoints(new[] { MatPrimary, 999999, MatTertiary }).Should().Be(125);
            resolver.GetDesignPoints(null!).Should().Be(0);
        }

        [Fact]
        public void GetComponentName_FallsBackToTheIdSoTheUiAlwaysHasSomething()
        {
            SkinrRecipeResolver resolver = BuildResolver();

            resolver.GetComponentName(MatPrimary).Should().Be("Azure Matte");
            resolver.GetComponentName(999999).Should().Be("Component 999999");
        }

        [Fact]
        public void Resolve_PreservesBlendModeBecauseItChangesTheRender()
        {
            BuildResolver().Resolve(FullRecipe("nested_inverted"))
                .PatternBlendMode.Should().Be("nested_inverted");

            EsiSkinrRecipe missing = FullRecipe();
            missing.Layout.PatternBlendMode = null!;
            BuildResolver().Resolve(missing).PatternBlendMode.Should().Be("normal");
        }

        // ------------------------------------------------------------------
        // DNA composition, independent of any catalog
        // ------------------------------------------------------------------

        [Fact]
        public void Dna_PadsAndTruncatesToTheRequiredArity()
        {
            SkinrHull hull = BuildCatalog().GetHull(RifterTypeId)!;

            SkinrDna.Build(hull, new[] { "a" }, null)
                .Should().Be("mf4_t1:minmatarbase:minmatar:material?a;none;none;none");

            SkinrDna.Build(hull, new[] { "a", "b", "c", "d", "e" }, null)
                .Should().Be("mf4_t1:minmatarbase:minmatar:material?a;b;c;d");

            SkinrDna.Build(hull, new[] { "a", "", "  ", null! }, null)
                .Should().Be("mf4_t1:minmatarbase:minmatar:material?a;none;none;none");
        }

        [Fact]
        public void Dna_IsEmptyWithoutASofIdentity()
        {
            SkinrDna.Build(null, new[] { "a" }, null).Should().BeEmpty();
            SkinrDna.Build(BuildCatalog().GetHull(670), new[] { "a" }, null).Should().BeEmpty();
        }

        [Fact]
        public void Dna_PatternCommandAlwaysCarriesBothMaterialArguments()
        {
            SkinrHull hull = BuildCatalog().GetHull(RifterTypeId)!;

            // pattern? validates at arity three exactly — the materials are not optional.
            SkinrDna.Build(hull, new[] { "a", "b", "c", "d" }, new[] { "p1" })
                .Should().EndWith(":pattern?cosm_blank_projection;p1;none");

            SkinrDna.Build(hull, new[] { "a", "b", "c", "d" }, System.Array.Empty<string>())
                .Should().NotContain("pattern?");
        }
    }
}
