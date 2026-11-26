// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Dates derived from a plan's training time. All dates are UTC.
    /// </summary>
    public static class PlanSchedule
    {
        /// <summary>
        /// The UTC time a plan finishes when its training starts at <paramref name="utcNow"/>.
        /// </summary>
        /// <param name="utcNow">The UTC time training starts.</param>
        /// <param name="trainingTime">The training time left in the plan.</param>
        public static DateTime CompletionDate(DateTime utcNow, TimeSpan trainingTime)
            => DateTime.SpecifyKind(utcNow, DateTimeKind.Utc) + trainingTime;
    }
}
