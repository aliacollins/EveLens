// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Enumerations;

namespace EVEMon.Common.Interfaces
{
    /// <summary>
    /// Adds the internal methods for a query monitor.
    /// </summary>
    internal interface IQueryMonitorEx : IQueryMonitor
    {
        /// <summary>
        /// Resets the monitor with the given last update time.
        /// </summary>
        /// <param name="lastUpdate">The UTC time of the last update.</param>
        void Reset(DateTime lastUpdate);

        /// <summary>
        /// Forces an update.
        /// </summary>
        /// <param name="retryOnError">When true, the update will be reattempted until succesful.</param>
        void ForceUpdate(bool retryOnError = false);

        /// <summary>
        /// Drives the monitor's update check externally (called by parent querying class).
        /// Use this instead of self-ticking when the monitor is managed by a parent.
        /// </summary>
        void UpdateTick();

        /// <summary>
        /// Unsubscribes this monitor from FiveSecondTick so it can be driven externally.
        /// Call this after construction when the monitor's parent will drive updates.
        /// </summary>
        void SuppressSelfTicking();

        /// <summary>
        /// Updates monitor status externally when the fetch is driven by EsiScheduler
        /// rather than the monitor's own HTTP path. Used for UI status display (throbber).
        /// </summary>
        /// <param name="isUpdating">Whether a fetch is currently in progress.</param>
        /// <param name="lastUpdate">The UTC time of the last successful update (null to leave unchanged).</param>
        void SetExternalStatus(bool isUpdating, DateTime? lastUpdate = null);

        /// <summary>
        /// Sets the CachedUntil time for the NextUpdate timer display.
        /// Called by EsiScheduler closures after a fetch completes with cache metadata.
        /// </summary>
        void SetCachedUntilOverride(DateTime cachedUntil);
    }
}