// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Interfaces;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    /// <summary>
    /// Priority-change guarantees for the plan editor. Priority became a first-class
    /// editor feature (visible chips, priority bands) after Issue #135 showed the old
    /// silent-cycle handler read as broken.
    /// </summary>
    [Collection("StaticData")]
    public class PlanEditorPriorityTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        private static (PlanEditorViewModel vm, Plan plan) CreateEditor()
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
                plan.PlanTo(skill, 2);

            var vm = new PlanEditorViewModel(CreateAggregator()) { Plan = plan };
            return (vm, plan);
        }

        [Fact]
        public void TrySetPriority_SetsPriority_OnIndependentSkill()
        {
            var (vm, plan) = CreateEditor();

            var entry = plan.First();
            vm.TrySetPriority(new[] { entry }, 1).Should().BeTrue();

            entry.Priority.Should().Be(1);
            vm.Dispose();
        }

        /// <summary>
        /// The WinForms-era convention passed the SORTED display plan into the model's
        /// priority API, which rebuilds the plan from whatever it is handed — silently
        /// persisting a transient sort into the manual order. The VM must hand over the
        /// plan's own order instead.
        /// </summary>
        [Fact]
        public void TrySetPriority_WithActiveSort_DoesNotPersistDisplayOrderIntoPlan()
        {
            var (vm, plan) = CreateEditor();
            var manualOrder = plan.Select(e => $"{e.Skill.Name} {e.Level}").ToList();

            // Activate a sort that changes the display order
            plan.SortingPreferences.Criteria = PlanEntrySort.Name;
            plan.SortingPreferences.Order = ThreeStateSortOrder.Descending;
            vm.UpdateDisplayPlan();

            vm.TrySetPriority(new[] { plan.First() }, 2).Should().BeTrue();

            plan.Select(e => $"{e.Skill.Name} {e.Level}").Should().Equal(manualOrder,
                "changing a priority must never rewrite the plan's manual order");
            vm.Dispose();
        }

        /// <summary>
        /// Consistency law: a prerequisite (here, level I under level II of the same
        /// skill) may never sit at lower priority than what depends on it. The plain
        /// set must refuse and roll back; the fixing set must pull the prerequisite along.
        /// </summary>
        [Fact]
        public void TrySetPriority_PrerequisiteConflict_RefusesAndRollsBack()
        {
            var (vm, plan) = CreateEditor();

            var levelOne = plan.First(e => e.Level == 1);
            var levelTwo = plan.First(e => e.Skill == levelOne.Skill && e.Level == 2);

            // Raising only level II to top importance would leave its own level I behind
            vm.TrySetPriority(new[] { levelTwo }, 1).Should().BeFalse();
            levelTwo.Priority.Should().Be(3, "a refused change must roll back");

            vm.SetPriority(new[] { levelTwo }, 1);
            levelTwo.Priority.Should().Be(1);
            levelOne.Priority.Should().Be(1, "the fixing variant pulls prerequisites along");
            vm.Dispose();
        }
    }
}
