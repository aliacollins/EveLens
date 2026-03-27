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
    /// ViewModel for the blueprint browser view showing blueprints organized by market groups.
    /// Uses <see cref="BrowserTreeNode"/> for hierarchical display (same pattern as Ship/Item browsers).
    /// </summary>
    public sealed class BlueprintBrowserViewModel : CharacterViewModelBase
    {
        private readonly PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private bool _showCanBuildOnly;
        private Blueprint? _selectedBlueprint;
        private BlueprintDetailInfo? _selectedBlueprintDetail;
        private List<BrowserTreeNode> _topLevelNodes = new();
        private List<BrowserTreeNode> _flattenedNodes = new();

        public BlueprintBrowserViewModel() : base()
        {
            SubscribeEvents();
        }

        public BlueprintBrowserViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            SubscribeEvents();
        }

        public BlueprintBrowserViewModel(PlanEditorViewModel planEditor) : base()
        {
            _planEditor = planEditor;
            SubscribeEvents();
        }

        public BlueprintBrowserViewModel(PlanEditorViewModel planEditor, IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor;
            SubscribeEvents();
        }

        private void SubscribeEvents()
        {
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        /// <summary>
        /// Gets the flattened list of visible tree nodes (for binding to a flat ItemsControl).
        /// </summary>
        public List<BrowserTreeNode> FlattenedNodes => _flattenedNodes;

        /// <summary>
        /// Gets or sets the currently selected blueprint.
        /// </summary>
        public Blueprint? SelectedBlueprint
        {
            get => _selectedBlueprint;
            private set => SetProperty(ref _selectedBlueprint, value);
        }

        /// <summary>
        /// Gets or sets the detail info for the selected blueprint.
        /// </summary>
        public BlueprintDetailInfo? SelectedBlueprintDetail
        {
            get => _selectedBlueprintDetail;
            private set => SetProperty(ref _selectedBlueprintDetail, value);
        }

        /// <summary>
        /// Gets or sets the text filter for blueprint names.
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
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets or sets whether to show only blueprints the character can build.
        /// </summary>
        public bool ShowCanBuildOnly
        {
            get => _showCanBuildOnly;
            set
            {
                if (_showCanBuildOnly != value)
                {
                    _showCanBuildOnly = value;
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Called when the character changes. Rebuilds the tree.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Refresh();
        }

        /// <summary>
        /// Rebuilds the blueprint tree from static data and applies the current filter.
        /// </summary>
        public void Refresh()
        {
            BuildTree();
            ApplyFilter();

            if (_selectedBlueprint != null)
                SelectBlueprint(_selectedBlueprint);
        }

        /// <summary>
        /// Selects a blueprint and computes its detail information.
        /// </summary>
        public void SelectBlueprint(Blueprint? blueprint)
        {
            SelectedBlueprint = blueprint;

            if (blueprint == null)
            {
                SelectedBlueprintDetail = null;
                return;
            }

            var prerequisites = new List<BlueprintPrerequisiteInfo>();
            foreach (var prereq in blueprint.Prerequisites)
            {
                long characterLevel = 0;
                if (Character is Character ch)
                {
                    var skill = ch.Skills[prereq.Skill.ID];
                    if (skill != null)
                        characterLevel = skill.LastConfirmedLvl;
                }

                prerequisites.Add(new BlueprintPrerequisiteInfo(
                    prereq.Skill.Name,
                    prereq.Level,
                    characterLevel));
            }

            var materials = blueprint.MaterialRequirements
                .Where(m => m.Activity == BlueprintActivity.Manufacturing)
                .Select(m => new MaterialInfo(m.Name, m.Quantity))
                .ToList();

            string productionTimeText = FormatProductionTime(blueprint.ProductionTime);

            bool canBuild = prerequisites.Count == 0 || prerequisites.All(p => p.IsMet);

            SelectedBlueprintDetail = new BlueprintDetailInfo(
                blueprint.Name,
                blueprint.Description,
                blueprint.ProducesItem?.Name ?? string.Empty,
                productionTimeText,
                prerequisites,
                materials,
                canBuild);
        }

        /// <summary>
        /// Plans the prerequisite skills for a blueprint that the character has not yet trained.
        /// </summary>
        public void PlanToBuild(Blueprint? blueprint)
        {
            if (blueprint == null)
                return;

            var plan = _planEditor?.Plan;
            if (plan == null)
                return;

            foreach (var prereq in blueprint.Prerequisites)
            {
                plan.PlanTo(prereq.Skill, prereq.Level);
            }
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

        /// <summary>
        /// Collapses all blueprint groups.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var node in _topLevelNodes)
                node.SetExpandedAll(false);
            UpdateFlattenedNodes();
        }

        /// <summary>
        /// Expands all blueprint groups.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var node in _topLevelNodes)
                node.SetExpandedAll(true);
            UpdateFlattenedNodes();
        }

        private void BuildTree()
        {
            var marketGroups = StaticBlueprints.BlueprintMarketGroups;
            if (marketGroups == null)
            {
                _topLevelNodes = new List<BrowserTreeNode>();
                return;
            }

            var character = Character as Character;

            var nodes = new List<BrowserTreeNode>();
            foreach (var group in marketGroups.OrderBy(g => g.Name))
            {
                var node = BrowserTreeNode.FromBlueprintMarketGroup(group, 0, character);
                if (node.TotalLeafCount > 0)
                    nodes.Add(node);
            }

            _topLevelNodes = nodes;
        }

        private void ApplyFilter()
        {
            if (_topLevelNodes.Count == 0)
            {
                _flattenedNodes = new List<BrowserTreeNode>();
                return;
            }

            foreach (var node in _topLevelNodes)
                node.ApplyFilter(_textFilter, _showCanBuildOnly);

            UpdateFlattenedNodes();
        }

        private void UpdateFlattenedNodes()
        {
            _flattenedNodes = BrowserTreeNode.FlattenVisible(
                _topLevelNodes.Where(n => n.VisibleChildren.Count > 0 || n.IsLeaf));
        }

        private static string FormatProductionTime(double seconds)
        {
            if (seconds <= 0)
                return "Instant";

            var ts = TimeSpan.FromSeconds(seconds);

            if (ts.TotalDays >= 1)
                return $"{(int)ts.TotalDays}d {ts.Hours}h {ts.Minutes}m {ts.Seconds}s";

            if (ts.TotalHours >= 1)
                return $"{ts.Hours}h {ts.Minutes}m {ts.Seconds}s";

            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}m {ts.Seconds}s";

            return $"{ts.Seconds}s";
        }

        #region Nested Types

        public sealed class BlueprintDetailInfo
        {
            public string Name { get; }
            public string Description { get; }
            public string ProducesItemName { get; }
            public string ProductionTimeText { get; }
            public List<BlueprintPrerequisiteInfo> Prerequisites { get; }
            public List<MaterialInfo> Materials { get; }
            public bool CanBuild { get; }

            public BlueprintDetailInfo(
                string name,
                string description,
                string producesItemName,
                string productionTimeText,
                List<BlueprintPrerequisiteInfo> prerequisites,
                List<MaterialInfo> materials,
                bool canBuild)
            {
                Name = name;
                Description = description;
                ProducesItemName = producesItemName;
                ProductionTimeText = productionTimeText;
                Prerequisites = prerequisites;
                Materials = materials;
                CanBuild = canBuild;
            }
        }

        public sealed class MaterialInfo
        {
            public string Name { get; }
            public long Quantity { get; }
            public string QuantityText => $"{Quantity:N0}";

            public MaterialInfo(string name, long quantity)
            {
                Name = name;
                Quantity = quantity;
            }
        }

        public sealed class BlueprintPrerequisiteInfo
        {
            public string Name { get; }
            public long RequiredLevel { get; }
            public long CharacterLevel { get; }
            public bool IsMet { get; }
            public string DisplayText => $"{Name} {Skill.GetRomanFromInt(RequiredLevel)}";

            public BlueprintPrerequisiteInfo(string name, long requiredLevel, long characterLevel)
            {
                Name = name;
                RequiredLevel = requiredLevel;
                CharacterLevel = characterLevel;
                IsMet = characterLevel >= requiredLevel;
            }
        }

        #endregion
    }
}
