// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Models;
using EveLens.Common.SettingsObjects;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Order guarantees for the plan sort pass. The plan editor runs this pass on every
    /// mutation, so "no sort selected" must mean "don't move anything".
    /// </summary>
    [Collection("StaticData")]
    public class PlanSortOrderTests
    {
        /// <summary>
        /// Issue #136 — adding a skill rearranged the whole plan. The prerequisite-order pass
        /// clustered same-primary-attribute skills unconditionally, so it reordered plans that
        /// had no sort active at all.
        /// </summary>
        [Fact]
        public void Sort_NoCriteria_LeavesManualOrderAlone()
        {
            var plan = BuildInterleavedPlan(out List<string> expectedOrder);

            plan.Sort(new PlanSorting());

            Describe(plan).Should().Equal(expectedOrder);
        }

        /// <summary>
        /// The same pass must still keep the manual order when a plan is only grouped by
        /// priority — grouping is a partition, not a licence to shuffle within a group.
        /// </summary>
        [Fact]
        public void Sort_GroupByPriorityOnly_LeavesManualOrderAlone()
        {
            var plan = BuildInterleavedPlan(out List<string> expectedOrder);

            plan.Sort(new PlanSorting { GroupByPriority = true });

            Describe(plan).Should().Equal(expectedOrder);
        }

        /// <summary>
        /// Group by Attr is where clustering belongs: with the attribute sort active, entries
        /// sharing a primary attribute must come out contiguous.
        /// </summary>
        [Fact]
        public void Sort_ByPrimaryAttribute_ClustersAttributes()
        {
            var plan = BuildInterleavedPlan(out _);

            plan.Sort(new PlanSorting
            {
                Criteria = PlanEntrySort.PrimaryAttribute,
                Order = ThreeStateSortOrder.Ascending,
            });

            var attributes = plan.Select(entry => entry.Skill.PrimaryAttribute).ToList();
            attributes.Distinct().Count().Should().Be(2, "the fixture plans two attribute groups");
            CountRuns(attributes).Should().Be(2, "each attribute should appear as one unbroken run");
        }

        /// <summary>
        /// Builds a plan whose entries alternate between two primary attributes, using
        /// prerequisite-free skills so nothing but the sort can justify a move.
        /// </summary>
        private static Plan BuildInterleavedPlan(out List<string> order)
        {
            PlanTestFixture.EnsureStaticSkillsLoaded();
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character);

            var byAttribute = StaticSkills.AllSkills
                .Where(skill => skill.IsPublic && !skill.Prerequisites.Any())
                .GroupBy(skill => skill.PrimaryAttribute)
                .Where(group => group.Count() >= 2)
                .OrderBy(group => (int)group.Key)
                .Take(2)
                .Select(group => group.OrderBy(skill => skill.Name).Take(2).ToList())
                .ToList();

            byAttribute.Should().HaveCount(2, "test needs two attribute groups of standalone skills");

            // Alternate the groups so any clustering shows up as a reorder
            for (int i = 0; i < 2; i++)
            {
                foreach (var group in byAttribute)
                    plan.PlanTo(group[i], 1);
            }

            plan.Should().HaveCount(4);
            order = Describe(plan);
            return plan;
        }

        private static List<string> Describe(Plan plan)
            => plan.Select(entry => $"{entry.Skill.Name} {entry.Level}").ToList();

        private static int CountRuns<T>(IReadOnlyList<T> values)
        {
            int runs = 0;
            for (int i = 0; i < values.Count; i++)
            {
                if (i == 0 || !Equals(values[i], values[i - 1]))
                    runs++;
            }
            return runs;
        }
    }
}
