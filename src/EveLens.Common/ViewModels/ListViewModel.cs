// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// Represents a named group of items in a list view.
    /// </summary>
    /// <typeparam name="TItem">The type of items in the group.</typeparam>
    public sealed class ListGrouping<TItem>
    {
        /// <summary>
        /// Gets the display key for this group (e.g., "Amarr", "Region", "Category").
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// Gets the items in this group.
        /// </summary>
        public IReadOnlyList<TItem> Items { get; }

        public ListGrouping(string key, IReadOnlyList<TItem> items)
        {
            Key = key ?? string.Empty;
            Items = items ?? throw new ArgumentNullException(nameof(items));
        }
    }

    /// <summary>
    /// Generic base ViewModel for all list views in character monitoring.
    /// Consolidates the filter/sort/group pipeline duplicated across 11 IListView controls.
    /// Each concrete subclass implements ~100-150 lines vs 400-1500 lines per current control.
    /// </summary>
    /// <typeparam name="TItem">The domain model type (e.g., Asset, MarketOrder).</typeparam>
    /// <typeparam name="TColumn">The column enum (e.g., AssetColumn, MarketOrderColumn).</typeparam>
    /// <typeparam name="TGrouping">The grouping enum (e.g., AssetGrouping, MarketOrderGrouping).</typeparam>
    public abstract class ListViewModel<TItem, TColumn, TGrouping> : CharacterViewModelBase
        where TColumn : struct, Enum
        where TGrouping : struct, Enum
    {
        private string _textFilter = string.Empty;
        private TGrouping _grouping;
        private TColumn _sortColumn;
        private bool _sortAscending = true;
        private IReadOnlyList<ListGrouping<TItem>> _groupedItems = Array.Empty<ListGrouping<TItem>>();
        private IReadOnlyList<TItem> _items = Array.Empty<TItem>();
        private int _totalItemCount;
        private DateTime _lastViewedTimestamp = DateTime.MinValue;
        private int _newItemCount;

        /// <summary>
        /// Creates a new list ViewModel with explicit dependencies (testing).
        /// </summary>
        protected ListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        /// <summary>
        /// Creates a new list ViewModel using AppServices defaults (production).
        /// </summary>
        protected ListViewModel()
        {
        }

        #region Observable Properties

        /// <summary>
        /// Gets or sets the text filter. Setting triggers a refresh.
        /// </summary>
        public string TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value ?? string.Empty))
                    Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the current grouping. Setting triggers a refresh.
        /// </summary>
        public TGrouping Grouping
        {
            get => _grouping;
            set
            {
                if (SetProperty(ref _grouping, value))
                    Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the sort column. Setting triggers a refresh.
        /// </summary>
        public TColumn SortColumn
        {
            get => _sortColumn;
            set
            {
                if (SetProperty(ref _sortColumn, value))
                    Refresh();
            }
        }

        /// <summary>
        /// Gets or sets the sort direction. Setting triggers a refresh.
        /// </summary>
        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                    Refresh();
            }
        }

        /// <summary>
        /// Gets the processed items: filtered, sorted, and grouped.
        /// Bind the UI to this property.
        /// </summary>
        public IReadOnlyList<ListGrouping<TItem>> GroupedItems
        {
            get => _groupedItems;
            private set => SetProperty(ref _groupedItems, value);
        }

        /// <summary>
        /// Gets all items after filtering and sorting, flattened across groups.
        /// Bind DataGrid.ItemsSource to this property.
        /// </summary>
        public IReadOnlyList<TItem> Items
        {
            get => _items;
            private set => SetProperty(ref _items, value);
        }

        /// <summary>
        /// Gets the total number of items after filtering (before grouping).
        /// </summary>
        public int TotalItemCount
        {
            get => _totalItemCount;
            private set => SetProperty(ref _totalItemCount, value);
        }

        /// <summary>
        /// Gets the session-scoped timestamp of when the user last viewed this tab.
        /// Items with a timestamp after this value are considered "new".
        /// </summary>
        public DateTime LastViewedTimestamp
        {
            get => _lastViewedTimestamp;
            private set => SetProperty(ref _lastViewedTimestamp, value);
        }

        /// <summary>
        /// Gets the count of items whose timestamp is newer than <see cref="LastViewedTimestamp"/>.
        /// </summary>
        public int NewItemCount
        {
            get => _newItemCount;
            private set => SetProperty(ref _newItemCount, value);
        }

        #endregion

        #region Abstract Methods (subclass-defined)

        /// <summary>
        /// Returns the raw source items from the model. Called during <see cref="Refresh"/>.
        /// </summary>
        protected abstract IEnumerable<TItem> GetSourceItems();

        /// <summary>
        /// Returns true if the item matches the text filter.
        /// </summary>
        /// <param name="item">The item to test.</param>
        /// <param name="filter">The text filter (never null, may be empty).</param>
        protected abstract bool MatchesFilter(TItem item, string filter);

        /// <summary>
        /// Compares two items for sorting by the given column.
        /// Return negative if x &lt; y, zero if equal, positive if x &gt; y.
        /// </summary>
        protected abstract int CompareItems(TItem x, TItem y, TColumn column);

        /// <summary>
        /// Returns the group key string for the given item and grouping mode.
        /// Return null or empty to place the item in a default group.
        /// </summary>
        protected abstract string GetGroupKey(TItem item, TGrouping grouping);

        #endregion

        #region New-Item Tracking

        /// <summary>
        /// Returns the timestamp for an item, used for new-item tracking.
        /// Override in subclasses that have timestamped items (e.g., Journal→Date, Mail→SentDate).
        /// Returns <see cref="DateTime.MinValue"/> by default (no new-item tracking).
        /// </summary>
        protected virtual DateTime GetItemTimestamp(TItem item) => DateTime.MinValue;

        /// <summary>
        /// Marks the current view as seen, updating <see cref="LastViewedTimestamp"/>
        /// to the current time. Call this in each view's OnAttachedToVisualTree.
        /// </summary>
        public void MarkAsViewed()
        {
            LastViewedTimestamp = DateTime.UtcNow;
            UpdateNewItemCount();
        }

        /// <summary>
        /// Recalculates <see cref="NewItemCount"/> based on current items and <see cref="LastViewedTimestamp"/>.
        /// </summary>
        private void UpdateNewItemCount()
        {
            if (_lastViewedTimestamp == DateTime.MinValue)
            {
                NewItemCount = 0;
                return;
            }

            var source = GetSourceItems();
            if (source == null)
            {
                NewItemCount = 0;
                return;
            }

            int count = 0;
            foreach (var item in source)
            {
                var ts = GetItemTimestamp(item);
                if (ts > _lastViewedTimestamp)
                    count++;
            }
            NewItemCount = count;
        }

        /// <summary>
        /// Checks whether a specific item is "new" (its timestamp is after <see cref="LastViewedTimestamp"/>).
        /// </summary>
        public bool IsNewItem(TItem item)
        {
            if (_lastViewedTimestamp == DateTime.MinValue)
                return false;

            var ts = GetItemTimestamp(item);
            return ts > _lastViewedTimestamp;
        }

        #endregion

        #region Pipeline

        /// <summary>
        /// Toggles sort on a column: if same column, reverses direction; if different column, sorts ascending.
        /// </summary>
        /// <param name="column">The column to sort by.</param>
        public void ToggleSort(TColumn column)
        {
            if (EqualityComparer<TColumn>.Default.Equals(_sortColumn, column))
            {
                SortAscending = !_sortAscending;
            }
            else
            {
                _sortAscending = true;
                SortColumn = column; // triggers refresh via setter
            }
        }

        /// <summary>
        /// Refreshes the <see cref="GroupedItems"/> by running the full pipeline:
        /// source -> filter -> sort -> group.
        /// Call this when underlying data changes.
        /// </summary>
        public void Refresh()
        {
            var source = GetSourceItems();
            if (source == null)
            {
                GroupedItems = Array.Empty<ListGrouping<TItem>>();
                Items = Array.Empty<TItem>();
                TotalItemCount = 0;
                NewItemCount = 0;
                return;
            }

            // Filter
            var filtered = string.IsNullOrEmpty(_textFilter)
                ? source.ToList()
                : source.Where(item => MatchesFilter(item, _textFilter)).ToList();

            TotalItemCount = filtered.Count;

            // Sort
            if (filtered.Count > 1)
            {
                var comparer = new ItemComparer(this);
                filtered.Sort(comparer);
            }

            // Set flat items list for DataGrid binding
            Items = filtered;

            // Group
            var grouping = _grouping;
            var isDefaultGrouping = EqualityComparer<TGrouping>.Default.Equals(grouping, default);

            if (isDefaultGrouping)
            {
                // No grouping — single group with all items
                GroupedItems = new[] { new ListGrouping<TItem>(string.Empty, filtered) };
            }
            else
            {
                // Group by key, preserving sort order within groups
                var groups = new List<ListGrouping<TItem>>();
                var currentKey = (string?)null;
                var currentItems = new List<TItem>();

                // First pass: collect unique keys in order of first appearance
                var keyedItems = filtered
                    .Select(item => new { Item = item, Key = GetGroupKey(item, grouping) ?? string.Empty })
                    .ToList();

                var groupedByKey = keyedItems
                    .GroupBy(x => x.Key)
                    .Select(g => new ListGrouping<TItem>(g.Key, g.Select(x => x.Item).ToList()))
                    .ToList();

                GroupedItems = groupedByKey;
            }

            // Update new-item tracking
            UpdateNewItemCount();
        }

        /// <summary>
        /// Called when the character changes. Refreshes the data.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Refresh();
        }

        #endregion

        #region Comparer

        private sealed class ItemComparer : IComparer<TItem>
        {
            private readonly ListViewModel<TItem, TColumn, TGrouping> _vm;

            public ItemComparer(ListViewModel<TItem, TColumn, TGrouping> vm) => _vm = vm;

            public int Compare(TItem? x, TItem? y)
            {
                if (x == null && y == null) return 0;
                if (x == null) return -1;
                if (y == null) return 1;

                int result = _vm.CompareItems(x, y, _vm._sortColumn);
                return _vm._sortAscending ? result : -result;
            }
        }

        #endregion
    }
}
