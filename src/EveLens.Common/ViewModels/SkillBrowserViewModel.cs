// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the skill browser view showing skills organized by groups.
    /// </summary>
    public sealed class SkillBrowserViewModel : CharacterViewModelBase
    {
        private bool _showAll;
        private string _filter = string.Empty;
        private List<SkillBrowserGroupEntry>? _allGroups;
        private List<SkillBrowserGroupEntry> _visibleGroups = new();
        private int _totalTrained;
        private int _totalSkills;
        private long _totalSP;

        public SkillBrowserViewModel() : base()
        {
            SubscribeForCharacter<CharacterUpdatedEvent>(e => Rebuild());
            Subscribe<SettingsChangedEvent>(e => Rebuild());
        }

        public SkillBrowserViewModel(IEventAggregator agg, IDispatcher? disp = null)
            : base(agg, disp) { }

        /// <summary>
        /// Gets the list of skill groups currently visible after filtering.
        /// </summary>
        public List<SkillBrowserGroupEntry> VisibleGroups => _visibleGroups;

        /// <summary>
        /// Gets the total count of trained skills across all groups.
        /// </summary>
        public int TotalTrained => _totalTrained;

        /// <summary>
        /// Gets the total count of all skills (trained and untrained).
        /// </summary>
        public int TotalSkills => _totalSkills;

        /// <summary>
        /// Gets the total skill points across all trained skills.
        /// </summary>
        public long TotalSP => _totalSP;

        /// <summary>
        /// Gets or sets whether to show all skills (true) or only trained skills (false).
        /// </summary>
        public bool ShowAll
        {
            get => _showAll;
            set
            {
                if (_showAll != value)
                {
                    _showAll = value;
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text filter for skill names.
        /// </summary>
        public string Filter
        {
            get => _filter;
            set
            {
                var newFilter = value ?? string.Empty;
                if (_filter != newFilter)
                {
                    _filter = newFilter;
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Rebuilds the skill group data when the character changes.
        /// </summary>
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
        /// Builds the complete skill group list from the character.
        /// </summary>
        private void BuildGroupData()
        {
            if (Character == null)
            {
                _allGroups = null;
                return;
            }

            _allGroups = Character.SkillGroups
                .Where(g => g.Any()) // only groups with skills
                .OrderBy(g => g.Name)
                .Select(g => new SkillBrowserGroupEntry(g))
                .ToList();
        }

        /// <summary>
        /// Applies the current filter and visibility settings to update the visible groups.
        /// </summary>
        public void ApplyFilter()
        {
            if (_allGroups == null)
            {
                _visibleGroups = new List<SkillBrowserGroupEntry>();
                _totalTrained = 0;
                _totalSkills = 0;
                _totalSP = 0;
                return;
            }

            _totalTrained = 0;
            _totalSkills = 0;
            _totalSP = 0;

            var visible = new List<SkillBrowserGroupEntry>();

            foreach (var group in _allGroups)
            {
                group.UpdateVisibility(_showAll, _filter);

                if (group.VisibleSkills.Count > 0)
                    visible.Add(group);

                _totalTrained += group.TrainedCount;
                _totalSkills += group.TotalCount;
                _totalSP += group.TrainedSP;
            }

            _visibleGroups = visible;
        }

        /// <summary>
        /// Collapses all skill groups.
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
        /// Expands all skill groups.
        /// </summary>
        public void ExpandAll()
        {
            if (_allGroups != null)
            {
                foreach (var g in _allGroups)
                    g.IsExpanded = true;
            }
        }
    }

    /// <summary>
    /// Represents a skill group with its skills and statistics.
    /// </summary>
    public sealed class SkillBrowserGroupEntry
    {
        private readonly List<SkillBrowserSkillEntry> _allSkills;

        public string Name { get; }
        public int TotalCount { get; }
        public int TrainedCount { get; }
        public long TotalSP { get; }
        public long TrainedSP { get; }
        public bool IsExpanded { get; set; }
        public List<SkillBrowserSkillEntry> VisibleSkills { get; private set; }

        public string TrainedCountText => $"{TrainedCount} / {TotalCount} skills";
        public string SPText => $"{TrainedSP:N0} SP";

        public SkillBrowserGroupEntry(SkillGroup group)
        {
            Name = group.Name;
            // Filter out unpublished skills (CFO Training, Chief Science Officer, etc.)
            _allSkills = group.Where(s => s.IsPublic || s.IsKnown)
                .Select(s => new SkillBrowserSkillEntry(s)).ToList();
            TotalCount = _allSkills.Count;
            TrainedCount = _allSkills.Count(s => s.IsKnown);
            TotalSP = group.TotalSP;
            TrainedSP = _allSkills.Where(s => s.IsKnown).Sum(s => s.SkillPoints);
            IsExpanded = TrainedCount > 0;
            VisibleSkills = _allSkills.Where(s => s.IsKnown).ToList();
        }

        public void UpdateVisibility(bool showAll, string filter)
        {
            var filtered = showAll ? _allSkills : _allSkills.Where(s => s.IsKnown);

            if (!string.IsNullOrEmpty(filter))
                filtered = filtered.Where(s =>
                    s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

            VisibleSkills = filtered.ToList();
        }
    }

    /// <summary>
    /// Represents a single skill with its training status and level information.
    /// </summary>
    public sealed class SkillBrowserSkillEntry
    {
        public string Name { get; }
        public long Level { get; }
        public long SkillPoints { get; }
        public long Rank { get; }
        public bool IsKnown { get; }
        public bool IsTraining { get; }

        public string RankText => $"(Rank {Rank})";
        public string SPText => IsKnown ? $"{SkillPoints:N0} SP" : "";

        public SkillBrowserSkillEntry(Skill skill)
        {
            Name = skill.Name;
            Level = skill.LastConfirmedLvl;
            SkillPoints = skill.SkillPoints;
            Rank = skill.Rank;
            IsKnown = skill.IsKnown;
            IsTraining = skill.IsTraining;
        }
    }
}
