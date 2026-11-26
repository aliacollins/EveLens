// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Models
{
    /// <summary>
    /// The neural remaps a character can spend: bonus remaps, usable at any time, and the
    /// timed remap, usable once its cooldown has passed.
    /// </summary>
    /// <param name="BonusRemaps">The number of bonus remaps.</param>
    /// <param name="NextTimedRemap">The UTC time the timed remap becomes usable;
    /// null when no cooldown is running.</param>
    public sealed record RemapAvailability(int BonusRemaps, DateTime? NextTimedRemap)
    {
        /// <summary>A character with no bonus remaps and no cooldown running.</summary>
        public static RemapAvailability TimedOnly { get; } = new(0, null);

        /// <summary>True when the timed remap is usable at the given UTC time.</summary>
        public bool IsTimedAvailableAt(DateTime utc) => NextTimedRemap == null || utc >= NextTimedRemap.Value;

        /// <summary>The number of remaps usable at the given UTC time.</summary>
        public int AvailableAt(DateTime utc) => Math.Max(0, BonusRemaps) + (IsTimedAvailableAt(utc) ? 1 : 0);

        /// <summary>True when at least one remap is usable at the given UTC time.</summary>
        public bool CanRemapAt(DateTime utc) => AvailableAt(utc) > 0;

        /// <summary>
        /// The number of remaps a schedule may place at the given UTC time. Never below one,
        /// so a schedule can be worked out for a character with nothing to spend.
        /// </summary>
        public int PlanningBudgetAt(DateTime utc) => Math.Max(1, AvailableAt(utc));

        /// <summary>
        /// Whole days until the timed remap becomes usable; zero when it already is.
        /// </summary>
        public int DaysUntilTimedRemap(DateTime utc)
            => IsTimedAvailableAt(utc) ? 0 : (int)(NextTimedRemap!.Value - utc).TotalDays;
    }
}
