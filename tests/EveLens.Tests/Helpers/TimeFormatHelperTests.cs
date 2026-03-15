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
    public class TimeFormatHelperTests
    {
        [Fact]
        public void FormatRemaining_MultiDay_ShowsDaysAndHours()
        {
            var ts = new TimeSpan(3, 14, 22, 0);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("3d 14h");
        }

        [Fact]
        public void FormatRemaining_OneDay_ShowsDaysAndHours()
        {
            var ts = new TimeSpan(1, 0, 30, 0);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("1d 0h");
        }

        [Fact]
        public void FormatRemaining_HoursOnly_ShowsHoursAndMinutes()
        {
            var ts = new TimeSpan(0, 5, 42, 0);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("5h 42m");
        }

        [Fact]
        public void FormatRemaining_MinutesOnly_ShowsMinutesAndSeconds()
        {
            var ts = new TimeSpan(0, 0, 23, 45);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("23m 45s");
        }

        [Fact]
        public void FormatRemaining_Zero_ReturnsDone()
        {
            TimeFormatHelper.FormatRemaining(TimeSpan.Zero).Should().Be("Done");
        }

        [Fact]
        public void FormatRemaining_Negative_ReturnsDone()
        {
            TimeFormatHelper.FormatRemaining(TimeSpan.FromSeconds(-5)).Should().Be("Done");
        }

        [Fact]
        public void FormatRemaining_JustUnderOneDay_ShowsHoursAndMinutes()
        {
            var ts = new TimeSpan(0, 23, 59, 59);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("23h 59m");
        }

        [Fact]
        public void FormatRemaining_JustUnderOneHour_ShowsMinutesAndSeconds()
        {
            var ts = new TimeSpan(0, 0, 59, 30);
            TimeFormatHelper.FormatRemaining(ts).Should().Be("59m 30s");
        }

        [Fact]
        public void FormatRemaining_ExactlyOneHour_ShowsHoursAndMinutes()
        {
            TimeFormatHelper.FormatRemaining(TimeSpan.FromHours(1)).Should().Be("1h 0m");
        }

        [Fact]
        public void FormatRemaining_ExactlyOneDay_ShowsDaysAndHours()
        {
            TimeFormatHelper.FormatRemaining(TimeSpan.FromDays(1)).Should().Be("1d 0h");
        }
    }
}
