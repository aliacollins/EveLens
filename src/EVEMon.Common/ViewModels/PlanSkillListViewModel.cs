// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel that groups plan entries by training status:
    /// currently training, missing skills, and trained/completed skills.
    /// </summary>
    public sealed class PlanSkillListViewModel : ViewModelBase
    {
        private PlanEditorViewModel? _planEditor;
        private string _textFilter = string.Empty;
        private List<PlanEntry> _trainingEntries = new();
        private List<PlanEntry> _missingEntries = new();
        private List<PlanEntry> _trainedEntries = new();
        private int _trainingCount;
        private int _missingCount;
        private int _trainedCount;
        private TimeSpan _trainingTimeTotal;

        public PlanSkillListViewModel(PlanEditorViewModel planEditor,
            IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor;
        }

        public PlanSkillListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanSkillListViewModel()
        {
        }

        public PlanEditorViewModel? PlanEditor
        {
            get => _planEditor;
            set => SetProperty(ref _planEditor, value);
        }

        public string TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    Refresh();
            }
        }

        public List<PlanEntry> TrainingEntries
        {
            get => _trainingEntries;
            private set => SetProperty(ref _trainingEntries, value);
        }

        public List<PlanEntry> MissingEntries
        {
            get => _missingEntries;
            private set => SetProperty(ref _missingEntries, value);
        }

        public List<PlanEntry> TrainedEntries
        {
            get => _trainedEntries;
            private set => SetProperty(ref _trainedEntries, value);
        }

        public int TrainingCount
        {
            get => _trainingCount;
            private set => SetProperty(ref _trainingCount, value);
        }

        public int MissingCount
        {
            get => _missingCount;
            private set => SetProperty(ref _missingCount, value);
        }

        public int TrainedCount
        {
            get => _trainedCount;
            private set => SetProperty(ref _trainedCount, value);
        }

        public TimeSpan TrainingTimeTotal
        {
            get => _trainingTimeTotal;
            private set => SetProperty(ref _trainingTimeTotal, value);
        }

        /// <summary>
        /// Rebuilds the categorized entry lists from the current display plan.
        /// </summary>
        public void Refresh()
        {
            if (_planEditor?.DisplayPlan == null)
                return;

            var entries = _planEditor.DisplayPlan.ToArray();
            var now = DateTime.UtcNow;

            var training = new List<PlanEntry>();
            var missing = new List<PlanEntry>();
            var trained = new List<PlanEntry>();

            foreach (var entry in entries)
            {
                if (IsCurrentlyTraining(entry, now))
                    training.Add(entry);
                else if (IsTrained(entry))
                    trained.Add(entry);
                else
                    missing.Add(entry);
            }

            if (!string.IsNullOrEmpty(_textFilter))
            {
                training = training.Where(e => MatchesFilter(e)).ToList();
                missing = missing.Where(e => MatchesFilter(e)).ToList();
                trained = trained.Where(e => MatchesFilter(e)).ToList();
            }

            TrainingEntries = training;
            MissingEntries = missing;
            TrainedEntries = trained;
            TrainingCount = training.Count;
            MissingCount = missing.Count;
            TrainedCount = trained.Count;
            TrainingTimeTotal = entries.Aggregate(TimeSpan.Zero, (sum, e) => sum + e.TrainingTime);
        }

        private static bool IsCurrentlyTraining(PlanEntry entry, DateTime now)
            => entry.FractionCompleted > 0 && entry.StartTime <= now && entry.EndTime >= now;

        private static bool IsTrained(PlanEntry entry)
        {
            var charSkill = entry.CharacterSkill;
            return charSkill != null && charSkill.Level >= entry.Level;
        }

        private bool MatchesFilter(PlanEntry entry)
            => entry.Skill.Name.Contains(_textFilter, StringComparison.OrdinalIgnoreCase);
    }
}
