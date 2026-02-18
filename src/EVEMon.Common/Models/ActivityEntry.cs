using System;

namespace EVEMon.Common.Models
{
    /// <summary>
    /// Lightweight POCO for a single activity log entry.
    /// </summary>
    public sealed class ActivityEntry
    {
        public DateTime Timestamp { get; set; }
        public string CharacterName { get; set; } = "";
        public string Category { get; set; } = "";
        public string Description { get; set; } = "";
        public bool IsRead { get; set; }
    }
}
