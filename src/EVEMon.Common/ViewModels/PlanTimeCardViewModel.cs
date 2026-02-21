// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the plan time card showing training time estimates.
    /// Updated by <see cref="PlanDashboardViewModel"/>.
    /// </summary>
    public sealed class PlanTimeCardViewModel : ViewModelBase
    {
        private TimeSpan _totalTrainingTime;
        private TimeSpan _optimalTrainingTime;

        public PlanTimeCardViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
        }

        public PlanTimeCardViewModel()
        {
        }

        public TimeSpan TotalTrainingTime
        {
            get => _totalTrainingTime;
            set
            {
                if (SetProperty(ref _totalTrainingTime, value))
                {
                    OnPropertyChanged(nameof(TotalTrainingTimeText));
                    OnPropertyChanged(nameof(TimeSaved));
                    OnPropertyChanged(nameof(TimeSavedText));
                    OnPropertyChanged(nameof(HasOptimization));
                }
            }
        }

        /// <summary>
        /// Gets the optimal training time after attribute optimization.
        /// Placeholder: same as total for now.
        /// </summary>
        public TimeSpan OptimalTrainingTime
        {
            get => _optimalTrainingTime;
            set
            {
                if (SetProperty(ref _optimalTrainingTime, value))
                {
                    OnPropertyChanged(nameof(TimeSaved));
                    OnPropertyChanged(nameof(TimeSavedText));
                    OnPropertyChanged(nameof(HasOptimization));
                }
            }
        }

        public string TotalTrainingTimeText => FormatTrainingTime(_totalTrainingTime);

        public TimeSpan TimeSaved => _totalTrainingTime - _optimalTrainingTime;

        public string TimeSavedText => TimeSaved > TimeSpan.Zero
            ? FormatTrainingTime(TimeSaved)
            : string.Empty;

        public bool HasOptimization => TimeSaved > TimeSpan.Zero;

        /// <summary>
        /// Formats a training time into a compact human-readable string.
        /// </summary>
        internal static string FormatTrainingTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero)
                return "Done";
            if (time.TotalDays >= 1)
                return $"{(int)time.TotalDays}d {time.Hours}h {time.Minutes}m";
            if (time.TotalHours >= 1)
                return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }
    }
}
