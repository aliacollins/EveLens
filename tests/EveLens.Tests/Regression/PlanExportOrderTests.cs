// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Common.ViewModels;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// The skill list copied to the clipboard, the text export and the plan editor's
    /// list all follow the plan's sorting preferences, and none of them reorders the plan.
    /// </summary>
    [Collection("StaticData")]
    public class PlanExportOrderTests
    {
        private static readonly Dictionary<string, long> s_romanLevels = new()
        {
            ["I"] = 1, ["II"] = 2, ["III"] = 3, ["IV"] = 4, ["V"] = 5,
        };

        public PlanExportOrderTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        /// <summary>
        /// Stored order: Navigation I, Navigation II (priority 3), CPU Management I (priority 1),
        /// Spaceship Command I (priority 2). None of the skills has a prerequisite.
        /// </summary>
        private static Plan CreatePrioritisedPlan(out CCPCharacter character)
        {
            character = PlanTestFixture.CreateTestCharacter();
            Plan plan = PlanTestFixture.CreateTestPlan(character, "Export order");
            plan.PlanTo(PlanTestFixture.GetSkill("Navigation"), 2);
            plan.PlanTo(PlanTestFixture.GetSkill("CPU Management"), 1);
            plan.PlanTo(PlanTestFixture.GetSkill("Spaceship Command"), 1);
            SetPriority(plan, "CPU Management", 1, 1);
            SetPriority(plan, "Spaceship Command", 1, 2);
            return plan;
        }

        private static void SetPriority(Plan plan, string skillName, long level, int priority)
        {
            PlanEntry? entry = plan.GetEntry(PlanTestFixture.GetSkill(skillName), level);
            entry.Should().NotBeNull($"{skillName} {level} should be planned");
            plan.SetPriority(plan.ToList(), new[] { entry! }, priority);
        }

        private static PlanExportSettings NamesOnlySettings() => new() { Markup = MarkupType.None };

        private static List<string> Lines(string text)
            => text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Trim())
                .Where(line => line.Length > 0)
                .ToList();

        /// <summary>Turns "Skill Name 2" or "Skill Name II" lines into "Skill Name|2" keys.</summary>
        private static List<string> OrderKeys(string text)
            => Lines(text).Select(line =>
            {
                int split = line.LastIndexOf(' ');
                split.Should().BeGreaterThan(0, $"'{line}' should be a 'Skill Name level' line");
                string name = line.Substring(0, split);
                string level = line.Substring(split + 1);
                return $"{name}|{(s_romanLevels.TryGetValue(level, out long roman) ? roman : long.Parse(level))}";
            }).ToList();

        private static List<string> OrderKeys(IEnumerable<PlanEntry> entries)
            => entries.Select(entry => $"{entry.Skill.Name}|{entry.Level}").ToList();

        private static List<string> StoredIdentity(Plan plan)
            => plan.Select(entry => $"{entry.Skill.Name}|{entry.Level}|{entry.Priority}").ToList();

        [Fact]
        public void GroupByPriority_ClipboardFileAndDisplayOrdersAllMatch()
        {
            Plan plan = CreatePrioritisedPlan(out _);
            plan.SortingPreferences.GroupByPriority = true;
            using var viewModel = new PlanEditorViewModel(new EventAggregator()) { Plan = plan };

            List<string> displayOrder = OrderKeys(viewModel.DisplayPlan!);
            List<string> clipboardOrder = OrderKeys(PlanGameTextExporter.Export(plan));
            List<string> fileOrder = OrderKeys(PlanIOHelper.ExportAsText(plan, NamesOnlySettings()));

            displayOrder.Should().Equal("CPU Management|1", "Spaceship Command|1", "Navigation|1", "Navigation|2");
            clipboardOrder.Should().Equal(displayOrder);
            fileOrder.Should().Equal(displayOrder);
        }

        [Fact]
        public void GroupByPriority_ClipboardTextIsOneSkillAndNumericLevelPerLine()
        {
            Plan plan = CreatePrioritisedPlan(out _);
            plan.SortingPreferences.GroupByPriority = true;

            string clipboard = PlanGameTextExporter.Export(plan);

            Lines(clipboard).Should().Equal("CPU Management 1", "Spaceship Command 1", "Navigation 1", "Navigation 2");
        }

        [Fact]
        public void Exporting_DoesNotChangeTheStoredOrder()
        {
            Plan plan = CreatePrioritisedPlan(out _);
            plan.SortingPreferences.GroupByPriority = true;
            List<string> storedBefore = StoredIdentity(plan);
            List<PlanEntry> instancesBefore = plan.ToList();

            PlanGameTextExporter.Export(plan);
            PlanIOHelper.ExportAsText(plan, NamesOnlySettings());

            StoredIdentity(plan).Should().Equal(storedBefore);
            plan.ToList().Should().Equal(instancesBefore);
            storedBefore.Should().Equal("Navigation|1|3", "Navigation|2|3", "CPU Management|1|1", "Spaceship Command|1|2");
        }

        [Theory]
        [InlineData(ThreeStateSortOrder.Ascending)]
        [InlineData(ThreeStateSortOrder.Descending)]
        public void NameSort_ClipboardAndFileFollowTheDisplayOrder(ThreeStateSortOrder order)
        {
            Plan plan = CreatePrioritisedPlan(out _);
            plan.SortingPreferences.Criteria = PlanEntrySort.Name;
            plan.SortingPreferences.Order = order;
            using var viewModel = new PlanEditorViewModel(new EventAggregator()) { Plan = plan };

            List<string> displayOrder = OrderKeys(viewModel.DisplayPlan!);

            OrderKeys(PlanGameTextExporter.Export(plan)).Should().Equal(displayOrder);
            OrderKeys(PlanIOHelper.ExportAsText(plan, NamesOnlySettings())).Should().Equal(displayOrder);
        }

        [Fact]
        public void SelectedEntriesExport_KeepsTheDisplayOrder()
        {
            Plan plan = CreatePrioritisedPlan(out CCPCharacter character);
            plan.SortingPreferences.GroupByPriority = true;
            using var viewModel = new PlanEditorViewModel(new EventAggregator())
            {
                Character = character,
                Plan = plan,
            };
            PlanEntry[] selection =
            {
                plan.GetEntry(PlanTestFixture.GetSkill("Navigation"), 1)!,
                plan.GetEntry(PlanTestFixture.GetSkill("CPU Management"), 1)!,
                plan.GetEntry(PlanTestFixture.GetSkill("Spaceship Command"), 1)!,
            };

            string exported = viewModel.ExportSelectedAsText(selection, NamesOnlySettings());

            OrderKeys(exported).Should().Equal("CPU Management|1", "Spaceship Command|1", "Navigation|1");
            StoredIdentity(plan).Should().Equal("Navigation|1|3", "Navigation|2|3", "CPU Management|1|1",
                "Spaceship Command|1|2");
        }

        [Fact]
        public void NoSorting_ExportsInTheStoredOrder()
        {
            Plan plan = CreatePrioritisedPlan(out _);
            List<string> storedOrder = OrderKeys(plan);

            OrderKeys(PlanGameTextExporter.Export(plan)).Should().Equal(storedOrder);
            OrderKeys(PlanIOHelper.ExportAsText(plan, NamesOnlySettings())).Should().Equal(storedOrder);
            storedOrder.Should().Equal("Navigation|1", "Navigation|2", "CPU Management|1", "Spaceship Command|1");
        }

        [Fact]
        public void ExportingTwice_GivesTheSameText()
        {
            Plan plan = CreatePrioritisedPlan(out _);
            plan.SortingPreferences.GroupByPriority = true;

            string first = PlanGameTextExporter.Export(plan);
            string second = PlanGameTextExporter.Export(plan);

            second.Should().Be(first);
        }

        [Fact]
        public void ClipboardCopyCallSites_UseTheSharedExporter()
        {
            string? avaloniaDir = FindProjectDirectory("src/EveLens.Avalonia");
            if (avaloniaDir == null)
                return;

            string[] copyPaths =
            {
                Path.Combine(avaloniaDir, "Views", "PlanEditor", "PlanUnifiedView.axaml.cs"),
                Path.Combine(avaloniaDir, "Views", "PlanEditor", "PlanEditorWindow.axaml.cs"),
            };
            foreach (string path in copyPaths)
            {
                File.Exists(path).Should().BeTrue($"{path} should exist");
                string content = File.ReadAllText(path);
                content.Should().Contain("_viewModel.ExportAsGameText()");
                content.Should().NotContain("{entry.Skill.Name} {entry.Level}");
            }
        }

        private static string? FindProjectDirectory(string relativePath)
        {
            string? dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            for (int i = 0; i < 8 && dir != null; i++)
            {
                string candidate = Path.Combine(dir, relativePath);
                if (Directory.Exists(candidate))
                    return candidate;
                dir = Path.GetDirectoryName(dir);
            }
            return null;
        }
    }
}
