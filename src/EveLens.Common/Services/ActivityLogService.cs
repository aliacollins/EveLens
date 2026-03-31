// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Encodings.Web;
using System.Text.Json;
using EveLens.Common.Models;

namespace EveLens.Common.Services
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

                var json = JsonSerializer.Serialize(toSave, new JsonSerializerOptions
                {
                    WriteIndented = false,
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                File.WriteAllText(_filePath, json);
            }
            catch
            {
                // Non-critical — don't crash on save failure
            }
        }
    }
}
