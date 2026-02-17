using System.ComponentModel;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels.Lists
{
    public class PlanetaryListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void ShowEcuOnly_DefaultFalse()
        {
            var vm = new PlanetaryListViewModel(CreateAggregator());
            vm.ShowEcuOnly.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ShowEcuOnly_RaisesPropertyChanged()
        {
            var vm = new PlanetaryListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.ShowEcuOnly))
                    changedProp = e.PropertyName;
            };

            vm.ShowEcuOnly = true;

            changedProp.Should().Be("ShowEcuOnly");
            vm.Dispose();
        }

        [Fact]
        public void ShowEcuOnly_SetTrue_TriggersRefresh()
        {
            var vm = new PlanetaryListViewModel(CreateAggregator());
            bool groupedItemsChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.GroupedItems))
                    groupedItemsChanged = true;
            };

            vm.ShowEcuOnly = true;

            groupedItemsChanged.Should().BeTrue();
            vm.GroupedItems.Should().NotBeNull();
            vm.Dispose();
        }
    }
}
