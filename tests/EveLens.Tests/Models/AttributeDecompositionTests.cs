// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Models
{
    public class AttributeDecompositionTests
    {
        [Fact]
        public void LegalAllocation_HasNoBonus()
        {
            var split = AttributeDecomposition.FromObserved(new long[] { 27, 21, 17, 17, 17 });

            split.IsSupported.Should().BeTrue();
            split.TemporaryBonus.Should().Be(0);
            split.Allocation.Should().Equal(27, 21, 17, 17, 17);
        }

        [Theory]
        [InlineData(1)]
        [InlineData(4)]
        [InlineData(12)]
        public void UniformBonus_IsSeparatedFromTheAllocation(long bonus)
        {
            var split = AttributeDecomposition.FromObserved(
                new[] { 20 + bonus, 20 + bonus, 19 + bonus, 20 + bonus, 20 + bonus });

            split.IsSupported.Should().BeTrue();
            split.TemporaryBonus.Should().Be(bonus);
            split.Allocation.Should().Equal(20, 20, 19, 20, 20);
        }

        [Theory]
        [InlineData(new long[] { 27, 21, 17, 17, 18 })]
        [InlineData(new long[] { 27, 21, 17, 17, 16 })]
        [InlineData(new long[] { 32, 21, 17, 17, 17 })]
        [InlineData(new long[] { 20, 20, 20, 20, 14 })]
        [InlineData(new long[] { 0, 0, 0, 0, 0 })]
        [InlineData(new long[] { -1, 25, 25, 25, 25 })]
        public void ValuesThatAreNotAnAllocationPlusUniformBonus_AreReturnedUnchanged(long[] values)
        {
            var split = AttributeDecomposition.FromObserved(values);

            split.IsSupported.Should().BeFalse();
            split.TemporaryBonus.Should().Be(0);
            split.Allocation.Should().Equal(values);
        }

        [Fact]
        public void HugeValues_DoNotOverflow()
        {
            var values = new[] { long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue, long.MaxValue };

            var split = AttributeDecomposition.FromObserved(values);

            split.IsSupported.Should().BeFalse();
            split.Allocation.Should().Equal(values);
        }

        [Fact]
        public void WrongAttributeCount_Throws()
        {
            Action act = () => AttributeDecomposition.FromObserved(new long[] { 20, 20, 20, 20 });

            act.Should().Throw<ArgumentException>();
        }
    }
}
