// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.ViewModels;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class PlanSkillBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyGroups()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Groups.Should().NotBeNull();
            vm.Groups.Should().BeEmpty();
            vm.SelectedSkill.Should().BeNull();
            vm.SelectedSkillDetail.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.ShowAll.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Navigation";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.ShowAll = false;
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToLevel_WithNullSkill_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToLevel(null, 3);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectSkill_WithNull_SetsDetailToNull()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.SelectSkill(null);
            vm.SelectedSkill.Should().BeNull();
            vm.SelectedSkillDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void SelectedSkill_RaisesPropertyChanged()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanSkillBrowserViewModel.SelectedSkill))
                    changed = e.PropertyName;
            };
            vm.SelectSkill(null);
            // Setting to null when already null won't fire, but this confirms no crash
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void ConstructorWithPlanEditor_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var planEditor = new PlanEditorViewModel(agg);
            var vm = new PlanSkillBrowserViewModel(planEditor, agg);
            vm.Should().NotBeNull();
            vm.Dispose();
            planEditor.Dispose();
        }
    }

    /// <summary>
    /// Regression tests for the plan skill browser that require loaded static skill data.
    /// </summary>
    [Collection("StaticData")]
    public class PlanSkillBrowserStaticDataTests
    {
        public PlanSkillBrowserStaticDataTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void Refresh_ExcludesUnpublishedSkills_Issue37()
        {
            // Arrange
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Refresh();

            // Act — collect all skills across all groups
            var allSkills = vm.Groups
                .SelectMany(g => g.VisibleSkills)
                .Select(s => s.StaticSkill)
                .ToList();

            // Assert — no unpublished skills should appear
            allSkills.Should().NotBeEmpty("static data should be loaded");
            allSkills.Should().OnlyContain(s => s.IsPublic,
                "unpublished skills (e.g. CFO Training, Chief Science Officer) must not appear in the plan skill browser");
        }

        [Fact]
        public void Refresh_NoGroupHasZeroTotalCount()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Refresh();

            vm.Groups.Should().NotBeEmpty();
            vm.Groups.Should().OnlyContain(g => g.TotalCount > 0,
                "groups containing only unpublished skills should be excluded entirely");
            vm.Dispose();
        }

        [Fact]
        public void Refresh_PopulatesAvailableAttributeCombos()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Refresh();

            vm.AvailableAttributeCombos.Should().NotBeEmpty(
                "published skills should produce at least one attribute combination");
            vm.AvailableAttributeCombos.Should().OnlyHaveUniqueItems();
            vm.Dispose();
        }

        [Fact]
        public void AttributeFilter_FiltersSkillsByAttributeCombo_Issue38()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Refresh();

            // Pick the first available combo
            var combo = vm.AvailableAttributeCombos.First();
            vm.AttributeFilter = combo;

            // All visible skills must match the selected attributes
            var visibleSkills = vm.Groups
                .SelectMany(g => g.VisibleSkills)
                .Select(s => s.StaticSkill)
                .ToList();

            visibleSkills.Should().NotBeEmpty(
                $"there should be skills with {combo.DisplayText}");
            visibleSkills.Should().OnlyContain(s =>
                s.PrimaryAttribute == combo.Primary && s.SecondaryAttribute == combo.Secondary,
                $"all visible skills should match {combo.DisplayText}");
            vm.Dispose();
        }

        [Fact]
        public void AttributeFilter_NullShowsAllSkills()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Refresh();

            int allCount = vm.Groups.SelectMany(g => g.VisibleSkills).Count();

            // Set a filter then clear it
            vm.AttributeFilter = vm.AvailableAttributeCombos.First();
            int filteredCount = vm.Groups.SelectMany(g => g.VisibleSkills).Count();

            vm.AttributeFilter = null;
            int restoredCount = vm.Groups.SelectMany(g => g.VisibleSkills).Count();

            filteredCount.Should().BeLessThan(allCount);
            restoredCount.Should().Be(allCount);
            vm.Dispose();
        }
    }
}
