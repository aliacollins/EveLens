using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the employment history timeline showing corporations and their durations.
    /// </summary>
    public sealed class EmploymentTimelineViewModel : CharacterViewModelBase
    {
        private List<EmploymentTimelineEntry> _timelineEntries = new();
        private int _corporationCount;

        public EmploymentTimelineViewModel() : base() { }

        public EmploymentTimelineViewModel(IEventAggregator agg, IDispatcher? disp = null)
            : base(agg, disp) { }

        /// <summary>
        /// Gets the list of employment timeline entries (newest first).
        /// </summary>
        public List<EmploymentTimelineEntry> TimelineEntries => _timelineEntries;

        /// <summary>
        /// Gets the total number of corporations in the history.
        /// </summary>
        public int CorporationCount => _corporationCount;

        /// <summary>
        /// Rebuilds the timeline when the character changes.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            BuildTimeline();
        }

        /// <summary>
        /// Builds the timeline entries from the character's employment history.
        /// </summary>
        private void BuildTimeline()
        {
            if (Character == null)
            {
                _timelineEntries = new List<EmploymentTimelineEntry>();
                _corporationCount = 0;
                return;
            }

            var records = Character.EmploymentHistory.ToList();
            _corporationCount = records.Count;

            // Build timeline entries with duration (newest first = left to right)
            var timeline = new List<EmploymentTimelineEntry>();
            var sorted = records.OrderByDescending(r => r.StartDate).ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var record = sorted[i];
                DateTime endDate = (i == 0) ? DateTime.UtcNow : sorted[i - 1].StartDate;
                var duration = endDate - record.StartDate;
                timeline.Add(new EmploymentTimelineEntry(record, duration, isCurrent: i == 0));
            }

            _timelineEntries = timeline;
        }
    }

    /// <summary>
    /// Represents a single entry in the employment timeline with duration calculation.
    /// </summary>
    public sealed class EmploymentTimelineEntry
    {
        public EmploymentRecord Record { get; }
        public TimeSpan Duration { get; }
        public bool IsCurrent { get; }
        public string CorporationName => Record.CorporationName;
        public DateTime StartDate => Record.StartDate;

        public string DurationText
        {
            get
            {
                string prefix = IsCurrent ? "Current - " : "";
                if (Duration.TotalDays >= 365)
                    return $"{prefix}{(int)(Duration.TotalDays / 365)}y {(int)(Duration.TotalDays % 365 / 30)}m";
                if (Duration.TotalDays >= 30)
                    return $"{prefix}{(int)(Duration.TotalDays / 30)}m {(int)(Duration.TotalDays % 30)}d";
                return $"{prefix}{(int)Duration.TotalDays}d";
            }
        }

        public EmploymentTimelineEntry(EmploymentRecord record, TimeSpan duration, bool isCurrent = false)
        {
            Record = record;
            Duration = duration;
            IsCurrent = isCurrent;
        }
    }
}
