using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Bridges EsiScheduler state persistence to settings storage.
    /// On shutdown: collects FetchJob state and writes to SerializableAPIUpdate lists.
    /// On startup: reads SerializableAPIUpdate lists and returns CachedEndpointState.
    /// </summary>
    internal static class SessionCache
    {
        /// <summary>
        /// Converts persisted API update records into CachedEndpointState for warm restart.
        /// </summary>
        public static List<CachedEndpointState> LoadForCharacter(
            IEnumerable<Serialization.Settings.SerializableAPIUpdate>? updates)
        {
            if (updates == null)
                return new List<CachedEndpointState>();

            return updates
                .Where(u => u.Method != null)
                .Select(u => new CachedEndpointState
                {
                    Method = ParseMethod(u.Method!),
                    LastUpdate = u.Time,
                    ETag = u.ETag,
                    CachedUntil = u.CachedUntil == default ? null : u.CachedUntil,
                })
                .Where(s => s.Method >= 0)
                .ToList();
        }

        /// <summary>
        /// Converts the method name string back to an int enum value.
        /// Returns -1 if parsing fails.
        /// </summary>
        private static long ParseMethod(string methodName)
        {
            if (Enum.TryParse<Enumerations.CCPAPI.ESIAPICharacterMethods>(methodName, out var method))
                return (long)method;
            return -1;
        }
    }
}
