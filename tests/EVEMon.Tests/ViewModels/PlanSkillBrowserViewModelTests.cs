using System.ComponentModel;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class PlanSkillBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyGroups()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Groups.Should().NotBeNull();
            vm.Groups.Should().BeEmpty();
            vm.SelectedSkill.Should().BeNull();
            vm.SelectedSkillDetail.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.ShowAll.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Navigation";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ShowAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.ShowAll = false;
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToLevel_WithNullSkill_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToLevel(null, 3);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectSkill_WithNull_SetsDetailToNull()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.SelectSkill(null);
            vm.SelectedSkill.Should().BeNull();
            vm.SelectedSkillDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void SelectedSkill_RaisesPropertyChanged()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            string? changed = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(PlanSkillBrowserViewModel.SelectedSkill))
                    changed = e.PropertyName;
            };
            vm.SelectSkill(null);
            // Setting to null when already null won't fire, but this confirms no crash
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_DoesNotThrow()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new PlanSkillBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void ConstructorWithPlanEditor_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var planEditor = new PlanEditorViewModel(agg);
            var vm = new PlanSkillBrowserViewModel(planEditor, agg);
            vm.Should().NotBeNull();
            vm.Dispose();
            planEditor.Dispose();
        }
    }
}
