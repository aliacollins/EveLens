// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Enumerations;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the item browser view showing items organized by market groups,
    /// excluding ships.
    /// </summary>
    public sealed class ItemBrowserViewModel : CharacterViewModelBase
    {
        private readonly PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private bool _showCanUseOnly;
        private List<ItemGroupEntry>? _allGroups;
        private List<ItemGroupEntry> _groups = new();
        private Item? _selectedItem;
        private ItemDetailInfo? _selectedItemDetail;
        private List<BrowserTreeNode> _treeRoots = new();
        private List<BrowserTreeNode> _flattenedNodes = new();

        public ItemBrowserViewModel() : base()
        {
            Subscribe<PlanChangedEvent>(_ => Rebuild());
            SubscribeForCharacter<CharacterUpdatedEvent>(_ => Rebuild());
        }

        public ItemBrowserViewModel(PlanEditorViewModel planEditor,
            IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor;
        }

        public ItemBrowserViewModel(PlanEditorViewModel planEditor) : base()
        {
            _planEditor = planEditor;
            Subscribe<PlanChangedEvent>(_ => Rebuild());
            SubscribeForCharacter<CharacterUpdatedEvent>(_ => Rebuild());
        }

        public ItemBrowserViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        /// <summary>
        /// Gets the list of item groups currently visible after filtering.
        /// </summary>
        public List<ItemGroupEntry> Groups => _groups;

        /// <summary>
        /// Gets the hierarchical tree roots for the item browser (one per top-level market group).
        /// </summary>
        public List<BrowserTreeNode> TreeRoots => _treeRoots;

        /// <summary>
        /// Gets the flattened list of visible tree nodes.
        /// </summary>
        public List<BrowserTreeNode> FlattenedNodes => _flattenedNodes;

        /// <summary>
        /// Gets or sets the currently selected item.
        /// </summary>
        public Item? SelectedItem
        {
            get => _selectedItem;
            private set => SetProperty(ref _selectedItem, value);
        }

        /// <summary>
        /// Gets the detail info for the currently selected item.
        /// </summary>
        public ItemDetailInfo? SelectedItemDetail
        {
            get => _selectedItemDetail;
            private set => SetProperty(ref _selectedItemDetail, value);
        }

        /// <summary>
        /// Gets or sets the text filter for item names.
        /// </summary>
        public string TextFilter
        {
            get => _textFilter;
            set
            {
                var newFilter = value ?? string.Empty;
                if (_textFilter != newFilter)
                {
                    _textFilter = newFilter;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show only items the character can use.
        /// </summary>
        public bool ShowCanUseOnly
        {
            get => _showCanUseOnly;
            set
            {
                if (_showCanUseOnly != value)
                {
                    _showCanUseOnly = value;
                    Refresh();
                }
            }
        }

        /// <summary>
        /// Rebuilds the item group data when the character changes.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Rebuild();
        }

        private void Rebuild()
        {
            BuildGroupData();
            Refresh();
        }

        private void BuildGroupData()
        {
            var marketGroups = StaticItems.MarketGroups;
            if (marketGroups == null)
            {
                _allGroups = null;
                _treeRoots = new List<BrowserTreeNode>();
                return;
            }

            var character = Character as Character;
            var groups = new List<ItemGroupEntry>();
            var shipsGroup = StaticItems.ShipsMarketGroup;
            var treeRoots = new List<BrowserTreeNode>();

            foreach (var topGroup in marketGroups)
            {
                if (shipsGroup != null && topGroup == shipsGroup)
                    continue;

                // Build flat groups (backward compat)
                CollectLeafGroups(topGroup, groups, character, shipsGroup);

                // Build hierarchical tree node (collapsed by default — item tree is very large)
                var treeNode = BrowserTreeNode.FromMarketGroup(topGroup, 0, character);
                if (treeNode.TotalLeafCount > 0)
                {
                    treeNode.IsExpanded = false;
                    treeRoots.Add(treeNode);
                }
            }

            _allGroups = groups.OrderBy(g => g.Name).ToList();
            _treeRoots = treeRoots;
        }

        private static void CollectLeafGroups(MarketGroup group, List<ItemGroupEntry> result,
            Character? character, MarketGroup? shipsGroup)
        {
            if (shipsGroup != null && group == shipsGroup)
                return;

            // If the group has items directly, add it as a leaf group
            if (group.Items != null && group.Items.Any())
            {
                var entries = new List<ItemEntry>();
                foreach (var item in group.Items)
                {
                    bool canUse = CanUseItem(item, character);
                    entries.Add(new ItemEntry(item, canUse));
                }

                if (entries.Count > 0)
                {
                    result.Add(new ItemGroupEntry(group.CategoryPath, entries));
                }
            }

            // Recurse into sub-groups
            if (group.SubGroups != null)
            {
                foreach (var subGroup in group.SubGroups)
                {
                    CollectLeafGroups(subGroup, result, character, shipsGroup);
                }
            }
        }

        private static bool CanUseItem(Item item, Character? character)
        {
            if (character == null)
                return false;

            foreach (var prereq in item.Prerequisites)
            {
                if (character.GetSkillLevel(prereq.Skill) < prereq.Level)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Applies the current filter and visibility settings.
        /// </summary>
        public void Refresh()
        {
            if (_allGroups == null)
            {
                _groups = new List<ItemGroupEntry>();
                _flattenedNodes = new List<BrowserTreeNode>();
                return;
            }

            // Apply to flat groups (backward compat)
            var visible = new List<ItemGroupEntry>();

            foreach (var group in _allGroups)
            {
                group.UpdateVisibility(_textFilter, _showCanUseOnly);

                if (group.VisibleItems.Count > 0)
                    visible.Add(group);
            }

            _groups = visible;

            // Apply to tree
            foreach (var root in _treeRoots)
            {
                root.ApplyFilter(_textFilter, _showCanUseOnly);
            }

            UpdateFlattenedNodes();
        }

        /// <summary>
        /// Toggles a tree node's expanded state and rebuilds the flattened list.
        /// </summary>
        public void ToggleNode(BrowserTreeNode node)
        {
            if (node == null || node.IsLeaf) return;
            node.IsExpanded = !node.IsExpanded;
            UpdateFlattenedNodes();
        }

        private void UpdateFlattenedNodes()
        {
            _flattenedNodes = BrowserTreeNode.FlattenVisible(_treeRoots);
        }

        /// <summary>
        /// Selects an item and computes its detail information.
        /// </summary>
        public void SelectItem(Item? item)
        {
            SelectedItem = item;

            if (item == null)
            {
                SelectedItemDetail = null;
                return;
            }

            var character = Character as Character;
            var prereqs = new List<PrerequisiteInfo>();

            foreach (var prereq in item.Prerequisites)
            {
                long charLevel = character?.GetSkillLevel(prereq.Skill) ?? 0;
                prereqs.Add(new PrerequisiteInfo(
                    prereq.Skill.Name,
                    prereq.Level,
                    charLevel,
                    charLevel >= prereq.Level));
            }

            bool canUse = prereqs.Count == 0 || prereqs.All(p => p.IsMet);
            string slotText = item.FittingSlot switch
            {
                ItemSlot.High => "High Slot",
                ItemSlot.Medium => "Medium Slot",
                ItemSlot.Low => "Low Slot",
                ItemSlot.NoSlot => "No Slot",
                _ => ""
            };

            SelectedItemDetail = new ItemDetailInfo(
                item.Name,
                item.Description,
                slotText,
                prereqs,
                canUse);
        }

        /// <summary>
        /// Plans the prerequisites needed to use the specified item.
        /// </summary>
        public void PlanToUse(Item? item)
        {
            if (item == null)
                return;

            var plan = _planEditor?.Plan;
            if (plan == null)
                return;

            foreach (var prereq in item.Prerequisites)
            {
                var character = Character as Character;
                long charLevel = character?.GetSkillLevel(prereq.Skill) ?? 0;
                if (charLevel < prereq.Level)
                {
                    plan.PlanTo(prereq.Skill, prereq.Level);
                }
            }
        }

        /// <summary>
        /// Collapses all item groups.
        /// </summary>
        public void CollapseAll()
        {
            if (_allGroups != null)
            {
                foreach (var g in _allGroups)
                    g.IsExpanded = false;
            }

            foreach (var root in _treeRoots)
                root.SetExpandedAll(false);
            UpdateFlattenedNodes();
        }

        /// <summary>
        /// Expands all item groups.
        /// </summary>
        public void ExpandAll()
        {
            if (_allGroups != null)
            {
                foreach (var g in _allGroups)
                    g.IsExpanded = true;
            }

            foreach (var root in _treeRoots)
                root.SetExpandedAll(true);
            UpdateFlattenedNodes();
        }

        #region Nested Types

        /// <summary>
        /// Represents a group of items from a market group.
        /// </summary>
        public sealed class ItemGroupEntry
        {
            private readonly List<ItemEntry> _allItems;

            public string Name { get; }
            public List<ItemEntry> AllItems => _allItems;
            public List<ItemEntry> VisibleItems { get; private set; }
            public bool IsExpanded { get; set; }
            public string CountText => $"{VisibleItems.Count} items";

            public ItemGroupEntry(string name, List<ItemEntry> items)
            {
                Name = name;
                _allItems = items.OrderBy(i => i.Name).ToList();
                VisibleItems = _allItems;
                IsExpanded = false;
            }

            public void UpdateVisibility(string filter, bool showCanUseOnly)
            {
                IEnumerable<ItemEntry> filtered = _allItems;

                if (showCanUseOnly)
                    filtered = filtered.Where(i => i.CanUse);

                if (!string.IsNullOrEmpty(filter))
                    filtered = filtered.Where(i =>
                        i.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

                VisibleItems = filtered.ToList();
            }
        }

        /// <summary>
        /// Represents a single item entry with its usability status.
        /// </summary>
        public sealed class ItemEntry
        {
            public Item Item { get; }
            public string Name { get; }
            public bool CanUse { get; }

            public ItemEntry(Item item, bool canUse)
            {
                Item = item;
                Name = item.Name;
                CanUse = canUse;
            }
        }

        /// <summary>
        /// Detailed information about a selected item.
        /// </summary>
        public sealed class ItemDetailInfo
        {
            public string Name { get; }
            public string Description { get; }
            public string SlotType { get; }
            public List<PrerequisiteInfo> Prerequisites { get; }
            public bool CanUse { get; }

            public ItemDetailInfo(string name, string description, string slotType,
                List<PrerequisiteInfo> prerequisites, bool canUse)
            {
                Name = name;
                Description = description;
                SlotType = slotType;
                Prerequisites = prerequisites;
                CanUse = canUse;
            }
        }

        /// <summary>
        /// Prerequisite skill information for an item.
        /// </summary>
        public sealed class PrerequisiteInfo
        {
            public string Name { get; }
            public long RequiredLevel { get; }
            public long CharacterLevel { get; }
            public bool IsMet { get; }
            public string DisplayText => $"{Name} {Skill.GetRomanFromInt(RequiredLevel)}";

            public PrerequisiteInfo(string name, long requiredLevel, long characterLevel, bool isMet)
            {
                Name = name;
                RequiredLevel = requiredLevel;
                CharacterLevel = characterLevel;
                IsMet = isMet;
            }
        }

        #endregion
    }
}
