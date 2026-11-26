// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Enumerations;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Grades a skill queue by the training time it still holds.
    /// </summary>
    public static class SkillQueueHealth
    {
        /// <summary>A queue with less than this left is critical.</summary>
        public static readonly TimeSpan CriticalBelow = TimeSpan.FromHours(24);

        /// <summary>A queue with less than this left needs attention.</summary>
        public static readonly TimeSpan WarningBelow = TimeSpan.FromDays(5);

        /// <summary>
        /// Grades a queue.
        /// </summary>
        /// <param name="isTraining">Whether a skill is training.</param>
        /// <param name="remaining">The time until the queue ends; zero or negative when it has ended.</param>
        public static QueueHealth Evaluate(bool isTraining, TimeSpan remaining)
        {
            if (!isTraining)
                return QueueHealth.NotTraining;
            if (remaining <= TimeSpan.Zero)
                return QueueHealth.Empty;
            if (remaining < CriticalBelow)
                return QueueHealth.Critical;
            if (remaining < WarningBelow)
                return QueueHealth.Warning;
            return QueueHealth.Healthy;
        }
    }
}
