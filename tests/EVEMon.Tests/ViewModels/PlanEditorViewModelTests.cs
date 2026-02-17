using System.ComponentModel;
using EVEMon.Common.Events;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanEditorViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_PlanIsNull()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.Plan.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SortAscendingTrue()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void SortColumn_RaisesPropertyChanged()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEditorViewModel.SortColumn))
                    changedProp = e.PropertyName;
            };

            vm.SortColumn = PlanColumn.Cost;

            changedProp.Should().Be("SortColumn");
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_SameColumn_ReversesDirection()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.SortColumn = PlanColumn.SkillName;
            vm.SortAscending = true;

            vm.ToggleSort(PlanColumn.SkillName);

            vm.SortAscending.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_DifferentColumn_SetsAscending()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.SortColumn = PlanColumn.SkillName;
            vm.SortAscending = false;

            vm.ToggleSort(PlanColumn.Cost);

            vm.SortColumn.Should().Be(PlanColumn.Cost);
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void Plan_RaisesPropertyChanged()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEditorViewModel.Plan))
                    changedProp = e.PropertyName;
            };

            vm.Plan = null; // Setting to same value — should not raise
            changedProp.Should().BeNull();

            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
