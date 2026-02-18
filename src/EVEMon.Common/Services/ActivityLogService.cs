using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using EVEMon.Common.Models;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Persistence service for activity log entries.
    /// Loads/saves JSON array of ActivityEntry to activity-log.json.
    /// </summary>
    public sealed class ActivityLogService
    {
        private readonly string _filePath;
        private const int MaxEntries = 200;

        public ActivityLogService(string dataDirectory)
        {
            _filePath = Path.Combine(dataDirectory, "activity-log.json");
        }

        public List<ActivityEntry> Load()
        {
            try
            {
                if (!File.Exists(_filePath))
                    return new List<ActivityEntry>();

                var json = File.ReadAllText(_filePath);
                return JsonSerializer.Deserialize<List<ActivityEntry>>(json) ?? new List<ActivityEntry>();
            }
            catch
            {
                return new List<ActivityEntry>();
            }
        }

        public void Save(IReadOnlyList<ActivityEntry> entries)
        {
            try
            {
                var toSave = entries.Count > MaxEntries
                    ? entries.Take(MaxEntries).ToList()
                    : new List<ActivityEntry>(entries);

                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions { WriteIndented = false });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Non-critical — don't crash on save failure
            }
        }
    }
}
