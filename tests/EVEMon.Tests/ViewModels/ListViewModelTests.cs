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
            public DateTime Timestamp { get; set; } = DateTime.MinValue;
        }

        private sealed class TestListViewModel : ListViewModel<TestItem, TestColumn, TestGrouping>
        {
            private readonly List<TestItem> _items;
            private bool _trackTimestamps;

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

            public void EnableTimestampTracking() => _trackTimestamps = true;

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

            protected override DateTime GetItemTimestamp(TestItem item)
            {
                return _trackTimestamps ? item.Timestamp : base.GetItemTimestamp(item);
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

        #region New-Item Tracking

        [Fact]
        public void NewItemCount_DefaultZero_WhenNoTimestampTracking()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateSampleItems());
            vm.Refresh();

            vm.NewItemCount.Should().Be(0);
        }

        [Fact]
        public void NewItemCount_DefaultZero_WhenNeverViewed()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateTimestampedItems());
            vm.EnableTimestampTracking();
            vm.Refresh();

            vm.NewItemCount.Should().Be(0, "no items are new until MarkAsViewed sets the baseline");
        }

        [Fact]
        public void MarkAsViewed_SetsLastViewedTimestamp()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.LastViewedTimestamp.Should().Be(DateTime.MinValue);

            vm.MarkAsViewed();

            vm.LastViewedTimestamp.Should().BeAfter(DateTime.MinValue);
            vm.LastViewedTimestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(2));
        }

        [Fact]
        public void NewItemCount_CountsItemsAfterLastViewed()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateTimestampedItems());
            vm.EnableTimestampTracking();

            // Mark as viewed — sets LastViewedTimestamp to "now"
            vm.MarkAsViewed();
            var viewedAt = vm.LastViewedTimestamp;
            vm.NewItemCount.Should().Be(0, "all existing items are older than LastViewedTimestamp");

            // Add a new item that's in the future
            vm.SetItems(new List<TestItem>
            {
                new TestItem { Name = "Old", Value = 1, Category = "A", Timestamp = viewedAt.AddMinutes(-5) },
                new TestItem { Name = "New1", Value = 2, Category = "A", Timestamp = viewedAt.AddMinutes(1) },
                new TestItem { Name = "New2", Value = 3, Category = "B", Timestamp = viewedAt.AddMinutes(2) },
            });
            vm.Refresh();

            vm.NewItemCount.Should().Be(2, "two items have timestamps after LastViewedTimestamp");
        }

        [Fact]
        public void IsNewItem_ReturnsFalse_WhenNeverViewed()
        {
            var items = CreateTimestampedItems();
            var vm = new TestListViewModel(CreateAggregator(), items);
            vm.EnableTimestampTracking();
            vm.Refresh();

            vm.IsNewItem(items[0]).Should().BeFalse("never viewed means no items are new");
        }

        [Fact]
        public void IsNewItem_ReturnsTrueForNewerItems()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.EnableTimestampTracking();
            vm.MarkAsViewed();
            var viewedAt = vm.LastViewedTimestamp;

            var oldItem = new TestItem { Name = "Old", Timestamp = viewedAt.AddMinutes(-1) };
            var newItem = new TestItem { Name = "New", Timestamp = viewedAt.AddMinutes(1) };

            vm.IsNewItem(oldItem).Should().BeFalse();
            vm.IsNewItem(newItem).Should().BeTrue();
        }

        [Fact]
        public void MarkAsViewed_PastItemsAreNotNew()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.EnableTimestampTracking();

            // Items with timestamps in the past
            vm.SetItems(new List<TestItem>
            {
                new TestItem { Name = "Past1", Value = 1, Category = "A", Timestamp = DateTime.UtcNow.AddHours(-2) },
                new TestItem { Name = "Past2", Value = 2, Category = "B", Timestamp = DateTime.UtcNow.AddHours(-1) },
            });

            // Mark as viewed sets LastViewedTimestamp to UtcNow, which is after all items
            vm.MarkAsViewed();
            vm.NewItemCount.Should().Be(0, "all items are in the past relative to MarkAsViewed time");
        }

        [Fact]
        public void MarkAsViewed_MovesTimestampForward()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.EnableTimestampTracking();
            vm.MarkAsViewed();
            var first = vm.LastViewedTimestamp;

            System.Threading.Thread.Sleep(15);
            vm.MarkAsViewed();
            var second = vm.LastViewedTimestamp;

            second.Should().BeAfter(first);
        }

        [Fact]
        public void NewItemCount_PropertyChanged_Fires()
        {
            var vm = new TestListViewModel(CreateAggregator(), CreateTimestampedItems());
            vm.EnableTimestampTracking();
            bool fired = false;
            ((INotifyPropertyChanged)vm).PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(vm.NewItemCount))
                    fired = true;
            };

            vm.Refresh();

            // NewItemCount goes from 0 to 0 (no change), but the setter fires PropertyChanged
            // Test MarkAsViewed which sets LastViewedTimestamp, triggering UpdateNewItemCount
            vm.MarkAsViewed();
            // fired may or may not be true depending on whether value changed
            // So let's test with items that actually change the count
            fired = false;
            vm.SetItems(new List<TestItem>
            {
                new TestItem { Name = "New", Timestamp = vm.LastViewedTimestamp.AddMinutes(1) },
            });
            vm.Refresh();

            fired.Should().BeTrue();
        }

        [Fact]
        public void Refresh_WithNullSource_ResetsNewItemCount()
        {
            var vm = new TestListViewModel(CreateAggregator());
            vm.EnableTimestampTracking();
            vm.MarkAsViewed();
            vm.Refresh();

            vm.NewItemCount.Should().Be(0);
        }

        private static List<TestItem> CreateTimestampedItems()
        {
            var baseTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
            return new List<TestItem>
            {
                new TestItem { Name = "Item1", Value = 1, Category = "A", Timestamp = baseTime.AddHours(-3) },
                new TestItem { Name = "Item2", Value = 2, Category = "A", Timestamp = baseTime.AddHours(-2) },
                new TestItem { Name = "Item3", Value = 3, Category = "B", Timestamp = baseTime.AddHours(-1) },
            };
        }

        #endregion
    }
}
