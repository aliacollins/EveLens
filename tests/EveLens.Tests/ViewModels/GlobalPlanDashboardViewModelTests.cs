// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    [Collection("AppServices")]
    public class GlobalPlanDashboardViewModelTests : IDisposable
    {
        public GlobalPlanDashboardViewModelTests()
        {
            AppServices.Reset();
            EveLens.Common.Settings.GlobalPlanTemplates = new List<GlobalPlanTemplate>();
        }

        public void Dispose()
        {
            EveLens.Common.Settings.GlobalPlanTemplates = new List<GlobalPlanTemplate>();
            AppServices.Reset();
        }

        [Fact]
        public void Refresh_LoadsTemplatesFromSettings()
        {
            EveLens.Common.Settings.GlobalPlanTemplates.Add(new GlobalPlanTemplate { Name = "Doctrine A" });
            EveLens.Common.Settings.GlobalPlanTemplates.Add(new GlobalPlanTemplate { Name = "Doctrine B" });

            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();

            vm.Templates.Should().HaveCount(2);
            vm.Templates[0].Name.Should().Be("Doctrine A");
            vm.Templates[1].Name.Should().Be("Doctrine B");
        }

        [Fact]
        public void CreateTemplate_AddsToSettingsAndTemplates()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();

            var template = vm.CreateTemplate("My Template");

            template.Name.Should().Be("My Template");
            vm.Templates.Should().HaveCount(1);
            EveLens.Common.Settings.GlobalPlanTemplates.Should().HaveCount(1);
            template.Id.Should().NotBeNullOrEmpty();
        }

        [Fact]
        public void DeleteTemplate_RemovesFromSettingsAndTemplates()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("To Delete");

            vm.DeleteTemplate(template);

            vm.Templates.Should().BeEmpty();
            EveLens.Common.Settings.GlobalPlanTemplates.Should().BeEmpty();
        }

        [Fact]
        public void RenameTemplate_UpdatesName()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Old Name");

            vm.RenameTemplate(template, "New Name");

            template.Name.Should().Be("New Name");
        }

        [Fact]
        public void SelectTemplate_WithNull_ClearsState()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            vm.CreateTemplate("Test");

            vm.SelectTemplate(null);

            vm.SelectedTemplate.Should().BeNull();
            vm.SubscribedCharacters.Should().BeEmpty();
            vm.ComparisonRows.Should().BeEmpty();
        }

        [Fact]
        public void SelectTemplate_SetsSelectedAndBuildsRows()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");

            vm.SelectTemplate(template);

            vm.SelectedTemplate.Should().Be(template);
        }

        [Fact]
        public void AddSkill_ToNullTemplate_ReturnsFalse()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();

            bool result = vm.AddSkill(12345, 3);

            result.Should().BeFalse();
        }

        [Fact]
        public void AddSkill_WithInvalidId_ReturnsFalse()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");
            vm.SelectTemplate(template);

            bool result = vm.AddSkill(-1, 3);

            result.Should().BeFalse();
        }

        [Fact]
        public void RemoveSkill_RemovesEntry()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");
            template.Entries.Add(new GlobalPlanTemplateEntry
            {
                SkillID = 100,
                SkillName = "Test Skill",
                Level = 3
            });
            vm.SelectTemplate(template);

            vm.RemoveSkill(100, 3);

            template.Entries.Should().BeEmpty();
        }

        [Fact]
        public void SubscribeCharacter_NullTemplate_DoesNothing()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();

            var identity = new EveLens.Common.Models.CharacterIdentity(1L, "Test");
            var character = new EveLens.Common.Models.CCPCharacter(
                identity, new NullCharacterServices());

            vm.SubscribeCharacter(character);

            vm.SubscribedCharacters.Should().BeEmpty();
        }

        [Fact]
        public void Template_Entries_PreventDuplicates()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");
            vm.SelectTemplate(template);

            template.Entries.Add(new GlobalPlanTemplateEntry
            {
                SkillID = 100,
                SkillName = "Duplicate",
                Level = 3
            });

            vm.AddSkill(100, 3);

            template.Entries.Where(e => e.SkillID == 100 && e.Level == 3).Should().HaveCount(1);
        }

        [Fact]
        public void DeleteTemplate_WhenSelected_ClearsSelection()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");
            vm.SelectTemplate(template);

            vm.DeleteTemplate(template);

            vm.SelectedTemplate.Should().BeNull();
        }

        [Fact]
        public void TotalSkillsInTemplate_ReturnsCorrectCount()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Test");
            template.Entries.Add(new GlobalPlanTemplateEntry { SkillID = 1, Level = 1 });
            template.Entries.Add(new GlobalPlanTemplateEntry { SkillID = 2, Level = 3 });
            vm.SelectTemplate(template);

            vm.TotalSkillsInTemplate.Should().Be(2);
        }

        [Fact]
        public void GetCharacterTotalTime_NoRows_ReturnsZero()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Empty");
            vm.SelectTemplate(template);

            vm.GetCharacterTotalTime(0).Should().Be(TimeSpan.Zero);
        }

        [Fact]
        public void GetCharacterTrainedCount_NoRows_ReturnsZero()
        {
            var vm = new GlobalPlanDashboardViewModel();
            vm.Refresh();
            var template = vm.CreateTemplate("Empty");
            vm.SelectTemplate(template);

            vm.GetCharacterTrainedCount(0).Should().Be(0);
        }

        [Fact]
        public void Template_PersistsId()
        {
            var template = new GlobalPlanTemplate { Name = "Persist Test" };

            template.Id.Should().NotBeNullOrEmpty();
            template.Id.Should().HaveLength(32);
        }

        [Fact]
        public void Template_DefaultsToEmptyCollections()
        {
            var template = new GlobalPlanTemplate();

            template.Entries.Should().NotBeNull().And.BeEmpty();
            template.SubscribedCharacterGuids.Should().NotBeNull().And.BeEmpty();
        }
    }
}
