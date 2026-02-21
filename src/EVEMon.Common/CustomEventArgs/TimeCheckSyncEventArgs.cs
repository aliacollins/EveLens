// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EVEMon.Common.CustomEventArgs
{

    ///// <summary>
    ///// Helper class for the result of asyncronous time sync requests.
    ///// </summary>
    public class TimeCheckSyncEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="TimeCheckSyncEventArgs"/> class.
        /// </summary>
        /// <param name="isSynchronised">if set to <c>true</c> the user's machine is synchronised.</param>
        /// <param name="serverTimeToLocalTime">The server time to local time.</param>
        /// <param name="localTime">The local time.</param>
        internal TimeCheckSyncEventArgs(bool isSynchronised, DateTime serverTimeToLocalTime, DateTime localTime)
        {
            IsSynchronised = isSynchronised;
            ServerTimeToLocalTime = serverTimeToLocalTime;
            LocalTime = localTime;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is synchronised.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is synchronised; otherwise, <c>false</c>.
        /// </value>
        public bool IsSynchronised { get; }

        /// <summary>
        /// Gets the server time to local time.
        /// </summary>
        /// <value>
        /// The server time to local time.
        /// </value>
        public DateTime ServerTimeToLocalTime { get; }

        /// <summary>
        /// Gets the local time.
        /// </summary>
        /// <value>
        /// The local time.
        /// </value>
        public DateTime LocalTime { get; }
    }
}