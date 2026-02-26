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
    /// ViewModel for the skill queue view showing queued skills with their training status.
    /// </summary>
    public sealed class SkillQueueViewModel : CharacterViewModelBase
    {
        private List<SkillQueueEntry> _queueEntries = new();
        private int _trainingCount;
        private string _currentTrainingText = string.Empty;

        public SkillQueueViewModel() : base()
        {
            SubscribeForCharacter<CharacterUpdatedEvent>(e => Reload());
            Subscribe<SettingsChangedEvent>(e => Reload());
        }

        public SkillQueueViewModel(IEventAggregator agg, IDispatcher? disp = null)
            : base(agg, disp) { }

        /// <summary>
        /// Gets the list of skills in the training queue.
        /// </summary>
        public List<SkillQueueEntry> QueueEntries => _queueEntries;

        /// <summary>
        /// Gets the count of skills currently training.
        /// </summary>
        public int TrainingCount => _trainingCount;

        /// <summary>
        /// Gets the text description of the currently training skill.
        /// </summary>
        public string CurrentTrainingText => _currentTrainingText;

        /// <summary>
        /// Gets whether the queue has any entries.
        /// </summary>
        public bool HasEntries => _queueEntries.Count > 0;

        /// <summary>
        /// Rebuilds the queue data when the character changes.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Reload();
        }

        private void Reload() => BuildQueue();

        /// <summary>
        /// Builds the queue entry list from the character's skill queue.
        /// </summary>
        private void BuildQueue()
        {
            if (Character is not CCPCharacter ccp)
            {
                _queueEntries = new List<SkillQueueEntry>();
                _trainingCount = 0;
                _currentTrainingText = string.Empty;
                return;
            }

            _queueEntries = ccp.SkillQueue
                .Select(q => new SkillQueueEntry(q))
                .ToList();

            _trainingCount = _queueEntries.Count(e => e.IsTraining);

            if (_trainingCount > 0)
            {
                var training = _queueEntries.FirstOrDefault(e => e.IsTraining);
                _currentTrainingText = training?.SkillText ?? string.Empty;
            }
            else
            {
                _currentTrainingText = string.Empty;
            }
        }
    }

    /// <summary>
    /// Represents a single skill in the training queue with its status and progress.
    /// </summary>
    public sealed class SkillQueueEntry
    {
        public string SkillText { get; }
        public string RankText { get; }
        public string StatusText { get; }
        public string TimeText { get; }
        public double Progress { get; }
        public bool IsTraining { get; }
        public bool IsCompleted { get; }
        public bool IsQueued { get; }

        public SkillQueueEntry(QueuedSkill q)
        {
            SkillText = $"{q.SkillName} {ToRoman(q.Level)}";
            RankText = $"×{q.Rank}";
            IsTraining = q.IsTraining;
            IsCompleted = q.IsCompleted;
            Progress = q.FractionCompleted * 100.0;

            if (q.IsCompleted)
            {
                StatusText = "Completed";
                TimeText = "";
                IsQueued = false;
            }
            else if (q.IsTraining)
            {
                var rem = q.RemainingTime;
                string timeStr = rem.TotalDays >= 1 ? $"{(int)rem.TotalDays}d {rem.Hours}h {rem.Minutes}m"
                    : rem.TotalHours >= 1 ? $"{(int)rem.TotalHours}h {rem.Minutes}m"
                    : $"{rem.Minutes}m {rem.Seconds}s";
                StatusText = "Training";
                TimeText = $"{timeStr} remaining — ends {q.EndTime:dd MMM HH:mm}";
                IsQueued = false;
            }
            else
            {
                StatusText = "Queued";
                TimeText = q.EndTime > DateTime.MinValue ? $"Starts {q.StartTime:dd MMM HH:mm}" : "";
                IsQueued = true;
            }
        }

        private static string ToRoman(int level) => level switch
        {
            1 => "I",
            2 => "II",
            3 => "III",
            4 => "IV",
            5 => "V",
            _ => level.ToString()
        };
    }
}
