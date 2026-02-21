// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Scheduling;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Scheduling
{
    public class TokenTrackerTests
    {
        private readonly TokenTracker _tracker = new();

        [Fact]
        public void CanFetch_UnknownCharacter_ReturnsTrue()
        {
            _tracker.CanFetch(999L, "group1").Should().BeTrue();
        }

        [Fact]
        public void CanFetch_WithBudget_ReturnsTrue()
        {
            _tracker.Update(1L, "group1", remaining: 100, limit: 150);

            _tracker.CanFetch(1L, "group1").Should().BeTrue();
        }

        [Fact]
        public void CanFetch_Exhausted_ReturnsFalse()
        {
            _tracker.Update(1L, "group1", remaining: 10, limit: 150);

            _tracker.CanFetch(1L, "group1").Should().BeFalse();
        }

        [Fact]
        public void Update_CreatesNewBucket_IfNotExists()
        {
            _tracker.Update(1L, "group1", remaining: 80, limit: 150);

            _tracker.CanFetch(1L, "group1").Should().BeTrue();
        }

        [Fact]
        public void IndependentBuckets_DontInterfere()
        {
            _tracker.Update(1L, "group1", remaining: 5, limit: 150);
            _tracker.Update(2L, "group1", remaining: 100, limit: 150);

            _tracker.CanFetch(1L, "group1").Should().BeFalse();
            _tracker.CanFetch(2L, "group1").Should().BeTrue();
        }

        [Fact]
        public void RemoveCharacter_ClearsAllBuckets()
        {
            _tracker.Update(1L, "group1", remaining: 100, limit: 150);
            _tracker.Update(1L, "group2", remaining: 100, limit: 150);

            _tracker.RemoveCharacter(1L);

            // After removal, unknown character returns true (optimistic default)
            _tracker.CanFetch(1L, "group1").Should().BeTrue();
            _tracker.CanFetch(1L, "group2").Should().BeTrue();
        }

        [Fact]
        public void NullRateGroup_UsesDefaultGroup()
        {
            _tracker.Update(1L, null, remaining: 5, limit: 150);

            _tracker.CanFetch(1L, null).Should().BeFalse();
        }
    }
}
