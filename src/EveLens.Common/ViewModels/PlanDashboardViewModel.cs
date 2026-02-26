// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// Composes <see cref="PlanEditorViewModel"/> as a read-only dependency and computes
    /// summary data for the dashboard cards: goal, time, and cost.
    /// </summary>
    public sealed class PlanDashboardViewModel : ViewModelBase
    {
        private readonly PlanEditorViewModel _planEditor;

        private string _goalName = string.Empty;
        private int _skillsTrained;
        private int _skillsMissing;
        private int _totalSkills;
        private TimeSpan _totalTime;
        private long _booksCost;
        private long _notKnownBooksCost;

        public PlanDashboardViewModel(PlanEditorViewModel planEditor,
            IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            _planEditor = planEditor ?? throw new ArgumentNullException(nameof(planEditor));
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        public PlanDashboardViewModel(PlanEditorViewModel planEditor)
            : base()
        {
            _planEditor = planEditor ?? throw new ArgumentNullException(nameof(planEditor));
            Subscribe<PlanChangedEvent>(_ => Refresh());
        }

        public string GoalName
        {
            get => _goalName;
            private set => SetProperty(ref _goalName, value);
        }

        public int SkillsTrained
        {
            get => _skillsTrained;
            private set => SetProperty(ref _skillsTrained, value);
        }

        public int SkillsMissing
        {
            get => _skillsMissing;
            private set => SetProperty(ref _skillsMissing, value);
        }

        public int TotalSkills
        {
            get => _totalSkills;
            private set => SetProperty(ref _totalSkills, value);
        }

        public TimeSpan TotalTime
        {
            get => _totalTime;
            private set => SetProperty(ref _totalTime, value);
        }

        public long BooksCost
        {
            get => _booksCost;
            private set => SetProperty(ref _booksCost, value);
        }

        public long NotKnownBooksCost
        {
            get => _notKnownBooksCost;
            private set => SetProperty(ref _notKnownBooksCost, value);
        }

        /// <summary>
        /// Recomputes all summary values from the current plan state.
        /// </summary>
        public void Refresh()
        {
            if (_planEditor.Plan == null || _planEditor.DisplayPlan == null)
                return;

            GoalName = _planEditor.Plan.Name;

            var entries = _planEditor.DisplayPlan.ToArray();
            int trained = 0;
            int missing = 0;

            foreach (var entry in entries)
            {
                var charSkill = entry.CharacterSkill;
                if (charSkill != null && charSkill.Level >= entry.Level)
                    trained++;
                else
                    missing++;
            }

            SkillsTrained = trained;
            SkillsMissing = missing;
            TotalSkills = entries.Length;
            TotalTime = _planEditor.PlanStats.TrainingTime;
            BooksCost = _planEditor.PlanStats.BooksCost;
            NotKnownBooksCost = _planEditor.PlanStats.NotKnownBooksCost;
        }
    }
}
