// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// How much training a character's skill queue still holds.
    /// </summary>
    public enum QueueHealth
    {
        /// <summary>No skill is training.</summary>
        NotTraining = 0,

        /// <summary>A skill is training but the queue has run out.</summary>
        Empty = 1,

        /// <summary>The queue ends within the critical window.</summary>
        Critical = 2,

        /// <summary>The queue ends within the warning window.</summary>
        Warning = 3,

        /// <summary>The queue runs past the warning window.</summary>
        Healthy = 4
    }
}
