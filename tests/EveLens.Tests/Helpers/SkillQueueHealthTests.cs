// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    [Collection("StaticData")]
    public class SkillQueueHealthTests
    {
        [Theory]
        [InlineData(-5.0, QueueHealth.Empty)]
        [InlineData(0.0, QueueHealth.Empty)]
        [InlineData(0.01, QueueHealth.Critical)]
        [InlineData(23.99, QueueHealth.Critical)]
        [InlineData(24.0, QueueHealth.Warning)]
        [InlineData(119.99, QueueHealth.Warning)]
        [InlineData(120.0, QueueHealth.Healthy)]
        [InlineData(2400.0, QueueHealth.Healthy)]
        public void TrainingQueue_IsGradedByTheHoursItHasLeft(double hoursLeft, QueueHealth expected)
        {
            SkillQueueHealth.Evaluate(true, TimeSpan.FromHours(hoursLeft)).Should().Be(expected);
        }

        [Theory]
        [InlineData(-5.0)]
        [InlineData(1.0)]
        [InlineData(2400.0)]
        public void QueueThatIsNotTraining_IsNotTraining_WhateverTimeIsLeft(double hoursLeft)
        {
            SkillQueueHealth.Evaluate(false, TimeSpan.FromHours(hoursLeft)).Should().Be(QueueHealth.NotTraining);
        }

        [Fact]
        public void CharacterWithoutAQueue_IsNotTraining()
        {
            var character = PlanTestFixture.CreateTestCharacter();

            character.QueueHealthAt(DateTime.UtcNow).Should().Be(QueueHealth.NotTraining);
        }
    }
}
