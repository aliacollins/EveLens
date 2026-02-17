using System.ComponentModel;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels.Lists
{
    public class IndustryJobsListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void HideInactive_DefaultFalse()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            vm.HideInactive.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void HideInactive_RaisesPropertyChanged()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.HideInactive))
                    changedProp = e.PropertyName;
            };

            vm.HideInactive = true;

            changedProp.Should().Be("HideInactive");
            vm.Dispose();
        }

        [Fact]
        public void HideInactive_SetTrue_TriggersRefresh()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            bool groupedItemsChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.GroupedItems))
                    groupedItemsChanged = true;
            };

            vm.HideInactive = true;

            groupedItemsChanged.Should().BeTrue();
            vm.GroupedItems.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_DefaultAll()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            vm.ShowIssuedFor.Should().Be(IssuedFor.All);
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_RaisesPropertyChanged()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.ShowIssuedFor))
                    changedProp = e.PropertyName;
            };

            vm.ShowIssuedFor = IssuedFor.Character;

            changedProp.Should().Be("ShowIssuedFor");
            vm.Dispose();
        }

        [Fact]
        public void ShowIssuedFor_SetCharacter_TriggersRefresh()
        {
            var vm = new IndustryJobsListViewModel(CreateAggregator());
            bool groupedItemsChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.GroupedItems))
                    groupedItemsChanged = true;
            };

            vm.ShowIssuedFor = IssuedFor.Character;

            groupedItemsChanged.Should().BeTrue();
            vm.GroupedItems.Should().NotBeNull();
            vm.Dispose();
        }
    }
}
