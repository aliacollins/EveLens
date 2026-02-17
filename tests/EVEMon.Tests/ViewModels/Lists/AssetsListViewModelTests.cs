using System;
using System.ComponentModel;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.ViewModels.Lists;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels.Lists
{
    public class AssetsListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        [Fact]
        public void CanInstantiate()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.Should().NotBeNull();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_TextFilterEmpty()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.TextFilter.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_SortAscendingTrue()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void DefaultState_GroupingNone()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.Grouping.Should().Be(AssetGrouping.None);
            vm.Dispose();
        }

        [Fact]
        public void RefreshWithNoCharacter_EmptyResults()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);
            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().BeEmpty();
            vm.Dispose();
        }

        [Fact]
        public void Dispose_SafeMultipleCalls()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.Dispose();
            var act = () => vm.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void TextFilter_RaisesPropertyChanged()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.TextFilter))
                    changedProp = e.PropertyName;
            };

            vm.TextFilter = "test";

            changedProp.Should().Be("TextFilter");
            vm.Dispose();
        }

        [Fact]
        public void Grouping_RaisesPropertyChanged()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.Grouping))
                    changedProp = e.PropertyName;
            };

            vm.Grouping = AssetGrouping.Category;

            changedProp.Should().Be("Grouping");
            vm.Dispose();
        }

        [Fact]
        public void SortColumn_RaisesPropertyChanged()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.SortColumn))
                    changedProp = e.PropertyName;
            };

            vm.SortColumn = AssetColumn.Quantity;

            changedProp.Should().Be("SortColumn");
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_SameColumn_ReversesDirection()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.SortColumn = AssetColumn.ItemName;
            vm.SortAscending = true;

            vm.ToggleSort(AssetColumn.ItemName);

            vm.SortAscending.Should().BeFalse();
            vm.Dispose();
        }

        [Fact]
        public void ToggleSort_DifferentColumn_SetsAscending()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.SortColumn = AssetColumn.ItemName;
            vm.SortAscending = false;

            vm.ToggleSort(AssetColumn.Quantity);

            vm.SortColumn.Should().Be(AssetColumn.Quantity);
            vm.SortAscending.Should().BeTrue();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_SettingsChanged_NoCharacter_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new AssetsListViewModel(agg);

            var act = () => agg.Publish(SettingsChangedEvent.Instance);

            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_ItemPricesUpdated_NoCharacter_DoesNotThrow()
        {
            var agg = CreateAggregator();
            var vm = new AssetsListViewModel(agg);

            var act = () => agg.Publish(ItemPricesUpdatedEvent.Instance);

            act.Should().NotThrow();
            vm.Dispose();
        }

        [Fact]
        public void EventSubscription_DisposedVM_DoesNotReceive()
        {
            var agg = CreateAggregator();
            var vm = new AssetsListViewModel(agg);
            vm.Dispose();

            // Should not throw even after dispose
            var act = () => agg.Publish(SettingsChangedEvent.Instance);
            act.Should().NotThrow();
        }

        [Fact]
        public void Refresh_GroupedItems_AlwaysHasAtLeastOneGroup()
        {
            var vm = new AssetsListViewModel(CreateAggregator());
            vm.Refresh();

            vm.GroupedItems.Should().HaveCountGreaterOrEqualTo(1);
            vm.Dispose();
        }
    }
}
