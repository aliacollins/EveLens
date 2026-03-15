// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// A node in a flattened tree projection. Wraps any data item with tree metadata
    /// (depth, expand state, group identity) for virtualized rendering.
    /// </summary>
    public sealed class FlatTreeNode<T>
    {
        public T Data { get; }
        public int Depth { get; }
        public bool IsGroup { get; }
        public bool IsExpanded { get; internal set; }
        public bool HasChildren { get; }
        public string GroupKey { get; }
        public int ChildCount { get; }
        public string Chevron => !IsGroup ? "" : IsExpanded ? "\u25BE" : "\u25B8";

        internal FlatTreeNode(T data, int depth, bool isGroup, bool isExpanded, bool hasChildren, string groupKey, int childCount)
        {
            Data = data;
            Depth = depth;
            IsGroup = isGroup;
            IsExpanded = isExpanded;
            HasChildren = hasChildren;
            GroupKey = groupKey ?? string.Empty;
            ChildCount = childCount;
        }
    }

    /// <summary>
    /// Describes a group and its children for input to <see cref="FlattenedTreeSource{T}"/>.
    /// Children can be leaf items or nested sub-groups for multi-level trees.
    /// </summary>
    public sealed class GroupData<T>
    {
        public string Key { get; }
        public T GroupItem { get; }
        public IReadOnlyList<T> Items { get; }
        public IReadOnlyList<GroupData<T>> SubGroups { get; }

        public GroupData(string key, T groupItem, IReadOnlyList<T> items, IReadOnlyList<GroupData<T>>? subGroups = null)
        {
            Key = key ?? throw new ArgumentNullException(nameof(key));
            GroupItem = groupItem;
            Items = items ?? throw new ArgumentNullException(nameof(items));
            SubGroups = subGroups ?? Array.Empty<GroupData<T>>();
        }
    }

    /// <summary>
    /// Maintains a flat projection of a hierarchical tree structure, implementing
    /// <see cref="IReadOnlyList{T}"/> and <see cref="INotifyCollectionChanged"/> for
    /// direct use as a virtualizing ItemsSource with granular update notifications.
    /// Supports expand/collapse with external expand state via <see cref="HashSet{T}"/>
    /// for persistence. All operations assume UI thread affinity.
    /// </summary>
    public sealed class FlattenedTreeSource<T> : IReadOnlyList<FlatTreeNode<T>>, IList, INotifyCollectionChanged
    {
        private readonly List<FlatTreeNode<T>> _visible = new();
        private readonly List<TreeEntry> _allEntries = new();
        private readonly HashSet<string> _expandedKeys;

        /// <summary>Fired after any structural change (SetData, ToggleExpand, ExpandAll, CollapseAll).</summary>
        public event Action? Changed;

        /// <summary>Granular collection change notifications for VirtualizingStackPanel.</summary>
        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public FlattenedTreeSource(HashSet<string>? expandedKeys = null)
        {
            _expandedKeys = expandedKeys ?? new HashSet<string>(StringComparer.Ordinal);
        }

        public int Count => _visible.Count;
        public FlatTreeNode<T> this[int index] => _visible[index];

        /// <summary>Returns a snapshot of the current expand state for persistence.</summary>
        public HashSet<string> GetExpandState() => new HashSet<string>(_expandedKeys, StringComparer.Ordinal);

        /// <summary>Replaces the expand state with persisted keys. Does not rebuild or fire Changed.</summary>
        public void SetExpandState(HashSet<string> expandedKeys)
        {
            _expandedKeys.Clear();
            if (expandedKeys != null)
            {
                foreach (var key in expandedKeys)
                    _expandedKeys.Add(key);
            }
        }

        /// <summary>Rebuilds the tree from new group data. Preserves expand state. Fires Reset.</summary>
        public void SetData(IReadOnlyList<GroupData<T>> groups)
        {
            if (groups == null)
                throw new ArgumentNullException(nameof(groups));

            _allEntries.Clear();
            BuildEntries(groups, depth: 0);
            RebuildVisible();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Changed?.Invoke();
        }

        /// <summary>Toggles expand/collapse on the group at the given flat index. No-op for leaves.
        /// Fires granular Add/Remove notifications for incremental UI updates.</summary>
        public void ToggleExpand(int flatIndex)
        {
            if (flatIndex < 0 || flatIndex >= _visible.Count)
                return;

            var node = _visible[flatIndex];
            if (!node.IsGroup)
                return;

            node.IsExpanded = !node.IsExpanded;

            if (node.IsExpanded)
            {
                _expandedKeys.Add(node.GroupKey);

                // Build the children that should become visible
                var insertedItems = new List<FlatTreeNode<T>>();
                var allEntryIndex = FindAllEntryIndex(node.GroupKey);
                if (allEntryIndex >= 0)
                {
                    var entry = _allEntries[allEntryIndex];
                    CollectVisibleChildren(entry.FirstChildIndex,
                        entry.FirstChildIndex + entry.TotalDescendantCount, insertedItems);
                }

                if (insertedItems.Count > 0)
                {
                    int insertAt = flatIndex + 1;
                    _visible.InsertRange(insertAt, insertedItems);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Add, (IList)insertedItems, insertAt));
                }
            }
            else
            {
                _expandedKeys.Remove(node.GroupKey);

                // Count how many visible items follow this group that are its descendants
                int removeStart = flatIndex + 1;
                int removeCount = CountVisibleDescendants(flatIndex);

                if (removeCount > 0)
                {
                    var removedItems = _visible.GetRange(removeStart, removeCount);
                    _visible.RemoveRange(removeStart, removeCount);
                    CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(
                        NotifyCollectionChangedAction.Remove, (IList)removedItems, removeStart));
                }
            }

            Changed?.Invoke();
        }

        /// <summary>Expands all group nodes at every depth level. Fires Reset.</summary>
        public void ExpandAll()
        {
            foreach (var entry in _allEntries)
            {
                if (entry.IsGroup)
                    _expandedKeys.Add(entry.GroupKey);
            }

            RebuildVisible();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Changed?.Invoke();
        }

        /// <summary>Collapses all group nodes at every depth level. Fires Reset.</summary>
        public void CollapseAll()
        {
            _expandedKeys.Clear();
            RebuildVisible();
            CollectionChanged?.Invoke(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            Changed?.Invoke();
        }

        /// <summary>Finds the flat index of the first group with the given key, or -1.</summary>
        public int IndexOfGroup(string groupKey)
        {
            for (int i = 0; i < _visible.Count; i++)
            {
                if (_visible[i].IsGroup && string.Equals(_visible[i].GroupKey, groupKey, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        public IEnumerator<FlatTreeNode<T>> GetEnumerator() => _visible.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #region IList (required by Avalonia ItemsSourceView for INCC support)

        bool IList.IsFixedSize => false;
        bool IList.IsReadOnly => true;
        bool ICollection.IsSynchronized => false;
        object ICollection.SyncRoot => _visible;
        int ICollection.Count => _visible.Count;

        object? IList.this[int index]
        {
            get => _visible[index];
            set => throw new NotSupportedException();
        }

        int IList.Add(object? value) => throw new NotSupportedException();
        void IList.Clear() => throw new NotSupportedException();
        bool IList.Contains(object? value) => value is FlatTreeNode<T> node && _visible.Contains(node);
        int IList.IndexOf(object? value) => value is FlatTreeNode<T> node ? _visible.IndexOf(node) : -1;
        void IList.Insert(int index, object? value) => throw new NotSupportedException();
        void IList.Remove(object? value) => throw new NotSupportedException();
        void IList.RemoveAt(int index) => throw new NotSupportedException();
        void ICollection.CopyTo(Array array, int index) => ((IList)_visible).CopyTo(array, index);

        #endregion

        #region Granular Expand/Collapse Helpers

        /// <summary>Finds the index in _allEntries for the group with the given key.</summary>
        private int FindAllEntryIndex(string groupKey)
        {
            for (int i = 0; i < _allEntries.Count; i++)
            {
                if (_allEntries[i].IsGroup && string.Equals(_allEntries[i].GroupKey, groupKey, StringComparison.Ordinal))
                    return i;
            }
            return -1;
        }

        /// <summary>Collects the FlatTreeNodes that would be visible for a range of _allEntries,
        /// respecting current expand state. Used when expanding a group.</summary>
        private void CollectVisibleChildren(int start, int end, List<FlatTreeNode<T>> result)
        {
            int i = start;
            while (i < end)
            {
                var entry = _allEntries[i];

                if (!entry.IsGroup)
                {
                    result.Add(new FlatTreeNode<T>(
                        entry.Data, entry.Depth, false, false, false, string.Empty, 0));
                    i++;
                    continue;
                }

                bool expanded = _expandedKeys.Contains(entry.GroupKey);
                result.Add(new FlatTreeNode<T>(
                    entry.Data, entry.Depth, true, expanded, entry.ChildCount > 0,
                    entry.GroupKey, entry.ChildCount));

                if (expanded)
                    CollectVisibleChildren(entry.FirstChildIndex, entry.FirstChildIndex + entry.TotalDescendantCount, result);

                i += 1 + entry.TotalDescendantCount;
            }
        }

        /// <summary>Counts the number of visible items after flatIndex that are descendants of the
        /// group at flatIndex. Stops at the next sibling (same or lower depth).</summary>
        private int CountVisibleDescendants(int flatIndex)
        {
            int parentDepth = _visible[flatIndex].Depth;
            int count = 0;
            for (int i = flatIndex + 1; i < _visible.Count; i++)
            {
                if (_visible[i].Depth <= parentDepth)
                    break;
                count++;
            }
            return count;
        }

        #endregion

        #region Internal Tree Storage

        private sealed class TreeEntry
        {
            public T Data;
            public int Depth;
            public bool IsGroup;
            public string GroupKey;
            public int ChildCount; // direct children (leaves + sub-groups at depth+1)

            // For groups: indices of direct children in _allEntries
            // For leaves: empty
            public int FirstChildIndex;
            public int TotalDescendantCount; // all descendants at any depth
        }

        private void BuildEntries(IReadOnlyList<GroupData<T>> groups, int depth)
        {
            foreach (var group in groups)
            {
                int groupIndex = _allEntries.Count;
                int directChildren = group.SubGroups.Count + group.Items.Count;

                var entry = new TreeEntry
                {
                    Data = group.GroupItem,
                    Depth = depth,
                    IsGroup = true,
                    GroupKey = group.Key,
                    ChildCount = directChildren,
                    FirstChildIndex = groupIndex + 1,
                    TotalDescendantCount = 0 // calculated after children are added
                };
                _allEntries.Add(entry);

                int beforeChildren = _allEntries.Count;

                // Add sub-groups recursively
                BuildEntries(group.SubGroups, depth + 1);

                // Add leaf items
                foreach (var item in group.Items)
                {
                    _allEntries.Add(new TreeEntry
                    {
                        Data = item,
                        Depth = depth + 1,
                        IsGroup = false,
                        GroupKey = string.Empty,
                        ChildCount = 0,
                        FirstChildIndex = -1,
                        TotalDescendantCount = 0
                    });
                }

                entry.TotalDescendantCount = _allEntries.Count - beforeChildren;
            }
        }

        private void RebuildVisible()
        {
            _visible.Clear();
            WalkEntries(0, _allEntries.Count);
        }

        private void WalkEntries(int start, int end)
        {
            int i = start;
            while (i < end)
            {
                var entry = _allEntries[i];

                if (!entry.IsGroup)
                {
                    // Leaf node — always visible if parent is expanded (caller ensures this)
                    _visible.Add(new FlatTreeNode<T>(
                        entry.Data, entry.Depth, false, false, false, string.Empty, 0));
                    i++;
                    continue;
                }

                // Group node
                bool expanded = _expandedKeys.Contains(entry.GroupKey);
                _visible.Add(new FlatTreeNode<T>(
                    entry.Data, entry.Depth, true, expanded, entry.ChildCount > 0,
                    entry.GroupKey, entry.ChildCount));

                if (expanded)
                {
                    // Walk children (they are contiguous in _allEntries)
                    WalkEntries(entry.FirstChildIndex, entry.FirstChildIndex + entry.TotalDescendantCount);
                }

                // Skip past all descendants to the next sibling
                i += 1 + entry.TotalDescendantCount;
            }
        }

        #endregion
    }
}
