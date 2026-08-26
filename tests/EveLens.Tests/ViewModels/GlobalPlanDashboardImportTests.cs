// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// Doctrine Designer import and batch-subscribe paths added for Issue #137.
    /// Uses real static skill data because import resolves skills by ID and name.
    /// </summary>
    [Collection("StaticData")]
    public class GlobalPlanDashboardImportTests : IDisposable
    {
        public GlobalPlanDashboardImportTests()
        {
            EveLens.Common.Settings.GlobalPlanTemplates = new List<GlobalPlanTemplate>();
        }

        public void Dispose()
        {
            EveLens.Common.Settings.GlobalPlanTemplates = new List<GlobalPlanTemplate>();
        }

        [Fact]
        public void CreateFromPlanFile_ResolvesById_AndByNameFallback()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var byId = PlanTestFixture.GetSkill("Spaceship Command");
            var byName = StaticSkills.AllSkills.First(s => s.ID != byId.ID);

            var serial = new SerializablePlan { Name = "Alliance Doctrine" };
            serial.Entries.Add(new SerializablePlanEntry { ID = byId.ID, Level = 3 });
            // Old exports may carry a name but a stale/zero ID
            serial.Entries.Add(new SerializablePlanEntry { ID = 0, SkillName = byName.Name, Level = 1 });

            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateFromPlanFile(serial);

            template.Should().NotBeNull();
            template!.Name.Should().Be("Alliance Doctrine");
            template.Entries.Should().Contain(e => e.SkillID == byId.ID && e.Level == 3);
            template.Entries.Should().Contain(e => e.SkillID == byName.ID && e.Level == 1);
            EveLens.Common.Settings.GlobalPlanTemplates.Should().Contain(template);
        }

        [Fact]
        public void CreateFromPlanFile_NothingResolves_ReturnsNullAndSavesNothing()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();

            var serial = new SerializablePlan { Name = "Corrupt" };
            serial.Entries.Add(new SerializablePlanEntry { ID = -42, SkillName = "No Such Skill", Level = 5 });

            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();

            vm.CreateFromPlanFile(serial).Should().BeNull();
            EveLens.Common.Settings.GlobalPlanTemplates.Should().BeEmpty();
        }

        [Fact]
        public void SubscribeCharacters_AddsAll_SkipsAlreadySubscribed()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var first = PlanTestFixture.CreateTestCharacter("Pilot One", 1L);
            var second = PlanTestFixture.CreateTestCharacter("Pilot Two", 2L);

            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Group Add");
            vm.SelectTemplate(template);
            vm.SubscribeCharacter(first);

            int added = vm.SubscribeCharacters(new[] { first, second });

            added.Should().Be(1, "the first character was already subscribed");
            template.SubscribedCharacterGuids.Should().HaveCount(2);
            template.SubscribedCharacterGuids.Should().Contain(first.Guid).And.Contain(second.Guid);
        }
    }
}
