using System;

namespace EVEMon.Common.Serialization.Esi
{
    /// <summary>
    /// Avoid saturating ESI with failed requests by refusing new requests if the error count
    /// approaches the limit. Implements ESI best practices for error rate limiting.
    /// See: https://developers.eveonline.com/docs/best-practices/
    /// </summary>
    public static class EsiErrors
    {
        // How many errors remaining to allow as a "buffer" before refusing requests
        private const int ERROR_THRESHOLD = 10;
        // Default error count when not in throttling mode (ESI allows 100 errors per minute)
        private const int DEFAULT_ERROR_COUNT = 100;

        // The number of errors remaining until throttling kicks in
        private static int s_errorCount = DEFAULT_ERROR_COUNT;
        // Locks synchronous access to the error count / time
        private static readonly object s_errorLock = new object();
        // When the error count resets
        private static DateTime s_errorReset = DateTime.MinValue;

        /// <summary>
        /// Returns the time when the error count will reset; delayed requests can be
        /// rescheduled on or after this time.
        /// </summary>
        public static DateTime ErrorCountResetTime
        {
            get
            {
                // No need to lock as it reads once and does not write
                DateTime when = DateTime.UtcNow, reset = s_errorReset;
                if (reset > when)
                    when = reset;
                return when;
            }
        }

        /// <summary>
        /// Returns true if no ESI requests should be issued due to error count problems.
        /// </summary>
        public static bool IsErrorCountExceeded
        {
            get
            {
                bool error;
                lock (s_errorLock)
                {
                    // Check if we've passed the reset time - if so, reset our tracking
                    if (DateTime.UtcNow >= s_errorReset)
                    {
                        s_errorCount = DEFAULT_ERROR_COUNT;
                        s_errorReset = DateTime.MinValue;
                    }
                    // Block requests if error count is at or below threshold
                    error = s_errorCount <= ERROR_THRESHOLD && DateTime.UtcNow < s_errorReset;
                }
                return error;
            }
        }

        /// <summary>
        /// Gets the current number of errors remaining. Returns null if not currently tracking.
        /// </summary>
        public static int? CurrentErrorCount
        {
            get
            {
                lock (s_errorLock)
                {
                    if (DateTime.UtcNow >= s_errorReset)
                        return null;
                    return s_errorCount;
                }
            }
        }

        /// <summary>
        /// Updates the error count when it is reported by ESI.
        /// Per ESI best practices, we track X-Esi-Error-Limit-Remain and X-Esi-Error-Limit-Reset.
        /// </summary>
        /// <param name="errorCount">The number of errors remaining until throttling (from X-Esi-Error-Limit-Remain).</param>
        /// <param name="errorReset">The time when the error count resets (calculated from X-Esi-Error-Limit-Reset).</param>
        /// <returns>true if throttling is NOT in effect (safe to continue), or false if we should stop making requests</returns>
        public static bool UpdateErrors(int errorCount, DateTime errorReset)
        {
            lock (s_errorLock)
            {
                // If we've passed the previous reset time, start fresh
                if (DateTime.UtcNow >= s_errorReset)
                {
                    s_errorCount = DEFAULT_ERROR_COUNT;
                }

                // Always update with the latest error count from ESI
                s_errorCount = errorCount;

                // Sanity check: error reset should not be too far in the future
                var maxErrorReset = DateTime.UtcNow.AddMinutes(2.0);
                if (errorReset > maxErrorReset)
                    errorReset = maxErrorReset;

                // Update reset time
                s_errorReset = errorReset;
            }

            return errorCount > ERROR_THRESHOLD;
        }
    }
}
