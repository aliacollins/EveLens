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
using EveLens.Common.Helpers;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the plan skill browser tab. Shows all skills organized by group,
    /// with detail panel for the selected skill including training times and "Plan to" actions.
    /// </summary>
    public sealed class PlanSkillBrowserViewModel : CharacterViewModelBase
    {
        private readonly PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private bool _showAll = true;
        private SkillFilterMode _filterMode = SkillFilterMode.AllSkills;
        private AttributeCombo? _attributeFilter;
        private List<PlanSkillGroupEntry>? _allGroups;
        private List<PlanSkillGroupEntry> _groups = new();
        private StaticSkill? _selectedSkill;
        private SkillDetailInfo? _selectedSkillDetail;

        public PlanSkillBrowserViewModel(PlanEditorViewModel planEditor,
            IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor;
            Subscribe<PlanChangedEvent>(_ => RefreshPlannedLevels());
        }

        public PlanSkillBrowserViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            Subscribe<PlanChangedEvent>(_ => RefreshPlannedLevels());
        }

        public PlanSkillBrowserViewModel(PlanEditorViewModel planEditor)
            : base()
        {
            _planEditor = planEditor;
            Subscribe<PlanChangedEvent>(_ => RefreshPlannedLevels());
        }

        public PlanSkillBrowserViewModel()
        {
            Subscribe<PlanChangedEvent>(_ => RefreshPlannedLevels());
        }

        /// <summary>
        /// Gets the visible skill groups after filtering.
        /// </summary>
        public List<PlanSkillGroupEntry> Groups => _groups;

        /// <summary>
        /// Gets or sets the currently selected skill.
        /// </summary>
        public StaticSkill? SelectedSkill
        {
            get => _selectedSkill;
            set
            {
                if (SetProperty(ref _selectedSkill, value))
                    ComputeSkillDetail();
            }
        }

        /// <summary>
        /// Gets the computed detail for the selected skill.
        /// </summary>
        public SkillDetailInfo? SelectedSkillDetail
        {
            get => _selectedSkillDetail;
            private set => SetProperty(ref _selectedSkillDetail, value);
        }

        /// <summary>
        /// Gets or sets the text filter for skill names.
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
        /// Gets or sets whether to show all skills (true) or only untrained/partially trained (false).
        /// Legacy property — use FilterMode for new code.
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
        /// Gets or sets the skill filter mode: All Skills, Trained, Have Prerequisites, or Untrained.
        /// </summary>
        public SkillFilterMode FilterMode
        {
            get => _filterMode;
            set
            {
                if (_filterMode != value)
                {
                    _filterMode = value;
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets or sets the attribute combination filter. Null means no filtering (show all).
        /// </summary>
        public AttributeCombo? AttributeFilter
        {
            get => _attributeFilter;
            set
            {
                if (_attributeFilter != value)
                {
                    _attributeFilter = value;
                    ApplyFilter();
                }
            }
        }

        /// <summary>
        /// Gets the distinct attribute combinations present in the skill data.
        /// Populated after <see cref="Refresh"/> is called.
        /// </summary>
        public List<AttributeCombo> AvailableAttributeCombos { get; private set; } = new();

        /// <summary>
        /// Gets the detected attribute combo based on the character's two highest attributes.
        /// Null if no character is set or attributes are balanced.
        /// </summary>
        public AttributeCombo? DetectedRemap { get; private set; }

        /// <summary>
        /// Plans the given skill to the specified level.
        /// </summary>
        public void PlanToLevel(StaticSkill? skill, long level)
        {
            if (skill == null || _planEditor?.Plan == null)
                return;

            _planEditor.Plan.PlanTo(skill, level);
        }

        /// <summary>
        /// Selects a skill and computes its detail info.
        /// </summary>
        public void SelectSkill(StaticSkill? skill)
        {
            SelectedSkill = skill;
        }

        /// <summary>
        /// Rebuilds the skill groups and applies the current filter.
        /// </summary>
        public void Refresh()
        {
            BuildGroupData();
            ApplyFilter();
        }

        public void CollapseAll()
        {
            if (_allGroups != null)
                foreach (var g in _allGroups) g.IsExpanded = false;
        }

        public void ExpandAll()
        {
            if (_allGroups != null)
                foreach (var g in _allGroups) g.IsExpanded = true;
        }

        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Refresh();
        }

        private void BuildGroupData()
        {
            Character? character = Character;

            _allGroups = StaticSkills.AllGroups
                .Where(g => g.Any())
                .OrderBy(g => g.Name)
                .Select(g => new PlanSkillGroupEntry(g, character, _planEditor?.Plan))
                .Where(g => g.TotalCount > 0)
                .ToList();

            // Build distinct attribute combos from all published skills
            AvailableAttributeCombos = _allGroups
                .SelectMany(g => g.AllSkills)
                .Select(s => new AttributeCombo(s.StaticSkill.PrimaryAttribute, s.StaticSkill.SecondaryAttribute))
                .Distinct()
                .OrderBy(c => c.Primary)
                .ThenBy(c => c.Secondary)
                .ToList();

            // Detect the character's current remap from their two highest attributes
            DetectedRemap = DetectCharacterRemap(character);
        }

        private AttributeCombo? DetectCharacterRemap(Character? character)
        {
            if (character == null)
                return null;

            var attrs = new[]
            {
                EveAttribute.Intelligence,
                EveAttribute.Perception,
                EveAttribute.Charisma,
                EveAttribute.Willpower,
                EveAttribute.Memory
            };

            // Sort by effective value descending, take top 2
            var sorted = attrs
                .Select(a => (Attr: a, Value: character[a].EffectiveValue))
                .OrderByDescending(x => x.Value)
                .ToList();

            var top = sorted[0];
            var second = sorted[1];

            // Only flag a remap if there's a meaningful gap (not balanced/default 20-20-20-20-20)
            if (top.Value <= second.Value || second.Value <= sorted[2].Value)
                return null;

            // Match against available combos — primary is highest, secondary is second-highest
            return AvailableAttributeCombos.FirstOrDefault(c =>
                c.Primary == top.Attr && c.Secondary == second.Attr);
        }

        private void ApplyFilter()
        {
            if (_allGroups == null)
            {
                _groups = new List<PlanSkillGroupEntry>();
                OnPropertyChanged(nameof(Groups));
                return;
            }

            var visible = new List<PlanSkillGroupEntry>();

            foreach (var group in _allGroups)
            {
                group.UpdateVisibility(_showAll, _filterMode, _textFilter, _attributeFilter, Character, _planEditor?.Plan);

                if (group.VisibleSkills.Count > 0)
                    visible.Add(group);
            }

            _groups = visible;
            OnPropertyChanged(nameof(Groups));
        }

        private void RefreshPlannedLevels()
        {
            if (_allGroups != null)
            {
                foreach (var group in _allGroups)
                    group.UpdatePlannedLevels(Character, _planEditor?.Plan);
            }

            ComputeSkillDetail();
        }

        private void ComputeSkillDetail()
        {
            if (_selectedSkill == null)
            {
                SelectedSkillDetail = null;
                return;
            }

            SelectedSkillDetail = new SkillDetailInfo(_selectedSkill, Character, _planEditor?.Plan);
        }
    }

    /// <summary>
    /// Represents a skill group in the plan skill browser.
    /// </summary>
    public sealed class PlanSkillGroupEntry
    {
        private readonly List<PlanSkillEntry> _allSkills;

        public string Name { get; }
        public int TotalCount { get; }
        public int TrainedCount { get; private set; }
        public bool IsExpanded { get; set; }
        public List<PlanSkillEntry> VisibleSkills { get; private set; }
        internal IReadOnlyList<PlanSkillEntry> AllSkills => _allSkills;

        public string TrainedCountText => $"{TrainedCount} / {TotalCount}";

        public PlanSkillGroupEntry(StaticSkillGroup group, Character? character, Plan? plan)
        {
            Name = group.Name;
            _allSkills = group
                .Where(s => s.IsPublic)
                .OrderBy(s => s.Name)
                .Select(s => new PlanSkillEntry(s, character, plan))
                .ToList();
            TotalCount = _allSkills.Count;
            TrainedCount = _allSkills.Count(s => s.IsKnown);
            IsExpanded = false;
            VisibleSkills = new List<PlanSkillEntry>(_allSkills);
        }

        public void UpdateVisibility(bool showAll, SkillFilterMode filterMode, string filter,
            AttributeCombo? attributeFilter, Character? character, Plan? plan)
        {
            IEnumerable<PlanSkillEntry> filtered = _allSkills;

            // Legacy ShowAll toggle
            if (!showAll)
            {
                filtered = filtered.Where(s => !s.IsKnown || s.CharacterLevel < 5);
            }

            // New filter modes
            switch (filterMode)
            {
                case SkillFilterMode.Trained:
                    filtered = filtered.Where(s => s.IsKnown);
                    break;
                case SkillFilterMode.HavePrerequisites:
                    filtered = filtered.Where(s => !s.IsKnown && character != null &&
                        s.StaticSkill.Prerequisites.All(p => character.GetSkillLevel(p.Skill) >= p.Level));
                    break;
                case SkillFilterMode.Untrained:
                    filtered = filtered.Where(s => !s.IsKnown);
                    break;
            }

            if (!string.IsNullOrEmpty(filter))
            {
                filtered = filtered.Where(s =>
                    s.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));
            }

            if (attributeFilter != null)
            {
                filtered = filtered.Where(s =>
                    s.StaticSkill.PrimaryAttribute == attributeFilter.Primary &&
                    s.StaticSkill.SecondaryAttribute == attributeFilter.Secondary);
            }

            VisibleSkills = filtered.ToList();
        }

        public void UpdatePlannedLevels(Character? character, Plan? plan)
        {
            foreach (var skill in _allSkills)
                skill.UpdatePlannedLevel(character, plan);

            TrainedCount = _allSkills.Count(s => s.IsKnown);
        }
    }

    /// <summary>
    /// Represents a single skill entry in the plan skill browser.
    /// </summary>
    public sealed class PlanSkillEntry
    {
        public StaticSkill StaticSkill { get; }
        public string Name { get; }
        public long CharacterLevel { get; private set; }
        public long PlannedLevel { get; private set; }
        public bool IsKnown { get; private set; }
        public long Rank { get; }

        public string RankText => $"(Rank {Rank})";
        public string LevelText => IsKnown ? $"Level {Skill.GetRomanFromInt(CharacterLevel)}" : "Not trained";

        public PlanSkillEntry(StaticSkill staticSkill, Character? character, Plan? plan)
        {
            StaticSkill = staticSkill;
            Name = staticSkill.Name;
            Rank = staticSkill.Rank;
            UpdatePlannedLevel(character, plan);
        }

        public void UpdatePlannedLevel(Character? character, Plan? plan)
        {
            CharacterLevel = character?.GetSkillLevel(StaticSkill) ?? 0;
            IsKnown = CharacterLevel > 0;
            PlannedLevel = plan?.GetPlannedLevel(StaticSkill) ?? 0;
        }
    }

    /// <summary>
    /// Detailed information about a selected skill for the detail panel.
    /// </summary>
    public sealed class SkillDetailInfo
    {
        public string Name { get; }
        public string Description { get; }
        public long Rank { get; }
        public EveAttribute PrimaryAttribute { get; }
        public EveAttribute SecondaryAttribute { get; }
        public List<SkillLevelDetail> LevelDetails { get; }
        public List<SkillPrerequisiteInfo> Prerequisites { get; }

        public SkillDetailInfo(StaticSkill skill, Character? character, Plan? plan)
        {
            Name = skill.Name;
            Description = skill.Description;
            Rank = skill.Rank;
            PrimaryAttribute = skill.PrimaryAttribute;
            SecondaryAttribute = skill.SecondaryAttribute;

            long charLevel = character?.GetSkillLevel(skill) ?? 0;
            long plannedLevel = plan?.GetPlannedLevel(skill) ?? 0;

            // Build level details for I-V
            LevelDetails = new List<SkillLevelDetail>(5);
            CharacterScratchpad? scratchpad = null;
            if (character != null)
            {
                try
                {
                    scratchpad = new CharacterScratchpad(character);
                }
                catch
                {
                    // Scratchpad may fail without loaded game data in tests
                }
            }

            for (long level = 1; level <= 5; level++)
            {
                TimeSpan trainingTime = TimeSpan.Zero;
                if (scratchpad != null)
                {
                    try
                    {
                        var tempScratchpad = new CharacterScratchpad(character!);
                        tempScratchpad.Train(new StaticSkillLevel(skill, level));
                        trainingTime = tempScratchpad.TrainingTime;
                    }
                    catch
                    {
                        // Graceful fallback
                    }
                }

                LevelDetails.Add(new SkillLevelDetail(
                    level,
                    trainingTime,
                    level <= charLevel,
                    level <= plannedLevel,
                    level > charLevel));
            }

            // Build prerequisites
            Prerequisites = skill.Prerequisites
                .Select(p => new SkillPrerequisiteInfo(
                    p.Skill.Name,
                    p.Level,
                    character?.GetSkillLevel(p.Skill) ?? 0))
                .ToList();
        }
    }

    /// <summary>
    /// Training time and status for a single skill level (I-V).
    /// </summary>
    public sealed class SkillLevelDetail
    {
        public long Level { get; }
        public TimeSpan TrainingTime { get; }
        public bool IsTrained { get; }
        public bool IsPlanned { get; }
        public bool CanPlan { get; }

        public string LevelText => Skill.GetRomanFromInt(Level);
        public string TrainingTimeText => FormatTime(TrainingTime);

        public SkillLevelDetail(long level, TimeSpan trainingTime, bool isTrained, bool isPlanned, bool canPlan)
        {
            Level = level;
            TrainingTime = trainingTime;
            IsTrained = isTrained;
            IsPlanned = isPlanned;
            CanPlan = canPlan;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "Done";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }
    }

    /// <summary>
    /// Prerequisite skill with met/unmet status.
    /// </summary>
    public sealed class SkillPrerequisiteInfo
    {
        public string Name { get; }
        public long RequiredLevel { get; }
        public long CharacterLevel { get; }
        public bool IsMet { get; }

        public string DisplayText => $"{Name} {Skill.GetRomanFromInt(RequiredLevel)}";

        public SkillPrerequisiteInfo(string name, long requiredLevel, long characterLevel)
        {
            Name = name;
            RequiredLevel = requiredLevel;
            CharacterLevel = characterLevel;
            IsMet = characterLevel >= requiredLevel;
        }
    }

    /// <summary>
    /// Represents a primary/secondary attribute combination for skill filtering.
    /// </summary>
    public sealed class AttributeCombo : IEquatable<AttributeCombo>
    {
        public EveAttribute Primary { get; }
        public EveAttribute Secondary { get; }
        public string DisplayText { get; }

        public AttributeCombo(EveAttribute primary, EveAttribute secondary)
        {
            Primary = primary;
            Secondary = secondary;
            DisplayText = $"{primary} / {secondary}";
        }

        public bool Equals(AttributeCombo? other)
            => other != null && Primary == other.Primary && Secondary == other.Secondary;

        public override bool Equals(object? obj) => Equals(obj as AttributeCombo);
        public override int GetHashCode() => HashCode.Combine(Primary, Secondary);
        public override string ToString() => DisplayText;
    }

    /// <summary>
    /// Filter modes for the skill browser.
    /// </summary>
    public enum SkillFilterMode
    {
        AllSkills,
        Trained,
        HavePrerequisites,
        Untrained
    }
}
