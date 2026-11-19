// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Helpers;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Helpers
{
    public class PlanScheduleTests
    {
        [Fact]
        public void CompletionDate_IsTheStartPlusTheTrainingTime()
        {
            var start = new DateTime(2026, 11, 19, 23, 30, 0, DateTimeKind.Utc);

            DateTime completion = PlanSchedule.CompletionDate(start, TimeSpan.FromHours(1));

            completion.Should().Be(new DateTime(2026, 11, 20, 0, 30, 0, DateTimeKind.Utc));
        }

        [Theory]
        [InlineData(DateTimeKind.Utc)]
        [InlineData(DateTimeKind.Unspecified)]
        public void CompletionDate_IsAlwaysUtc(DateTimeKind kind)
        {
            var start = new DateTime(2026, 11, 19, 12, 0, 0, kind);

            PlanSchedule.CompletionDate(start, TimeSpan.FromDays(2)).Kind.Should().Be(DateTimeKind.Utc);
        }

        [Fact]
        public void CompletionDate_WithNoTrainingLeft_IsTheStart()
        {
            var start = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);

            PlanSchedule.CompletionDate(start, TimeSpan.Zero).Should().Be(start);
        }
    }
}
