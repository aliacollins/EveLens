// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Models;
using EveLens.Common.Models.Collections;
using EveLens.Common.Serialization.Esi;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// SKINR licenses/components as first-class monitored ESI routes: enum wiring, scope
    /// gating, compatibility dating, and model import. The scheduler machinery itself is
    /// covered elsewhere — these pin the pieces SKINR plugs into it.
    /// </summary>
    public class SkinrMonitoredDataTests
    {
        private const string SkinrScope = "esi.cosmetic.char:read";

        [Fact]
        public void EnumBits_AreUnique()
        {
            var values = Enum.GetValues<ESIAPICharacterMethods>()
                .Where(v => v != ESIAPICharacterMethods.None)
                .Select(v => (ulong)v)
                .ToList();

            values.Should().OnlyHaveUniqueItems();
            values.Should().Contain((ulong)ESIAPICharacterMethods.SkinrLicenses);
            values.Should().Contain((ulong)ESIAPICharacterMethods.SkinrComponents);
        }

        [Theory]
        [InlineData("ESISkinrLicenses", "/characters/{0:D}/cosmetics/skinr")]
        [InlineData("ESISkinrComponents", "/characters/{0:D}/cosmetics/skinr/components")]
        public void RoutePaths_ResolveFromNetworkConstants(string key, string expected)
        {
            NetworkConstants.ResourceManager.GetString(key).Should().Be(expected);
        }

        [Fact]
        public void ScopeMapping_RequiresCosmeticScope()
        {
            var withScope = new[] { SkinrScope };
            var withoutScope = new[] { "esi-assets.read_assets.v1" };

            EsiScopeMapping.HasScope(withScope, ESIAPICharacterMethods.SkinrLicenses)
                .Should().BeTrue();
            EsiScopeMapping.HasScope(withScope, ESIAPICharacterMethods.SkinrComponents)
                .Should().BeTrue();
            EsiScopeMapping.HasScope(withoutScope, ESIAPICharacterMethods.SkinrLicenses)
                .Should().BeFalse();
            EsiScopeMapping.HasScope(withoutScope, ESIAPICharacterMethods.SkinrComponents)
                .Should().BeFalse();
        }

        [Fact]
        public void CompatibilityDates_DateSkinrRoutes_AndNothingElse()
        {
            EsiCompatibilityDates.ForMethod(ESIAPICharacterMethods.SkinrLicenses)
                .Should().Be("2026-08-18");
            EsiCompatibilityDates.ForMethod(ESIAPICharacterMethods.SkinrComponents)
                .Should().Be("2026-08-18");
            // Path-versioned routes must stay undated — a stray header on /v5/assets
            // would be harmless today and a subtle break the day CCP honors it.
            EsiCompatibilityDates.ForMethod(ESIAPICharacterMethods.AssetList)
                .Should().BeNull();
            EsiCompatibilityDates.ForMethod(null).Should().BeNull();
        }

        [Fact]
        public void LicenseCollection_Import_ReplacesState()
        {
            var collection = new SkinrLicenseCollection();
            collection.Import(new EsiSkinrInventory
            {
                Licenses = new List<EsiSkinrLicense>
                {
                    new() { SkinrId = "aaa", Activated = true, Unactivated = 0 },
                    new() { SkinrId = "bbb", Activated = false, Unactivated = 3 },
                }
            });

            collection.Should().HaveCount(2);
            var second = collection.First(l => l.SkinrId == "bbb");
            second.Activated.Should().BeFalse();
            second.Unactivated.Should().Be(3);

            // Re-import replaces, never accumulates
            collection.Import(new EsiSkinrInventory());
            collection.Should().BeEmpty();
        }

        [Fact]
        public void ComponentCollection_Import_MapsRunsVariants()
        {
            var collection = new SkinrComponentCollection();
            collection.Import(new EsiSkinrComponentInventory
            {
                Licenses = new List<EsiSkinrComponentLicense>
                {
                    new()
                    {
                        ComponentId = 100, Type = "nanocoating",
                        Runs = new EsiSkinrComponentRuns { Remaining = 5 }
                    },
                    new()
                    {
                        ComponentId = 200, Type = "pattern",
                        Runs = new EsiSkinrComponentRuns { Unlimited = true }
                    },
                }
            });

            collection.Should().HaveCount(2);
            var coating = collection.First(c => c.ComponentId == 100);
            coating.IsPattern.Should().BeFalse();
            coating.RunsRemaining.Should().Be(5);
            coating.IsUnlimited.Should().BeFalse();

            var pattern = collection.First(c => c.ComponentId == 200);
            pattern.IsPattern.Should().BeTrue();
            pattern.RunsRemaining.Should().BeNull();
            pattern.IsUnlimited.Should().BeTrue();
        }
    }
}
