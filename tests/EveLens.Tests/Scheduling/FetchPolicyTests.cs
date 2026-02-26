// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Scheduling;
using EveLens.Core.Enumerations;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Scheduling
{
    public class FetchPolicyTests
    {
        [Fact]
        public void GetPriority_VisibleMonitored_ReturnsActive()
        {
            FetchPolicy.GetPriority(isVisible: true, isMonitored: true)
                .Should().Be(FetchPriority.Active);
        }

        [Fact]
        public void GetPriority_NotVisibleMonitored_ReturnsBackground()
        {
            FetchPolicy.GetPriority(isVisible: false, isMonitored: true)
                .Should().Be(FetchPriority.Background);
        }

        [Fact]
        public void GetPriority_NotMonitored_ReturnsOff()
        {
            FetchPolicy.GetPriority(isVisible: true, isMonitored: false)
                .Should().Be(FetchPriority.Off);

            FetchPolicy.GetPriority(isVisible: false, isMonitored: false)
                .Should().Be(FetchPriority.Off);
        }

        [Fact]
        public void GetJitter_ActivePriority_IsSmall()
        {
            // Run multiple times since jitter is random
            for (int i = 0; i < 20; i++)
            {
                var jitter = FetchPolicy.GetJitter(FetchPriority.Active);
                jitter.TotalMilliseconds.Should().BeInRange(100, 500);
            }
        }

        [Fact]
        public void GetJitter_BackgroundPriority_IsLarger()
        {
            for (int i = 0; i < 20; i++)
            {
                var jitter = FetchPolicy.GetJitter(FetchPriority.Background);
                jitter.TotalMilliseconds.Should().BeInRange(500, 3000);
            }
        }

        [Theory]
        [InlineData(0)] // CharacterSheet
        [InlineData(1)] // Skills
        [InlineData(2)] // SkillQueue
        public void GetColdStartPhase_CoreEndpoints_ReturnsPhase1(int method)
        {
            FetchPolicy.GetColdStartPhase(method).Should().Be(1);
        }

        [Theory]
        [InlineData(5)]
        [InlineData(10)]
        [InlineData(30)]
        public void GetColdStartPhase_OtherEndpoints_ReturnsPhase4(int method)
        {
            FetchPolicy.GetColdStartPhase(method).Should().Be(4);
        }
    }
}
