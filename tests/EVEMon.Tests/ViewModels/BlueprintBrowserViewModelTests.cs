using EVEMon.Common.Services;
using EVEMon.Common.ViewModels;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class BlueprintBrowserViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_HasEmptyGroups()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.Groups.Should().NotBeNull();
            vm.SelectedBlueprint.Should().BeNull();
            vm.TextFilter.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void TextFilter_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.TextFilter = "Rifter";
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Refresh_WithNullCharacter_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.Refresh();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void PlanToBuild_WithNullBlueprint_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.PlanToBuild(null!);
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void SelectBlueprint_WithNull_ClearsDetail()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.SelectBlueprint(null);
            vm.SelectedBlueprint.Should().BeNull();
            vm.SelectedBlueprintDetail.Should().BeNull();
            vm.Dispose();
        }

        [Fact]
        public void CollapseAll_WithNullGroups_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.CollapseAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void ExpandAll_WithNullGroups_DoesNotThrow()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            var act = () => vm.ExpandAll();
            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_Safe()
        {
            var vm = new BlueprintBrowserViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }
    }
}
