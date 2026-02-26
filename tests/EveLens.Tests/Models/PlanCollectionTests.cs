// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Common.Models;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    /// <summary>
    /// Tests for <see cref="PlanCollection.ContainsName"/> and
    /// <see cref="PlanCollection.GetUniqueName"/> duplicate name handling.
    /// </summary>
    [Collection("StaticData")]
    public class PlanCollectionTests
    {
        [Fact]
        public void ContainsName_ReturnsTrueForExistingName()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character, "Plan A");
            character.Plans.Add(plan);

            character.Plans.ContainsName("Plan A").Should().BeTrue();
        }

        [Fact]
        public void ContainsName_IsCaseInsensitive()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character, "Plan A");
            character.Plans.Add(plan);

            character.Plans.ContainsName("plan a").Should().BeTrue();
            character.Plans.ContainsName("PLAN A").Should().BeTrue();
        }

        [Fact]
        public void ContainsName_WithExclude_IgnoresExcludedPlan()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character, "Plan A");
            character.Plans.Add(plan);

            character.Plans.ContainsName("Plan A", plan).Should().BeFalse();
        }

        [Fact]
        public void ContainsName_ReturnsFalseForNewName()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            var plan = PlanTestFixture.CreateTestPlan(character, "Plan A");
            character.Plans.Add(plan);

            character.Plans.ContainsName("Novel Name").Should().BeFalse();
        }

        [Fact]
        public void GetUniqueName_ReturnsOriginalWhenUnique()
        {
            var character = PlanTestFixture.CreateTestCharacter();

            character.Plans.GetUniqueName("Novel").Should().Be("Novel");
        }

        [Fact]
        public void GetUniqueName_AppendsSuffixForDuplicate()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.Plans.Add(PlanTestFixture.CreateTestPlan(character, "Plan A"));

            character.Plans.GetUniqueName("Plan A").Should().Be("Plan A (2)");
        }

        [Fact]
        public void GetUniqueName_IncrementsUntilUnique()
        {
            var character = PlanTestFixture.CreateTestCharacter();
            character.Plans.Add(PlanTestFixture.CreateTestPlan(character, "Plan A"));
            character.Plans.Add(PlanTestFixture.CreateTestPlan(character, "Plan A (2)"));

            character.Plans.GetUniqueName("Plan A").Should().Be("Plan A (3)");
        }
    }
}
