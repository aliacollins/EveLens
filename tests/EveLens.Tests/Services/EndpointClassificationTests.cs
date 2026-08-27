// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations.CCPAPI;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    public class EndpointClassificationTests
    {
        [Fact]
        public void CoreEndpoints_Contains_Eight_Methods()
        {
            EndpointClassification.CoreEndpoints.Should().HaveCount(8);
        }

        [Theory]
        [InlineData(ESIAPICharacterMethods.CharacterSheet)]
        [InlineData(ESIAPICharacterMethods.Skills)]
        [InlineData(ESIAPICharacterMethods.SkillQueue)]
        [InlineData(ESIAPICharacterMethods.AccountBalance)]
        [InlineData(ESIAPICharacterMethods.Location)]
        [InlineData(ESIAPICharacterMethods.Ship)]
        [InlineData(ESIAPICharacterMethods.Clones)]
        [InlineData(ESIAPICharacterMethods.Implants)]
        public void IsCore_Returns_True_For_Core_Methods(ESIAPICharacterMethods method)
        {
            EndpointClassification.IsCore(method).Should().BeTrue();
        }

        [Theory]
        [InlineData(ESIAPICharacterMethods.AssetList)]
        [InlineData(ESIAPICharacterMethods.MarketOrders)]
        [InlineData(ESIAPICharacterMethods.Contracts)]
        [InlineData(ESIAPICharacterMethods.IndustryJobs)]
        [InlineData(ESIAPICharacterMethods.WalletJournal)]
        [InlineData(ESIAPICharacterMethods.WalletTransactions)]
        [InlineData(ESIAPICharacterMethods.MailMessages)]
        [InlineData(ESIAPICharacterMethods.Notifications)]
        [InlineData(ESIAPICharacterMethods.KillLog)]
        [InlineData(ESIAPICharacterMethods.PlanetaryColonies)]
        [InlineData(ESIAPICharacterMethods.ResearchPoints)]
        [InlineData(ESIAPICharacterMethods.EmploymentHistory)]
        [InlineData(ESIAPICharacterMethods.ContactList)]
        [InlineData(ESIAPICharacterMethods.Standings)]
        [InlineData(ESIAPICharacterMethods.FactionalWarfareStats)]
        [InlineData(ESIAPICharacterMethods.Medals)]
        [InlineData(ESIAPICharacterMethods.LoyaltyPoints)]
        public void IsCore_Returns_False_For_OnDemand_Methods(ESIAPICharacterMethods method)
        {
            EndpointClassification.IsCore(method).Should().BeFalse();
        }

        [Fact]
        public void TabToEndpoint_Contains_All_OnDemand_Methods()
        {
            EndpointClassification.TabToEndpoint.Should().HaveCount(17);
        }

        [Theory]
        [InlineData("Assets", ESIAPICharacterMethods.AssetList)]
        [InlineData("Orders", ESIAPICharacterMethods.MarketOrders)]
        [InlineData("Contracts", ESIAPICharacterMethods.Contracts)]
        [InlineData("Industry", ESIAPICharacterMethods.IndustryJobs)]
        [InlineData("Journal", ESIAPICharacterMethods.WalletJournal)]
        [InlineData("Transactions", ESIAPICharacterMethods.WalletTransactions)]
        [InlineData("Mail", ESIAPICharacterMethods.MailMessages)]
        [InlineData("Notify", ESIAPICharacterMethods.Notifications)]
        [InlineData("Kills", ESIAPICharacterMethods.KillLog)]
        [InlineData("PI", ESIAPICharacterMethods.PlanetaryColonies)]
        [InlineData("Research", ESIAPICharacterMethods.ResearchPoints)]
        [InlineData("Employment", ESIAPICharacterMethods.EmploymentHistory)]
        [InlineData("Contacts", ESIAPICharacterMethods.ContactList)]
        [InlineData("Standings", ESIAPICharacterMethods.Standings)]
        [InlineData("FW", ESIAPICharacterMethods.FactionalWarfareStats)]
        [InlineData("Medals", ESIAPICharacterMethods.Medals)]
        [InlineData("LP", ESIAPICharacterMethods.LoyaltyPoints)]
        public void TabToEndpoint_Maps_Correctly(string tab, ESIAPICharacterMethods expected)
        {
            EndpointClassification.TabToEndpoint[tab].Should().Be(expected);
        }

        [Theory]
        [InlineData(ESIAPICharacterMethods.AssetList, "Asset Monitoring")]
        [InlineData(ESIAPICharacterMethods.MarketOrders, "Market Order Tracking")]
        [InlineData(ESIAPICharacterMethods.Contracts, "Contract Tracking")]
        [InlineData(ESIAPICharacterMethods.LoyaltyPoints, "Loyalty Points")]
        public void EndpointDisplayName_Returns_Friendly_Name(ESIAPICharacterMethods method, string expected)
        {
            EndpointClassification.EndpointDisplayName(method).Should().Be(expected);
        }

        [Fact]
        public void CoreEndpoints_And_TabToEndpoint_Are_Disjoint()
        {
            foreach (var tabEndpoint in EndpointClassification.TabToEndpoint.Values)
            {
                EndpointClassification.CoreEndpoints.Should().NotContain(tabEndpoint,
                    $"{tabEndpoint} is in both CoreEndpoints and TabToEndpoint");
            }
        }

        // --- scope-activated endpoints (SKINR) ---------------------------------
        // Regression for #139: the SKINR routes have no character-monitor tab, so
        // gating them on EnabledEndpoints left them permanently skipped — the
        // landing read a never-populated collection and showed an empty grid.

        [Theory]
        [InlineData(ESIAPICharacterMethods.SkinrLicenses)]
        [InlineData(ESIAPICharacterMethods.SkinrComponents)]
        public void Skinr_Endpoints_Are_ScopeActivated(ESIAPICharacterMethods method)
        {
            EndpointClassification.ScopeActivatedEndpoints.Should().Contain(method);
        }

        [Theory]
        [InlineData(ESIAPICharacterMethods.SkinrLicenses)]
        [InlineData(ESIAPICharacterMethods.SkinrComponents)]
        public void IsFetchAllowed_Permits_Skinr_Without_EnabledEndpoints_Entry(
            ESIAPICharacterMethods method)
        {
            EndpointClassification.IsFetchAllowed(method, new List<string>())
                .Should().BeTrue("scope-activated endpoints must not require a tab opt-in");
        }

        [Fact]
        public void IsFetchAllowed_Permits_Core_Without_EnabledEndpoints_Entry()
        {
            EndpointClassification.IsFetchAllowed(
                    ESIAPICharacterMethods.Skills, new List<string>())
                .Should().BeTrue();
        }

        [Fact]
        public void IsFetchAllowed_Blocks_OnDemand_Until_Enabled()
        {
            EndpointClassification.IsFetchAllowed(
                    ESIAPICharacterMethods.AssetList, new List<string>())
                .Should().BeFalse();
            EndpointClassification.IsFetchAllowed(
                    ESIAPICharacterMethods.AssetList, new List<string> { "AssetList" })
                .Should().BeTrue();
        }

        [Fact]
        public void ScopeActivated_And_Core_And_Tabs_Are_Disjoint()
        {
            foreach (var method in EndpointClassification.ScopeActivatedEndpoints)
            {
                EndpointClassification.CoreEndpoints.Should().NotContain(method);
                EndpointClassification.TabToEndpoint.Values.Should().NotContain(method);
            }
        }
    }
}
