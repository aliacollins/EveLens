// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EVEMon.Common.Scheduling;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.Scheduling
{
    public class CharacterAuthStateTests
    {
        [Fact]
        public void DefaultStatus_IsHealthy()
        {
            var state = new CharacterAuthState();

            state.Status.Should().Be(AuthStatus.Healthy);
            state.ConsecutiveFailures.Should().Be(0);
        }

        [Fact]
        public void MarkFailed_SetsAuthFailed_IncrementsCount()
        {
            var state = new CharacterAuthState();

            state.MarkFailed();
            state.Status.Should().Be(AuthStatus.AuthFailed);
            state.ConsecutiveFailures.Should().Be(1);

            state.MarkFailed();
            state.ConsecutiveFailures.Should().Be(2);
        }

        [Fact]
        public void MarkHealthy_ClearsFailure_ResetsCount()
        {
            var state = new CharacterAuthState();
            state.MarkFailed();
            state.MarkFailed();

            state.MarkHealthy();

            state.Status.Should().Be(AuthStatus.Healthy);
            state.ConsecutiveFailures.Should().Be(0);
        }
    }
}
