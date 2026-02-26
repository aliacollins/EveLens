// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.ViewModels;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class PlanTimeCardViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_ZeroTimes()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.TotalTrainingTime.Should().Be(TimeSpan.Zero);
            vm.OptimalTrainingTime.Should().Be(TimeSpan.Zero);
            vm.TimeSaved.Should().Be(TimeSpan.Zero);
            vm.HasOptimization.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void SetTrainingTime_EqualOptimal_NoSavings()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            var totalTime = TimeSpan.FromHours(48);
            vm.TotalTrainingTime = totalTime;
            vm.OptimalTrainingTime = totalTime;
            vm.TotalTrainingTime.Should().Be(totalTime);
            vm.TimeSaved.Should().Be(TimeSpan.Zero);
            vm.HasOptimization.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void OptimalLessThanTotal_ShowsSavings()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            var totalTime = TimeSpan.FromHours(48);
            var optimalTime = TimeSpan.FromHours(36);
            vm.TotalTrainingTime = totalTime;
            vm.OptimalTrainingTime = optimalTime;
            vm.TimeSaved.Should().Be(TimeSpan.FromHours(12));
            vm.HasOptimization.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void TotalTrainingTimeText_FormattedCorrectly()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.TotalTrainingTime = TimeSpan.FromDays(3)
                .Add(TimeSpan.FromHours(12))
                .Add(TimeSpan.FromMinutes(30));
            vm.TotalTrainingTimeText.Should().Contain("3d").And.Contain("12h").And.Contain("30m");
            vm.Dispose();
        }

        [Fact]
        public void ZeroTime_TextShowsDone()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.TotalTrainingTime = TimeSpan.Zero;
            vm.TotalTrainingTimeText.Should().Be("Done");
            vm.Dispose();
        }

        [Fact]
        public void TimeSavedText_EmptyWhenNoSavings()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.TotalTrainingTime = TimeSpan.FromHours(10);
            vm.OptimalTrainingTime = TimeSpan.FromHours(10);
            vm.TimeSavedText.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void TimeSavedText_NonEmptyWhenSavings()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.TotalTrainingTime = TimeSpan.FromHours(10);
            vm.OptimalTrainingTime = TimeSpan.FromHours(5);
            vm.TimeSavedText.Should().NotBeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void FormatTrainingTime_HoursAndMinutes()
        {
            var result = PlanTimeCardViewModel.FormatTrainingTime(
                TimeSpan.FromHours(5).Add(TimeSpan.FromMinutes(30)));
            result.Should().Contain("5h").And.Contain("30m");
        }

        [Fact]
        public void FormatTrainingTime_MinutesOnly()
        {
            var result = PlanTimeCardViewModel.FormatTrainingTime(TimeSpan.FromMinutes(45));
            result.Should().Contain("45m");
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanTimeCardViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
