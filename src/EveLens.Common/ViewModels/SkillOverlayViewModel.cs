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
    /// Immutable per-skill state extracted from a character. Avoids touching the live
    /// <see cref="Skill"/> model objects during rendering.
    /// </summary>
    internal readonly struct SkillState
    {
        public byte Level { get; init; }
        public long SkillPoints { get; init; }
        public bool IsKnown { get; init; }
        public bool IsTraining { get; init; }
    }

    /// <summary>
    /// Immutable template for a single skill, built once from <see cref="StaticSkill"/> data.
    /// Shared across all characters. Contains only SDE-sourced display data.
    /// </summary>
    internal sealed class SkillEntryTemplate
    {
        public int SkillId { get; }
        public string Name { get; }
        public long Rank { get; }
        public string RankText { get; }
        public bool IsPublic { get; }

        internal SkillEntryTemplate(StaticSkill skill)
        {
            SkillId = skill.ID;
            Name = skill.Name;
            Rank = skill.Rank;
            RankText = $"Rank {skill.Rank}";
            IsPublic = skill.IsPublic;
        }
    }

    /// <summary>
    /// Immutable template for a skill group, built once from <see cref="StaticSkillGroup"/> data.
    /// Contains a sorted list of <see cref="SkillEntryTemplate"/> entries.
    /// </summary>
    internal sealed class SkillGroupTemplate
    {
        public int GroupId { get; }
        public string Name { get; }
        public IReadOnlyList<SkillEntryTemplate> Skills { get; }

        internal SkillGroupTemplate(StaticSkillGroup group)
        {
            GroupId = group.ID;
            Name = group.Name;
            Skills = group.OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase)
                         .Select(s => new SkillEntryTemplate(s))
                         .ToList();
        }
    }

    /// <summary>
    /// One-time snapshot of the full static skill hierarchy, built from <see cref="StaticSkills"/>.
    /// Shared by all characters. Lazy-initialized; returns an empty list if static data has
    /// not been loaded yet.
    /// </summary>
    internal sealed class SkillTemplate
    {
        private static readonly Lazy<SkillTemplate> s_instance = new(Build);

        public static SkillTemplate Instance => s_instance.Value;

        public IReadOnlyList<SkillGroupTemplate> Groups { get; }

        private SkillTemplate(IReadOnlyList<SkillGroupTemplate> groups)
        {
            Groups = groups;
        }

        private static SkillTemplate Build()
        {
            var allGroups = StaticSkills.AllGroups;
            if (allGroups == null)
                return new SkillTemplate(Array.Empty<SkillGroupTemplate>());

            var groups = allGroups
                .Where(g => g.Count() > 0)
                .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
                .Select(g => new SkillGroupTemplate(g))
                .ToList();

            return new SkillTemplate(groups);
        }
    }

    /// <summary>
    /// Per-character overlay containing only the mutable skill data (level, SP, known, training).
    /// Keyed by <see cref="StaticSkill.ID"/>. Designed for near-zero allocation on tab switch
    /// when the character data has not changed — call <see cref="Update"/> to refresh.
    /// </summary>
    internal sealed class SkillOverlay
    {
        private readonly Dictionary<int, SkillState> _states = new();

        public int TotalTrained { get; private set; }
        public int TotalSkills { get; private set; }
        public int TotalPublicSkills { get; private set; }
        public long TotalSP { get; private set; }

        /// <summary>
        /// Skills at each level (index 0 = injected but untrained, 1-5 = trained to that level).
        /// </summary>
        public int[] SkillsAtLevel { get; } = new int[6];

        public SkillState GetState(int skillId)
            => _states.TryGetValue(skillId, out var s) ? s : default;

        public void Update(Character character)
        {
            _states.Clear();
            int trained = 0;
            int total = 0;
            int totalPublic = 0;
            long sp = 0L;
            Array.Clear(SkillsAtLevel, 0, 6);

            foreach (var group in character.SkillGroups)
            {
                foreach (Skill skill in group)
                {
                    total++;
                    if (skill.IsPublic)
                        totalPublic++;

                    var state = new SkillState
                    {
                        Level = (byte)Math.Min(skill.LastConfirmedLvl, 5),
                        SkillPoints = skill.SkillPoints,
                        IsKnown = skill.IsKnown,
                        IsTraining = skill.IsTraining
                    };

                    _states[skill.ID] = state;

                    if (state.IsKnown)
                    {
                        trained++;
                        sp += state.SkillPoints;
                        SkillsAtLevel[state.Level]++;
                    }
                }
            }

            TotalTrained = trained;
            TotalSkills = total;
            TotalPublicSkills = totalPublic;
            TotalSP = sp;
        }
    }

    /// <summary>
    /// ViewModel for the skill browser that uses a template+overlay architecture for
    /// near-zero-allocation character tab switching. The static <see cref="SkillTemplate"/>
    /// is built once from game data and shared by all characters; the per-character
    /// <see cref="SkillOverlay"/> stores only level/SP/known/training state.
    /// Exposes a <see cref="FlattenedTreeSource{T}"/> for virtualized rendering.
    /// </summary>
    public sealed class SkillOverlayViewModel : CharacterViewModelBase
    {
        private const string ViewName = "Skills";
        private static readonly SkillTemplate s_template = SkillTemplate.Instance;

        private readonly Dictionary<long, SkillOverlay> _overlays = new();
        private SkillOverlay? _activeOverlay;
        private bool _showAll;
        private string _filter = string.Empty;
        private int _levelFilter = -1;
        private bool _isRebuilding;

        public FlattenedTreeSource<object> TreeSource { get; } = new();

        public int TotalTrained => _activeOverlay?.TotalTrained ?? 0;

        public int TotalSkills => _activeOverlay?.TotalSkills ?? 0;

        public int TotalPublicSkills => _activeOverlay?.TotalPublicSkills ?? 0;

        public long TotalSP => _activeOverlay?.TotalSP ?? 0;

        /// <summary>
        /// Gets the number of skills at the given level (0 = injected/untrained, 1-5).
        /// </summary>
        public int GetSkillsAtLevel(int level)
            => _activeOverlay?.SkillsAtLevel is { } arr && level >= 0 && level < arr.Length
                ? arr[level] : 0;

        public string StatusText => _activeOverlay != null
            ? $"Trained: {_activeOverlay.TotalTrained} of {_activeOverlay.TotalPublicSkills} skills  |  Total SP: {_activeOverlay.TotalSP:N0}"
            : "";

        internal SkillState GetSkillState(int skillId)
            => _activeOverlay?.GetState(skillId) ?? default;

        public SkillOverlayViewModel() : base()
        {
            SubscribeForCharacter<CharacterUpdatedEvent>(e => RefreshOverlay());
            TreeSource.Changed += OnTreeSourceChanged;
        }

        public SkillOverlayViewModel(IEventAggregator agg, IDispatcher? disp = null)
            : base(agg, disp) { }

        public bool ShowAll
        {
            get => _showAll;
            set
            {
                if (_showAll != value)
                {
                    _showAll = value;
                    RebuildTree();
                    OnPropertyChanged(nameof(ShowAll));
                }
            }
        }

        public string Filter
        {
            get => _filter;
            set
            {
                var newFilter = value ?? string.Empty;
                if (_filter != newFilter)
                {
                    _filter = newFilter;
                    RebuildTree();
                    OnPropertyChanged(nameof(Filter));
                }
            }
        }

        /// <summary>
        /// Gets or sets the level filter. -1 means no filter (all levels).
        /// 0 = injected/untrained, 1-5 = that level.
        /// </summary>
        public int LevelFilter
        {
            get => _levelFilter;
            set
            {
                if (_levelFilter != value)
                {
                    _levelFilter = value;
                    RebuildTree();
                    OnPropertyChanged(nameof(LevelFilter));
                }
            }
        }

        public void CollapseAll() => TreeSource.CollapseAll();

        public void ExpandAll() => TreeSource.ExpandAll();

        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();

            if (Character == null)
            {
                _activeOverlay = null;
                TreeSource.SetData(Array.Empty<GroupData<object>>());
                NotifyOverlayChanged();
                return;
            }

            var charId = Character.CharacterID;
            if (!_overlays.TryGetValue(charId, out var overlay))
            {
                overlay = new SkillOverlay();
                _overlays[charId] = overlay;
            }

            overlay.Update(Character);
            _activeOverlay = overlay;

            // Load persisted expand state for this character.
            // If never saved, auto-expand groups with trained skills.
            // HasSavedState distinguishes "never saved" from "saved as all-collapsed".
            var expandState = CollapseStateHelper.LoadExpandState(charId, ViewName);
            if (!CollapseStateHelper.HasSavedState(charId, ViewName) && _activeOverlay != null)
            {
                // First time viewing — expand groups that have trained skills
                foreach (var group in s_template.Groups)
                {
                    bool hasTrained = group.Skills.Any(s => _activeOverlay.GetState(s.SkillId).IsKnown);
                    if (hasTrained)
                        expandState.Add(group.Name);
                }
            }
            TreeSource.SetExpandState(expandState);

            RebuildTree();
            NotifyOverlayChanged();
        }

        private void RefreshOverlay()
        {
            if (Character == null || _activeOverlay == null)
                return;

            _activeOverlay.Update(Character);
            RebuildTree();
            NotifyOverlayChanged();
        }

        private void NotifyOverlayChanged()
        {
            OnPropertyChanged(nameof(TotalTrained));
            OnPropertyChanged(nameof(TotalSkills));
            OnPropertyChanged(nameof(TotalSP));
            OnPropertyChanged(nameof(StatusText));
        }

        private void OnTreeSourceChanged()
        {
            if (_isRebuilding || Character == null)
                return;

            // User toggled expand/collapse — persist the state
            CollapseStateHelper.SaveExpandState(Character.CharacterID, ViewName, TreeSource.GetExpandState());
        }

        private void RebuildTree()
        {
            if (_activeOverlay == null)
            {
                TreeSource.SetData(Array.Empty<GroupData<object>>());
                return;
            }

            var groups = new List<GroupData<object>>();

            foreach (var group in s_template.Groups)
            {
                var visibleSkills = new List<object>();

                foreach (var skill in group.Skills)
                {
                    if (ShouldShow(skill))
                        visibleSkills.Add(skill);
                }

                if (visibleSkills.Count > 0)
                    groups.Add(new GroupData<object>(group.Name, group, visibleSkills));
            }

            _isRebuilding = true;
            try
            {
                TreeSource.SetData(groups);
            }
            finally
            {
                _isRebuilding = false;
            }
        }

        private bool ShouldShow(SkillEntryTemplate skill)
        {
            var state = _activeOverlay?.GetState(skill.SkillId) ?? default;

            // Text filter always applies
            if (_filter.Length > 0 &&
                !skill.Name.Contains(_filter, StringComparison.OrdinalIgnoreCase))
                return false;

            // "All Skills" — show every published skill
            if (_levelFilter == -3)
                return skill.IsPublic;

            // "Untrained" — published skills the character hasn't injected
            if (_levelFilter == -2)
                return skill.IsPublic && !state.IsKnown;

            if (_levelFilter >= 0)
            {
                if (_levelFilter == 0)
                    return state.IsKnown && state.Level == 0;
                return state.IsKnown && state.Level == _levelFilter;
            }

            // Default (-1 = "All Trained"): respect "trained only" toggle
            if (!_showAll && !state.IsKnown)
                return false;

            return true;
        }
    }
}
