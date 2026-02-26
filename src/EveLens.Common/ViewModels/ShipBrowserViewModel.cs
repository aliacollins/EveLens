// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the ship browser view showing ships organized by market groups.
    /// </summary>
    public sealed class ShipBrowserViewModel : CharacterViewModelBase
    {
        private readonly PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private bool _showCanFlyOnly;
        private List<ShipGroupEntry>? _allGroups;
        private List<ShipGroupEntry> _groups = new();
        private Item? _selectedShip;
        private ShipDetailInfo? _selectedShipDetail;
        private BrowserTreeNode? _treeRoot;
        private List<BrowserTreeNode> _flattenedNodes = new();

        public ShipBrowserViewModel() : base()
        {
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        public ShipBrowserViewModel(IEventAggregator agg)
            : base(agg) { }

        public ShipBrowserViewModel(PlanEditorViewModel planEditor,
            IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor;
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        public ShipBrowserViewModel(PlanEditorViewModel planEditor)
            : base()
        {
            _planEditor = planEditor;
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        /// <summary>
        /// Gets the list of ship groups currently visible after filtering.
        /// </summary>
        public List<ShipGroupEntry> Groups => _groups;

        /// <summary>
        /// Gets the hierarchical tree root for the ship browser.
        /// </summary>
        public BrowserTreeNode? TreeRoot => _treeRoot;

        /// <summary>
        /// Gets the flattened list of visible tree nodes (for binding to a flat ItemsControl).
        /// </summary>
        public List<BrowserTreeNode> FlattenedNodes => _flattenedNodes;

        /// <summary>
        /// Gets or sets the currently selected ship.
        /// </summary>
        public Item? SelectedShip
        {
            get => _selectedShip;
            private set => SetProperty(ref _selectedShip, value);
        }

        /// <summary>
        /// Gets or sets the detail info for the currently selected ship.
        /// </summary>
        public ShipDetailInfo? SelectedShipDetail
        {
            get => _selectedShipDetail;
            private set => SetProperty(ref _selectedShipDetail, value);
        }

        /// <summary>
        /// Gets or sets the text filter for ship names.
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
        /// Gets or sets whether to show only ships the character can fly.
        /// </summary>
        public bool ShowCanFlyOnly
        {
            get => _showCanFlyOnly;
            set
            {
                if (_showCanFlyOnly != value)
                {
                    _showCanFlyOnly = value;
                    ApplyFilter();
                }
            }
        }

        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Rebuild();
        }

        private void Rebuild()
        {
            BuildGroupData();
            ApplyFilter();
        }

        /// <summary>
        /// Rebuilds groups from static data and applies the current filter.
        /// </summary>
        public void Refresh()
        {
            Rebuild();
        }

        /// <summary>
        /// Selects a ship and computes its detail information.
        /// </summary>
        public void SelectShip(Item? ship)
        {
            SelectedShip = ship;

            if (ship == null)
            {
                SelectedShipDetail = null;
                return;
            }

            var prereqs = ship.Prerequisites
                .Select(p =>
                {
                    long charLevel = 0;
                    if (Character != null)
                        charLevel = Character.GetSkillLevel(p.Skill);

                    return new ShipPrerequisiteInfo(
                        p.Skill.Name,
                        p.Level,
                        charLevel,
                        charLevel >= p.Level);
                })
                .ToList();

            bool canFly = prereqs.Count == 0 || prereqs.All(p => p.IsMet);

            // Build breadcrumb path from market group chain
            string groupPath = BuildGroupPath(ship);

            SelectedShipDetail = new ShipDetailInfo(
                ship.Name,
                ship.Description,
                ship.Race.ToString(),
                prereqs,
                canFly,
                groupPath,
                ship.ID);
        }

        /// <summary>
        /// Plans all unmet prerequisites for the given ship into the current plan.
        /// </summary>
        public void PlanToFly(Item? ship)
        {
            if (ship == null || _planEditor == null)
                return;

            foreach (var prereq in ship.Prerequisites)
            {
                long charLevel = Character?.GetSkillLevel(prereq.Skill) ?? 0;
                if (charLevel < prereq.Level)
                {
                    _planEditor.PlanTo(prereq.Skill, prereq.Level);
                }
            }
        }

        /// <summary>
        /// Collapses all ship groups.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = false;

            _treeRoot?.SetExpandedAll(false);
            UpdateFlattenedNodes();
        }

        /// <summary>
        /// Expands all ship groups.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = true;

            _treeRoot?.SetExpandedAll(true);
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

        private static string BuildGroupPath(Item ship)
        {
            var parts = new List<string>();
            var group = ship.MarketGroup;
            while (group != null)
            {
                parts.Add(group.Name);
                group = group.ParentGroup;
            }
            parts.Reverse();
            return parts.Count > 0 ? string.Join(" \u203A ", parts) : "";
        }

        private void BuildGroupData()
        {
            var shipsRoot = StaticItems.ShipsMarketGroup;
            if (shipsRoot == null)
            {
                _allGroups = null;
                _treeRoot = null;
                return;
            }

            // Build flat groups (backward compat)
            var groups = new List<ShipGroupEntry>();
            CollectGroups(shipsRoot, groups);
            _allGroups = groups;

            // Build hierarchical tree
            var character = Character as Character;
            _treeRoot = BrowserTreeNode.FromMarketGroup(shipsRoot, 0, character);
        }

        private void CollectGroups(MarketGroup group, List<ShipGroupEntry> result)
        {
            // If this group has items directly, create an entry
            var items = group.Items.ToList();
            if (items.Count > 0)
            {
                var ships = items
                    .OrderBy(i => i.Name)
                    .Select(i => new ShipEntry(i, i.Name, CanCharacterFly(i), i.MetaLevel))
                    .ToList();

                result.Add(new ShipGroupEntry(group.Name, ships));
            }

            // Recurse into sub-groups
            foreach (var sub in group.SubGroups)
            {
                CollectGroups(sub, result);
            }
        }

        private bool CanCharacterFly(Item ship)
        {
            if (Character == null)
                return false;

            foreach (var prereq in ship.Prerequisites)
            {
                if (Character.GetSkillLevel(prereq.Skill) < prereq.Level)
                    return false;
            }

            return true;
        }

        private void ApplyFilter()
        {
            if (_allGroups == null)
            {
                _groups = new List<ShipGroupEntry>();
                _flattenedNodes = new List<BrowserTreeNode>();
                return;
            }

            // Apply to flat groups (backward compat)
            var visible = new List<ShipGroupEntry>();

            foreach (var group in _allGroups)
            {
                var filtered = group.AllShips.AsEnumerable();

                if (!string.IsNullOrEmpty(_textFilter))
                    filtered = filtered.Where(s =>
                        s.Name.Contains(_textFilter, StringComparison.OrdinalIgnoreCase));

                if (_showCanFlyOnly)
                    filtered = filtered.Where(s => s.CanFly);

                var list = filtered.ToList();
                group.VisibleShips = list;

                if (list.Count > 0)
                    visible.Add(group);
            }

            _groups = visible;

            // Apply to tree
            if (_treeRoot != null)
            {
                _treeRoot.ApplyFilter(_textFilter, _showCanFlyOnly);
                UpdateFlattenedNodes();
            }
        }

        private void UpdateFlattenedNodes()
        {
            if (_treeRoot == null)
            {
                _flattenedNodes = new List<BrowserTreeNode>();
                return;
            }

            _flattenedNodes = BrowserTreeNode.FlattenVisible(_treeRoot.VisibleChildren);
        }
    }

    /// <summary>
    /// Represents a group of ships from a market group.
    /// </summary>
    public sealed class ShipGroupEntry
    {
        public string Name { get; }
        public List<ShipEntry> AllShips { get; }
        public List<ShipEntry> VisibleShips { get; set; }
        public bool IsExpanded { get; set; }
        public int TotalCount => AllShips.Count;
        public string CountText => $"{VisibleShips.Count} ships";

        public ShipGroupEntry(string name, List<ShipEntry> ships)
        {
            Name = name;
            AllShips = ships;
            VisibleShips = ships;
            IsExpanded = true;
        }
    }

    /// <summary>
    /// Represents a single ship entry within a group.
    /// </summary>
    public sealed class ShipEntry
    {
        public Item Ship { get; }
        public string Name { get; }
        public bool CanFly { get; }
        public long Rank { get; }

        public ShipEntry(Item ship, string name, bool canFly, long rank)
        {
            Ship = ship;
            Name = name;
            CanFly = canFly;
            Rank = rank;
        }
    }

    /// <summary>
    /// Detail information about a selected ship.
    /// </summary>
    public sealed class ShipDetailInfo
    {
        public string Name { get; }
        public string Description { get; }
        public string Race { get; }
        public List<ShipPrerequisiteInfo> Prerequisites { get; }
        public bool CanFly { get; }
        public string GroupPath { get; }
        public long TypeId { get; }

        public ShipDetailInfo(string name, string description, string race,
            List<ShipPrerequisiteInfo> prerequisites, bool canFly,
            string groupPath = "", long typeId = 0)
        {
            Name = name;
            Description = description;
            Race = race;
            Prerequisites = prerequisites;
            CanFly = canFly;
            GroupPath = groupPath;
            TypeId = typeId;
        }
    }

    /// <summary>
    /// Prerequisite information for a ship, including character's current level.
    /// </summary>
    public sealed class ShipPrerequisiteInfo
    {
        public string Name { get; }
        public long RequiredLevel { get; }
        public long CharacterLevel { get; }
        public bool IsMet { get; }
        public string DisplayText => $"{Name} {Skill.GetRomanFromInt(RequiredLevel)}";

        public ShipPrerequisiteInfo(string name, long requiredLevel, long characterLevel, bool isMet)
        {
            Name = name;
            RequiredLevel = requiredLevel;
            CharacterLevel = characterLevel;
            IsMet = isMet;
        }
    }
}
