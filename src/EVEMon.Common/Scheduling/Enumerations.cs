// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.Scheduling
{
    /// <summary>
    /// Describes the frequency at which a schedule entry occurs.
    /// </summary>
    public enum RecurringFrequency
    {
        Daily,
        Weekdays,
        Weekends,
        Weekly,
        Monthly
    }

    /// <summary>
    /// Describes the options of a schedule entry.
    /// </summary>
    [Flags]
    public enum ScheduleEntryOptions
    {
        None = 0,

        /// <summary>
        /// Blocks skills training starting
        /// </summary>
        Blocking = 1,

        /// <summary>
        /// No tooltip notifications.
        /// </summary>
        Quiet = 2,

        /// <summary>
        /// Uses EVETime
        /// </summary>
        EVETime = 4
    }

    /// <summary>
    /// Describes the behaviour when a month is overflowed.
    /// </summary>
    public enum MonthlyOverflowResolution
    {
        /// <summary>
        /// April 31 becomes April 30
        /// </summary>
        ClipBack,

        /// <summary>
        /// April 31 is ignored
        /// </summary>
        Drop,

        /// <summary>
        /// April 31 becomes May 1
        /// </summary>
        OverlapForward
    }
}