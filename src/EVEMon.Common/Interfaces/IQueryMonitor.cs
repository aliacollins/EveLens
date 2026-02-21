// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Serialization.Eve;

namespace EVEMon.Common.Interfaces
{
    public interface IQueryMonitor
    {
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="IQueryMonitor"/> is enabled.
        /// </summary>
        /// <value><c>true</c> if enabled; otherwise, <c>false</c>.</value>
        bool Enabled { get; set; }

        /// <summary>
        /// Gets true whether the monitor has to do a query on application startup.
        /// </summary>
        bool QueryOnStartup { get; }

        /// <summary>
        /// Gets the status of the query.
        /// </summary>
        QueryStatus Status { get; }

        /// <summary>
        /// Gets the API method monitored by this instance.
        /// </summary>
        Enum Method { get; }

        /// <summary>
        /// Gets the last time this instance was updated (UTC).
        /// </summary>
        DateTime LastUpdate { get; }

        /// <summary>
        /// Gets true when the API provider is not CCP or the cache timer has expired.
        /// </summary>
        bool CanForceUpdate { get; }

        /// <summary>
        /// Gets the next time this instance should be updated (UTC), based on both the CCP cache time and the user preferences.
        /// </summary>
        DateTime NextUpdate { get; }

        /// <summary>
        /// Gets true whether the method is curently being requeried.
        /// </summary>
        bool IsUpdating { get; }

        /// <summary>
        /// Gets the last API result.
        /// </summary>
        IAPIResult LastResult { get; }

        /// <summary>
        /// Gets true whether this monitor has access to data.
        /// </summary>
        bool HasAccess { get; }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources
        /// </summary>
        void Dispose();
    }
}