// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for Issue #80 — "when deleting skill in plan editor it deletes the top skill".
    ///
    /// The plan editor's <c>DeleteSelected()</c> previously ignored the queue selection and always
    /// removed <c>_currentEntryItems.FirstOrDefault()</c> (the top row). The fix routes deletion
    /// through the actual selected entries and into <see cref="Plan.TryRemoveSet{T}"/>.
    ///
    /// These tests pin the model-level contract the fix depends on: removing a *specific* entry
    /// removes that entry, leaves unrelated entries intact, and only cascades to genuine dependents.
    /// </summary>
    [Collection("StaticData")]
    public class PlanDeleteSelectionTests
    {
        public PlanDeleteSelectionTests()
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
        }

        [Fact]
        public void TryRemoveSet_RemovesSelectedEntry_NotTheFirstEntry()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            // Two independent skills so neither is a prerequisite of the other.
            var first = PlanTestFixture.GetSkill("Spaceship Command");
            var second = PlanTestFixture.GetSkill("Science");
            plan.PlanTo(first, 1);
            plan.PlanTo(second, 1);

            // Select the SECOND entry (the one a user clicked), not the top row.
            var target = plan.First(e => e.Skill == second && e.Level == 1);
            var op = plan.TryRemoveSet(new[] { target });
            op.Perform();

            // The selected entry is gone; the first/top entry survives. (The pre-fix bug deleted the top.)
            plan.Any(e => e.Skill == second).Should().BeFalse("the selected entry must be removed");
            plan.Any(e => e.Skill == first && e.Level == 1).Should().BeTrue(
                "the unselected top entry must NOT be removed (this was the Issue #80 bug)");
        }

        [Fact]
        public void TryRemoveSet_RemovingHigherLevel_KeepsLowerLevels()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            var skill = PlanTestFixture.GetSkill("Spaceship Command");
            plan.PlanTo(skill, 3); // adds levels 1, 2, 3

            // Delete only level 3.
            var levelThree = plan.First(e => e.Skill == skill && e.Level == 3);
            plan.TryRemoveSet(new[] { levelThree }).Perform();

            plan.Any(e => e.Skill == skill && e.Level == 3).Should().BeFalse();
            plan.Any(e => e.Skill == skill && e.Level == 1).Should().BeTrue("lower levels are not dependents of L3 removal");
            plan.Any(e => e.Skill == skill && e.Level == 2).Should().BeTrue();
        }

        [Fact]
        public void TryRemoveSet_MultipleSelectedEntries_RemovesAllOfThem()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            var a = PlanTestFixture.GetSkill("Spaceship Command");
            var b = PlanTestFixture.GetSkill("Science");
            plan.PlanTo(a, 1);
            plan.PlanTo(b, 1);

            // Multi-select delete (the fix passes the whole selected set in one cascade).
            var entries = plan.Where(e => (e.Skill == a || e.Skill == b) && e.Level == 1).ToList();
            entries.Should().HaveCount(2);

            plan.TryRemoveSet(entries).Perform();

            plan.Any(e => e.Skill == a).Should().BeFalse();
            plan.Any(e => e.Skill == b).Should().BeFalse();
        }
    }
}
