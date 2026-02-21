using System.Collections.Generic;
using System.ComponentModel;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for a group of mail display entries.
    /// Contains zero business logic per Law 16.
    /// </summary>
    public sealed class MailGroupEntry : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public IReadOnlyList<MailDisplayEntry> Items { get; }

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
