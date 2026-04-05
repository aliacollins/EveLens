// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.ComponentModel;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for a group of mail display entries.
    /// Contains zero business logic per Law 16.
    /// </summary>
    public sealed class MailGroupEntry : INotifyPropertyChanged, ICollapsibleGroup
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public IReadOnlyList<MailDisplayEntry> Items { get; }
        public bool ShowHeader { get; set; } = true;

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
            }
        }

        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";

        public event PropertyChangedEventHandler? PropertyChanged;

        public MailGroupEntry(string name, IReadOnlyList<MailDisplayEntry> items)
        {
            Name = name;
            Items = items;
            CountText = $"{items.Count} message{(items.Count == 1 ? "" : "s")}";
        }
    }
}
