using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanEntryDetailViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_NoSelection()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.HasSelection.Should().BeFalse();
            vm.SelectedEntry.Should().BeNull();
            vm.SkillName.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_AllStringsEmpty()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.SkillDescription.Should().BeEmpty();
            vm.PrimaryAttribute.Should().BeEmpty();
            vm.SecondaryAttribute.Should().BeEmpty();
            vm.TrainingTime.Should().BeEmpty();
            vm.SkillPointsRequired.Should().BeEmpty();
            vm.SpPerHour.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_EmptyPrerequisites()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.Prerequisites.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void SelectedEntry_Null_ClearsSelection()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.SelectedEntry = null;
            vm.HasSelection.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void SelectedEntry_NullToNull_DoesNotRaisePropertyChanged()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanEntryDetailViewModel.SelectedEntry))
                    changed = e.PropertyName;
            };
            vm.SelectedEntry = null; // Setting to same value -- should not raise
            changed.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void HasSelection_FalseByDefault()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.HasSelection.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanEntryDetailViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
