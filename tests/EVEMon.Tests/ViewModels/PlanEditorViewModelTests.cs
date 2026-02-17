using System.ComponentModel;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Services;
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
        public void DefaultState_SortOrderNone()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.SortOrder.Should().Be(ThreeStateSortOrder.None);
            vm.Dispose();
        }

        [Fact]
        public void SortCriteria_RaisesPropertyChanged()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEditorViewModel.SortCriteria))
                    changedProp = e.PropertyName;
            };

            vm.SortCriteria = PlanEntrySort.Cost;

            changedProp.Should().Be("SortCriteria");
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

            vm.Plan = null; // Setting to same value -- should not raise
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

        [Fact]
        public void HasSelection_DefaultFalse()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.HasSelection.Should().BeFalse();
            vm.HasSingleSelection.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void DisplayPlan_NullWhenNoPlan()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.DisplayPlan.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void ContainsObsoleteEntries_FalseWhenNoPlan()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.ContainsObsoleteEntries.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ContainsInvalidEntries_FalseWhenNoPlan()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.ContainsInvalidEntries.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void GroupByPriority_DefaultFalse()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.GroupByPriority.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void EntryCount_DefaultZero()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.EntryCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void CanMoveUp_EmptySelection_ReturnsFalse()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.CanMoveUp(new int[0]).Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void CanMoveDown_EmptySelection_ReturnsFalse()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            vm.CanMoveDown(new int[0]).Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ToggleSortColumn_NoPlan_DoesNotThrow()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            var act = () => vm.ToggleSortColumn(PlanEntrySort.Name);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void UpdateDisplayPlan_NoPlan_DoesNotThrow()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            var act = () => vm.UpdateDisplayPlan();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void UpdateStatistics_NoPlan_DoesNotThrow()
        {
            var vm = new PlanEditorViewModel(CreateAggregator());
            var act = () => vm.UpdateStatistics();
            act.Should().NotThrow();
            vm.Dispose();
        }
    }
}
