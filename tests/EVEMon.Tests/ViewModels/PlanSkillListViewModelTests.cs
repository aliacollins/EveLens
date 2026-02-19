using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanSkillListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_EmptyLists()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.TrainingEntries.Should().BeEmpty();
            vm.MissingEntries.Should().BeEmpty();
            vm.TrainedEntries.Should().BeEmpty();
            vm.TrainingCount.Should().Be(0);
            vm.MissingCount.Should().Be(0);
            vm.TrainedCount.Should().Be(0);
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_RaisesPropertyChanged()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanSkillListViewModel.TextFilter))
                    changed = e.PropertyName;
            };
            vm.TextFilter = "test";
            changed.Should().Be("TextFilter");
            vm.Dispose();
        }

        [Fact]
        public void Refresh_NoPlanEditor_DoesNotThrow()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithPlanEditorButNoPlan_DoesNotThrow()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.PlanEditor = planEditor;
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void TextFilter_DefaultEmpty()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.TextFilter.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void PlanEditor_CanBeSet()
        {
            var planEditor = new PlanEditorViewModel(CreateAggregator());
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.PlanEditor = planEditor;
            vm.PlanEditor.Should().BeSameAs(planEditor);
            vm.Dispose();
            planEditor.Dispose();
        }

        [Fact]
        public void TrainingTimeTotal_DefaultZero()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.TrainingTimeTotal.Should().Be(System.TimeSpan.Zero);
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanSkillListViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
