// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Constants;
using EveLens.Common.Services;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services
{
    public class EsiScopePresetTests
    {
        [Fact]
        public void FullMonitoring_ReturnsAllScopes()
        {
            var scopes = EsiScopePresets.GetScopesForPreset(EsiScopePresets.FullMonitoring);
            scopes.Should().BeEquivalentTo(EsiScopePresets.AllScopes);
        }

        [Fact]
        public void SkillPlannerOnly_ReturnsCorrectSubset()
        {
            var scopes = EsiScopePresets.GetScopesForPreset(EsiScopePresets.SkillPlannerOnly);

            scopes.Should().Contain("esi-skills.read_skills.v1");
            scopes.Should().Contain("esi-skills.read_skillqueue.v1");
            scopes.Should().Contain("esi-clones.read_clones.v1");
            scopes.Should().Contain("esi-clones.read_implants.v1");
            scopes.Should().Contain("esi-characters.read_fatigue.v1");
            scopes.Should().Contain("esi-universe.read_structures.v1");
            scopes.Should().Contain("esi-characters.read_agents_research.v1");

            // Should not contain wallet, market, mail, etc.
            scopes.Should().NotContain("esi-wallet.read_character_wallet.v1");
            scopes.Should().NotContain("esi-markets.read_character_orders.v1");
            scopes.Should().NotContain("esi-mail.read_mail.v1");
        }

        [Fact]
        public void StandardMonitoring_ReturnsCorrectSubset()
        {
            var scopes = EsiScopePresets.GetScopesForPreset(EsiScopePresets.StandardMonitoring);

            // Should include skills, wallet, assets, market, contracts, industry
            scopes.Should().Contain("esi-skills.read_skills.v1");
            scopes.Should().Contain("esi-wallet.read_character_wallet.v1");
            scopes.Should().Contain("esi-assets.read_assets.v1");
            scopes.Should().Contain("esi-markets.read_character_orders.v1");
            scopes.Should().Contain("esi-contracts.read_character_contracts.v1");
            scopes.Should().Contain("esi-industry.read_character_jobs.v1");

            // Should not include mail, notifications, calendar, planetary, kills, corp data
            scopes.Should().NotContain("esi-mail.read_mail.v1");
            scopes.Should().NotContain("esi-characters.read_notifications.v1");
            scopes.Should().NotContain("esi-calendar.read_calendar_events.v1");
            scopes.Should().NotContain("esi-planets.manage_planets.v1");
            scopes.Should().NotContain("esi-killmails.read_killmails.v1");
            scopes.Should().NotContain("esi-corporations.read_structures.v1");
        }

        [Fact]
        public void DetectPreset_IdentifiesFullMonitoring()
        {
            var allScopes = new HashSet<string>(EsiScopePresets.AllScopes);
            EsiScopePresets.DetectPreset(allScopes).Should().Be(EsiScopePresets.FullMonitoring);
        }

        [Fact]
        public void DetectPreset_IdentifiesSkillPlannerOnly()
        {
            var scopes = EsiScopePresets.GetScopesForPreset(EsiScopePresets.SkillPlannerOnly);
            EsiScopePresets.DetectPreset(scopes).Should().Be(EsiScopePresets.SkillPlannerOnly);
        }

        [Fact]
        public void DetectPreset_IdentifiesStandardMonitoring()
        {
            var scopes = EsiScopePresets.GetScopesForPreset(EsiScopePresets.StandardMonitoring);
            EsiScopePresets.DetectPreset(scopes).Should().Be(EsiScopePresets.StandardMonitoring);
        }

        [Fact]
        public void DetectPreset_ReturnsCustomForArbitrarySubset()
        {
            var scopes = new HashSet<string> { "esi-skills.read_skills.v1" };
            EsiScopePresets.DetectPreset(scopes).Should().Be(EsiScopePresets.Custom);
        }

        [Fact]
        public void GetCustomDescription_ReportsCorrectCounts()
        {
            var selected = new HashSet<string> { "esi-skills.read_skills.v1", "esi-skills.read_skillqueue.v1" };
            string desc = EsiScopePresets.GetCustomDescription(selected);

            desc.Should().Contain("2 of ");
            desc.Should().Contain("Unavailable:");
        }

        [Fact]
        public void GetCustomDescription_NoUnavailable_WhenAllSelected()
        {
            var allScopes = new HashSet<string>(EsiScopePresets.AllScopes);
            string desc = EsiScopePresets.GetCustomDescription(allScopes);

            desc.Should().NotContain("Unavailable:");
        }

        [Fact]
        public void AllScopes_MatchSSOScopesConstant()
        {
            // The AllScopes in EsiScopePresets should cover the same scopes as NetworkConstants.SSOScopes
            var ssoScopes = new HashSet<string>(NetworkConstants.SSOScopes.Split(' '));
            var presetScopes = new HashSet<string>(EsiScopePresets.AllScopes);

            presetScopes.Should().BeEquivalentTo(ssoScopes,
                "all scopes in the preset system should match the SSO scopes constant");
        }

        [Fact]
        public void PresetKeys_DoNotIncludeCustom()
        {
            EsiScopePresets.PresetKeys.Should().NotContain(EsiScopePresets.Custom);
        }

        [Fact]
        public void FeatureGroups_AllHaveNonEmptyScopes()
        {
            foreach (var group in EsiScopePresets.FeatureGroups)
            {
                group.Scopes.Should().NotBeEmpty($"group '{group.Name}' should have scopes");
            }
        }

        [Fact]
        public void SkillPlannerOnly_IsSubsetOfStandardMonitoring()
        {
            var skillPlanner = EsiScopePresets.GetScopesForPreset(EsiScopePresets.SkillPlannerOnly);
            var standard = EsiScopePresets.GetScopesForPreset(EsiScopePresets.StandardMonitoring);

            skillPlanner.IsSubsetOf(standard).Should().BeTrue(
                "Skill Planner scopes should be a subset of Standard Monitoring scopes");
        }

        [Fact]
        public void StandardMonitoring_IsSubsetOfFullMonitoring()
        {
            var standard = EsiScopePresets.GetScopesForPreset(EsiScopePresets.StandardMonitoring);
            var full = EsiScopePresets.GetScopesForPreset(EsiScopePresets.FullMonitoring);

            standard.IsSubsetOf(full).Should().BeTrue(
                "Standard Monitoring scopes should be a subset of Full Monitoring scopes");
        }
    }
}
