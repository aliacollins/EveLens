// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// The clipboard text of "Copy Plan" and Export > "Copy to Clipboard" follows the order
    /// shown in the editor: the plan's sort and group-by-priority, the same as Save to File.
    /// </summary>
    [Collection("StaticData")]
    public class PlanEditorGameTextExportTests
    {
        private static (PlanEditorViewModel vm, Plan plan, PlanEntry first, PlanEntry second) CreateEditor()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            // Two independent skills so priorities can differ without prerequisites
            var standalone = StaticSkills.AllSkills
                .Where(s => s.IsPublic && !s.Prerequisites.Any())
                .OrderBy(s => s.Name)
                .Take(2)
                .ToList();
            standalone.Should().HaveCount(2);
            foreach (var skill in standalone)
                plan.PlanTo(skill, 1);

            var vm = new PlanEditorViewModel(new EventAggregator()) { Plan = plan };
            return (vm, plan, plan.ElementAt(0), plan.ElementAt(1));
        }

        private static List<string> Lines(string text) =>
            text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();

        private static List<string> DisplayOrder(PlanEditorViewModel vm) =>
            vm.DisplayPlan!.Select(e => $"{e.Skill.Name} {e.Level}").ToList();

        [Fact]
        public void ExportAsGameText_GroupByPriority_FollowsDisplayOrderNotStoredOrder()
        {
            var (vm, plan, first, second) = CreateEditor();

            // Stored order is first, second. Give the second entry the higher priority.
            vm.TrySetPriority(new[] { second }, 1).Should().BeTrue();
            vm.GroupByPriority = true;

            var expected = new List<string>
            {
                $"{second.Skill.Name} {second.Level}",
                $"{first.Skill.Name} {first.Level}",
            };
            DisplayOrder(vm).Should().Equal(expected, "precondition: grouping reorders the editor");

            Lines(vm.ExportAsGameText()).Should().Equal(expected,
                "the clipboard must match what the editor shows (Issue #160)");
            vm.Dispose();
        }

        [Fact]
        public void ExportAsGameText_ColumnSort_FollowsDisplayOrder()
        {
            var (vm, plan, first, second) = CreateEditor();

            plan.SortingPreferences.Criteria = PlanEntrySort.Name;
            plan.SortingPreferences.Order = ThreeStateSortOrder.Descending;
            vm.UpdateDisplayPlan();

            Lines(vm.ExportAsGameText()).Should().Equal(DisplayOrder(vm));
            vm.Dispose();
        }

        [Fact]
        public void ExportAsGameText_NoSort_KeepsManualOrder()
        {
            var (vm, plan, first, second) = CreateEditor();

            Lines(vm.ExportAsGameText()).Should().Equal(
                $"{first.Skill.Name} {first.Level}",
                $"{second.Skill.Name} {second.Level}");
            vm.Dispose();
        }

        [Fact]
        public void ExportAsGameText_DoesNotChangeStoredOrder()
        {
            var (vm, plan, first, second) = CreateEditor();
            vm.TrySetPriority(new[] { second }, 1).Should().BeTrue();
            vm.GroupByPriority = true;

            _ = vm.ExportAsGameText();

            plan.Select(e => e.Skill.Name).Should().Equal(first.Skill.Name, second.Skill.Name);
            vm.Dispose();
        }

        [Fact]
        public void ExportAsGameText_NoPlan_ReturnsEmpty()
        {
            var vm = new PlanEditorViewModel(new EventAggregator());
            vm.ExportAsGameText().Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void PlanGameTextExporter_MatchesFileExportOrder()
        {
            var (vm, plan, first, second) = CreateEditor();
            vm.TrySetPriority(new[] { second }, 1).Should().BeTrue();
            vm.GroupByPriority = true;

            var fileOrder = PlanScratchpad.CreateInDisplayOrder(plan)
                .Select(e => $"{e.Skill.Name} {e.Level}");

            Lines(PlanGameTextExporter.Export(plan)).Should().Equal(fileOrder);
            vm.Dispose();
        }
    }
}
