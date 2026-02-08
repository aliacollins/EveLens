using System;

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
    }
}