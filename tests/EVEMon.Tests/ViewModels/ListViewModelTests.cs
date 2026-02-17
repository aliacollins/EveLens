using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using Xunit;

namespace EVEMon.Tests.ViewModels
{
    public class ListViewModelTests
    {
        private static IEventAggregator CreateAggregator() => new EventAggregator();

        #region Test Types

        private enum TestColumn { Name, Value, Category }
        private enum TestGrouping { None, ByCategory }

        private sealed class TestItem
        {
            public string Name { get; set; } = string.Empty;
            public int Value { get; set; }
            public string Category { get; set; } = string.Empty;
        }

        private sealed class TestListViewModel : ListViewModel<TestItem, TestColumn, TestGrouping>
        {
            private readonly List<TestItem> _items;

            public TestListViewModel(IEventAggregator aggregator, List<TestItem>? items = null)
                : base(aggregator)
            {
                _items = items ?? new List<TestItem>();
            }

            public void SetItems(List<TestItem> items)
            {
                _items.Clear();
                _items.AddRange(items);
            }

            protected override IEnumerable<TestItem> GetSourceItems() => _items;

            protected override bool MatchesFilter(TestItem item, string filter)
            {
                return item.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                       item.Category.Contains(filter, StringComparison.OrdinalIgnoreCase);
            }

            protected override int CompareItems(TestItem x, TestItem y, TestColumn column)
            {
                return column switch
                {
                    TestColumn.Name => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase),
                    TestColumn.Value => x.Value.CompareTo(y.Value),
                    TestColumn.Category => string.Compare(x.Category, y.Category, StringComparison.OrdinalIgnoreCase),
                    _ => 0
                };
            }

            protected override string GetGroupKey(TestItem item, TestGrouping grouping)
            {
                return grouping switch
                {
                    TestGrouping.ByCategory => item.Category,
                    _ => string.Empty
                };
            }
        }

        #endregion

        private static List<TestItem> CreateSampleItems()
        {
            return new List<TestItem>
            {
                new TestItem { Name = "Tritanium", Value = 5, Category = "Minerals" },
                new TestItem { Name = "Pyerite", Value = 10, Category = "Minerals" },
                new TestItem { Name = "Rifter", Value = 500, Category = "Ships" },
                new TestItem { Name = "Punisher", Value = 400, Category = "Ships" },
                new TestItem { Name = "Hobgoblin I", Value = 15, Category = "Drones" },
            };
        }

        [Fact]
        public void Refresh_WithEmptySource_ReturnsEmptyGroups()
        {
            var vm = new TestListViewModel(CreateAggregator());

            vm.Refresh();

            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().BeEmpty();
            vm.TotalItemCount.Should().Be(0);
        }

        [Fact]
        public void Refresh_WithItems_ReturnsAllInSingleGroup()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);

            vm.Refresh();

            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().HaveCount(5);
            vm.TotalItemCount.Should().Be(5);
        }

        [Fact]
        public void TextFilter_FiltersItems()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.Refresh();

            vm.TextFilter = "Tri";

            vm.TotalItemCount.Should().Be(1);
            vm.GroupedItems[0].Items.Should().ContainSingle()
                .Which.Name.Should().Be("Tritanium");
        }

        [Fact]
        public void TextFilter_CaseInsensitive()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);

            vm.TextFilter = "minerals";

            vm.TotalItemCount.Should().Be(2, "both minerals should match category filter");
        }

        [Fact]
        public void TextFilter_Empty_ShowsAll()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.TextFilter = "Tri";
            vm.TotalItemCount.Should().Be(1);

            vm.TextFilter = string.Empty;

            vm.TotalItemCount.Should().Be(5);
        }

        [Fact]
        public void SortColumn_SortsByName()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.SortColumn = TestColumn.Name;
            vm.SortAscending = true;

            vm.Refresh();

            var names = vm.GroupedItems[0].Items.Select(i => i.Name).ToList();
            names.Should().BeInAscendingOrder();
        }

        [Fact]
        public void SortColumn_SortsByValueDescending()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.SortColumn = TestColumn.Value;

            vm.SortAscending = false;

            var values = vm.GroupedItems[0].Items.Select(i => i.Value).ToList();
            values.Should().BeInDescendingOrder();
        }

        [Fact]
        public void Grouping_ByCategory_CreatesGroups()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);

            vm.Grouping = TestGrouping.ByCategory;

            vm.GroupedItems.Should().HaveCount(3, "Minerals, Ships, Drones");
            var keys = vm.GroupedItems.Select(g => g.Key).ToList();
            keys.Should().Contain("Minerals");
            keys.Should().Contain("Ships");
            keys.Should().Contain("Drones");
        }

        [Fact]
        public void Grouping_ByCategory_ItemsInCorrectGroups()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);

            vm.Grouping = TestGrouping.ByCategory;

            var minerals = vm.GroupedItems.First(g => g.Key == "Minerals");
            minerals.Items.Should().HaveCount(2);

            var ships = vm.GroupedItems.First(g => g.Key == "Ships");
            ships.Items.Should().HaveCount(2);

            var drones = vm.GroupedItems.First(g => g.Key == "Drones");
            drones.Items.Should().HaveCount(1);
        }

        [Fact]
        public void Grouping_None_SingleGroup()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.Grouping = TestGrouping.ByCategory;

            vm.Grouping = TestGrouping.None;

            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().HaveCount(5);
        }

        [Fact]
        public void ToggleSort_SameColumn_ReversesDirection()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            vm.SortColumn = TestColumn.Name;
            vm.SortAscending = true;

            vm.ToggleSort(TestColumn.Name);

            vm.SortAscending.Should().BeFalse();
        }

        [Fact]
        public void ToggleSort_DifferentColumn_SetsAscending()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            vm.SortColumn = TestColumn.Name;
            vm.SortAscending = false;

            vm.ToggleSort(TestColumn.Value);

            vm.SortColumn.Should().Be(TestColumn.Value);
            vm.SortAscending.Should().BeTrue();
        }

        [Fact]
        public void CombinedFilterSortGroup_WorksTogether()
        {
            var items = CreateSampleItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.SortColumn = TestColumn.Name;
            vm.SortAscending = true;
            vm.Grouping = TestGrouping.ByCategory;

            vm.TextFilter = "i"; // matches Tritanium, Pyerite, Rifter, Punisher, Hobgoblin I (all contain 'i')

            // All 5 items contain 'i' in name
            vm.TotalItemCount.Should().Be(5);
            vm.GroupedItems.Should().HaveCountGreaterThan(0);
        }

        [Fact]
        public void GroupedItems_PropertyChanged_Fires()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            string? changedProp = null;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListViewModel<TestItem, TestColumn, TestGrouping>.GroupedItems))
                    changedProp = e.PropertyName;
            };

            vm.Refresh();

            changedProp.Should().Be("GroupedItems");
        }

        [Fact]
        public void TotalItemCount_PropertyChanged_Fires()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            bool totalChanged = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(ListViewModel<TestItem, TestColumn, TestGrouping>.TotalItemCount))
                    totalChanged = true;
            };

            vm.Refresh();

            totalChanged.Should().BeTrue();
        }

        [Fact]
        public void TextFilter_Null_TreatedAsEmpty()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            vm.TextFilter = "Tri"; // set a non-empty filter first
            vm.TotalItemCount.Should().Be(1);

            vm.TextFilter = null!;

            vm.TextFilter.Should().BeEmpty();
            vm.TotalItemCount.Should().Be(5);
        }

        [Fact]
        public void FilterSortGroup_WithNoMatchingFilter_EmptyResult()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());

            vm.TextFilter = "ZZZZZ_NONEXISTENT";

            vm.TotalItemCount.Should().Be(0);
            vm.GroupedItems.Should().HaveCount(1);
            vm.GroupedItems[0].Items.Should().BeEmpty();
        }

        [Fact]
        public void SetItems_ThenRefresh_ShowsNewItems()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.Refresh();
            vm.TotalItemCount.Should().Be(0);

            vm.SetItems(CreateSampleItems());
            vm.Refresh();

            vm.TotalItemCount.Should().Be(5);
        }
    }
}
