// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanDashboardViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanDashboardViewModel(planEditor, CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void DefaultState_EmptyValues()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanDashboardViewModel(planEditor, CreateAggregator());
            vm.GoalName.Should().BeEmpty();
            vm.SkillsTrained.Should().Be(0);
            vm.SkillsMissing.Should().Be(0);
            vm.TotalSkills.Should().Be(0);
            vm.TotalTime.Should().Be(TimeSpan.Zero);
            vm.BooksCost.Should().Be(0);
            vm.NotKnownBooksCost.Should().Be(0);
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void Refresh_NoPlan_DoesNotThrow()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanDashboardViewModel(planEditor, CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void Refresh_NoPlan_LeavesDefaultValues()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanDashboardViewModel(planEditor, CreateAggregator());
            vm.Refresh();
            vm.GoalName.Should().BeEmpty();
            vm.SkillsTrained.Should().Be(0);
            vm.SkillsMissing.Should().Be(0);
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void Constructor_NullPlanEditor_Throws()
        {
            var act = () => new PlanDashboardViewModel(null!, CreateAggregator());
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanDashboardViewModel(planEditor, CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
            planEditor.Dispose();
        }
    }
}
