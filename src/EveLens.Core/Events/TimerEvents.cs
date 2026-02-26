// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Events
{
    /// <summary>
    /// Published every second via <see cref="EveLens.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveLensClient.SecondTick</c> static event.
    /// </summary>
    /// <remarks>
    /// Triggered by <c>EveLensClient</c>'s one-second WinForms timer on the UI thread.
    /// Used for countdown updates (skill queue, industry jobs), UI refresh, and
    /// scheduling checks.
    ///
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocating a new
    /// object on every tick. Parameterless because the only information is "a second has passed."
    ///
    /// Subscribers receive this on the UI thread (no marshaling needed for WinForms controls).
    /// </remarks>
    public sealed class SecondTickEvent
    {
        /// <summary>
        /// Shared singleton instance. Reuse this instead of allocating a new object per tick.
        /// </summary>
        public static readonly SecondTickEvent Instance = new SecondTickEvent();
    }

    /// <summary>
    /// Published every five seconds via <see cref="EveLens.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveLensClient.FiveSecondTick</c> static event.
    /// </summary>
    /// <remarks>
    /// Triggered by <c>EveLensClient</c> when the one-second tick counter reaches a multiple of 5.
    /// Used by <c>CCPCharacter</c> for periodic status checks and UI refresh of less
    /// time-sensitive data (e.g., market orders, contracts).
    ///
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// Subscribers receive this on the UI thread.
    /// </remarks>
    public sealed class FiveSecondTickEvent
    {
        /// <summary>
        /// Shared singleton instance. Reuse this instead of allocating a new object per tick.
        /// </summary>
        public static readonly FiveSecondTickEvent Instance = new FiveSecondTickEvent();
    }

    /// <summary>
    /// Published every thirty seconds via <see cref="EveLens.Core.Interfaces.IEventAggregator"/>.
    /// Replaces <c>EveLensClient.ThirtySecondTick</c> static event.
    /// </summary>
    /// <remarks>
    /// Triggered by <c>EveLensClient</c> when the one-second tick counter reaches a multiple of 30.
    /// Used for infrequent background tasks such as checking for application updates,
    /// server status polling, and garbage collection of expired cache entries.
    ///
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// Subscribers receive this on the UI thread.
    /// </remarks>
    public sealed class ThirtySecondTickEvent
    {
        /// <summary>
        /// Shared singleton instance. Reuse this instead of allocating a new object per tick.
        /// </summary>
        public static readonly ThirtySecondTickEvent Instance = new ThirtySecondTickEvent();
    }
}
