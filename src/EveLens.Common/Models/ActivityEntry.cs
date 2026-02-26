// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Models
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
