// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations;
using EveLens.Common.ViewModels;
using EveLens.Common.Services;
using EveLens.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.ViewModels
{
    public class PlanOptimizerViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_NoResults()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.HasResults.Should().BeFalse();
            vm.IsCalculating.Should().BeFalse();
            vm.TimeSaved.Should().Be(TimeSpan.Zero);
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void RunOptimization_WithNullPlan_DoesNotThrow()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            var act = () => vm.RunOptimization(null, null);
            act.Should().NotThrow();
            vm.HasResults.Should().BeFalse();
            vm.IsCalculating.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void GetCurrent_DefaultsToZero()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.GetCurrent(EveAttribute.Intelligence).Should().Be(0);
            vm.GetCurrent(EveAttribute.Perception).Should().Be(0);
            vm.GetCurrent(EveAttribute.Charisma).Should().Be(0);
            vm.GetCurrent(EveAttribute.Willpower).Should().Be(0);
            vm.GetCurrent(EveAttribute.Memory).Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void GetOptimal_DefaultsToZero()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.GetOptimal(EveAttribute.Intelligence).Should().Be(0);
            vm.GetOptimal(EveAttribute.Memory).Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void TimeSavedText_WithNoResults_IsEmpty()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.TimeSavedText.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void CurrentDurationText_Default_IsDash()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.CurrentDurationText.Should().Be("\u2014");
            vm.Dispose();
        }

        [Fact]
        public void OptimalDurationText_Default_IsDash()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            vm.OptimalDurationText.Should().Be("\u2014");
            vm.Dispose();
        }

        [Fact]
        public void TimeSaved_RaisesPropertyChanged()
        {
            var vm = new PlanOptimizerViewModel(CreateAggregator());
            bool timeSavedChanged = false;
            bool timeSavedTextChanged = false;
            vm.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(PlanOptimizerViewModel.TimeSaved)) timeSavedChanged = true;
                if (e.PropertyName == nameof(PlanOptimizerViewModel.TimeSavedText)) timeSavedTextChanged = true;
            };

            vm.TimeSaved = TimeSpan.FromHours(5);

            timeSavedChanged.Should().BeTrue();
            timeSavedTextChanged.Should().BeTrue();
            vm.TimeSavedText.Should().Be("5h 0m");
            vm.Dispose();
        }
    }
}
