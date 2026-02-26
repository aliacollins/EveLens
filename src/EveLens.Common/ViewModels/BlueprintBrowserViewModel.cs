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
    /// </summary>
    public sealed class BlueprintBrowserViewModel : CharacterViewModelBase
    {
        private readonly PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private List<BlueprintGroupEntry> _groups = new();
        private List<BlueprintGroupEntry>? _allGroups;
        private Blueprint? _selectedBlueprint;
        private BlueprintDetailInfo? _selectedBlueprintDetail;

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
        /// Gets the list of blueprint groups currently visible after filtering.
        /// </summary>
        public List<BlueprintGroupEntry> Groups => _groups;

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
        /// Called when the character changes. Rebuilds the group data.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Refresh();
        }

        /// <summary>
        /// Rebuilds the blueprint groups from static data and applies the current filter.
        /// </summary>
        public void Refresh()
        {
            BuildGroupData();
            ApplyFilter();

            // Recompute detail if a blueprint is selected (character may have changed)
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
        /// Collapses all blueprint groups.
        /// </summary>
        public void CollapseAll()
        {
            if (_allGroups != null)
            {
                foreach (var g in _allGroups)
                    g.IsExpanded = false;
            }
        }

        /// <summary>
        /// Expands all blueprint groups.
        /// </summary>
        public void ExpandAll()
        {
            if (_allGroups != null)
            {
                foreach (var g in _allGroups)
                    g.IsExpanded = true;
            }
        }

        private void BuildGroupData()
        {
            var marketGroups = StaticBlueprints.BlueprintMarketGroups;
            if (marketGroups == null)
            {
                _allGroups = null;
                return;
            }

            var groups = new List<BlueprintGroupEntry>();
            CollectGroups(marketGroups, groups);

            _allGroups = groups
                .Where(g => g.AllBlueprints.Count > 0)
                .OrderBy(g => g.Name)
                .ToList();
        }

        private static void CollectGroups(BlueprintMarketGroupCollection collection, List<BlueprintGroupEntry> result)
        {
            foreach (var group in collection)
            {
                // If this group has blueprints directly, add it as an entry
                if (group.Blueprints.Any())
                {
                    var entries = new List<BlueprintEntry>();
                    foreach (var bp in group.Blueprints)
                    {
                        entries.Add(new BlueprintEntry(bp));
                    }

                    result.Add(new BlueprintGroupEntry(
                        group.Name,
                        entries.OrderBy(e => e.Name).ToList()));
                }

                // Recurse into subgroups
                if (group.SubGroups.Any())
                    CollectGroups(group.SubGroups, result);
            }
        }

        private void ApplyFilter()
        {
            if (_allGroups == null)
            {
                _groups = new List<BlueprintGroupEntry>();
                return;
            }

            var visible = new List<BlueprintGroupEntry>();

            foreach (var group in _allGroups)
            {
                if (string.IsNullOrEmpty(_textFilter))
                {
                    group.VisibleBlueprints = group.AllBlueprints;
                }
                else
                {
                    group.VisibleBlueprints = group.AllBlueprints
                        .Where(b => b.Name.Contains(_textFilter, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }

                if (group.VisibleBlueprints.Count > 0)
                    visible.Add(group);
            }

            _groups = visible;
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

        public sealed class BlueprintGroupEntry
        {
            public string Name { get; }
            public List<BlueprintEntry> AllBlueprints { get; }
            public List<BlueprintEntry> VisibleBlueprints { get; set; }
            public bool IsExpanded { get; set; }

            public string CountText => $"{VisibleBlueprints.Count} blueprints";

            public BlueprintGroupEntry(string name, List<BlueprintEntry> allBlueprints)
            {
                Name = name;
                AllBlueprints = allBlueprints;
                VisibleBlueprints = allBlueprints;
                IsExpanded = false;
            }
        }

        public sealed class BlueprintEntry
        {
            public Blueprint Blueprint { get; }
            public string Name { get; }
            public string ProducesItemName { get; }

            public BlueprintEntry(Blueprint blueprint)
            {
                Blueprint = blueprint;
                Name = blueprint.Name;
                ProducesItemName = blueprint.ProducesItem?.Name ?? string.Empty;
            }
        }

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
