// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanEditorWindowViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultMode_IsPlan()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.SelectedMode.Should().Be(PlanEditorMode.Plan);
            vm.Dispose();
        }

        [Fact]
        public void SelectedMode_RaisesPropertyChanged()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEditorWindowViewModel.SelectedMode))
                    changed = e.PropertyName;
            };
            vm.SelectedMode = PlanEditorMode.Advanced;
            changed.Should().Be("SelectedMode");
            vm.Dispose();
        }

        [Fact]
        public void ChildViewModels_AreNotNull()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.PlanEditor.Should().NotBeNull();
            vm.Dashboard.Should().NotBeNull();
            vm.SkillList.Should().NotBeNull();
            vm.Optimizer.Should().NotBeNull();
            vm.Detail.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void Plan_DelegatesToPlanEditor()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.Plan.Should().BeNull();
            vm.PlanEditor.Plan.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void IsOptimizerVisible_DefaultFalse()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.IsOptimizerVisible.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void IsOptimizerVisible_RaisesPropertyChanged()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEditorWindowViewModel.IsOptimizerVisible))
                    changed = e.PropertyName;
            };
            vm.IsOptimizerVisible = true;
            changed.Should().Be("IsOptimizerVisible");
            vm.Dispose();
        }

        [Fact]
        public void CreatePlanFromShip_DoesNotThrow()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            var act = () => vm.CreatePlanFromShip(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void CreatePlanFromFitting_DoesNotThrow()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            var act = () => vm.CreatePlanFromFitting(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SkillList_HasPlanEditorSet()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.SkillList.PlanEditor.Should().BeSameAs(vm.PlanEditor);
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanEditorWindowViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
