using System.Collections.Generic;

namespace EVEMon.Avalonia.ViewModels
{
    /// <summary>
    /// Avalonia display wrapper for a group of mail display entries.
    /// Contains zero business logic per Law 16.
    /// </summary>
    public sealed class MailGroupEntry
    {
        public string Name { get; }
        public string CountText { get; }
        public IReadOnlyList<MailDisplayEntry> Items { get; }
        public bool IsExpanded { get; set; } = true;

        public MailGroupEntry(string name, IReadOnlyList<MailDisplayEntry> items)
        {
            Name = name;
            Items = items;
            CountText = $"{items.Count} message{(items.Count == 1 ? "" : "s")}";
        }
    }
}
