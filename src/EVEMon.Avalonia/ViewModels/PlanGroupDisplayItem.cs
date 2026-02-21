// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Avalonia.Media;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for a plan skill group.
    /// Contains zero business logic per Law 16.
    /// Implements INPC for IsExpanded/Chevron so lightweight collapsible sections work.
    /// </summary>
    internal sealed class PlanGroupDisplayItem : INotifyPropertyChanged
    {
        private static readonly IBrush TrainingHeaderBrush = new SolidColorBrush(Color.Parse("#FFE6A817"));
        private static readonly IBrush MissingHeaderBrush = new SolidColorBrush(Color.Parse("#FFF0F0F0"));
        private static readonly IBrush TrainedHeaderBrush = new SolidColorBrush(Color.Parse("#FF707070"));

        private bool _isExpanded;

        public string Name { get; }
        public int Count { get; }
        public string CountText { get; }
        public string SubtotalTimeText { get; }
        public List<PlanEntryDisplayItem> Items { get; }
        public PlanEntryStatus GroupStatus { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded != value)
                {
                    _isExpanded = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
                }
            }
        }

        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";

        public IBrush HeaderBrush => GroupStatus switch
        {
            PlanEntryStatus.Training => TrainingHeaderBrush,
            PlanEntryStatus.Trained => TrainedHeaderBrush,
            _ => MissingHeaderBrush
        };

        public event PropertyChangedEventHandler? PropertyChanged;

        public PlanGroupDisplayItem(string name, PlanEntryStatus status, List<PlanEntryDisplayItem> items, TimeSpan subtotalTime)
        {
            Name = name;
            GroupStatus = status;
            Items = items;
            Count = items.Count;
            CountText = $"{Count} skill{(Count != 1 ? "s" : "")}";
            SubtotalTimeText = FormatTime(subtotalTime);
            _isExpanded = status != PlanEntryStatus.Trained;
        }

        private static string FormatTime(TimeSpan time)
        {
            if (time <= TimeSpan.Zero) return "";
            if (time.TotalDays >= 1) return $"{(int)time.TotalDays}d {time.Hours}h";
            if (time.TotalHours >= 1) return $"{(int)time.TotalHours}h {time.Minutes}m";
            return $"{(int)time.TotalMinutes}m";
        }
    }
}
