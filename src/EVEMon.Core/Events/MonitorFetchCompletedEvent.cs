namespace EVEMon.Core.Events
{
    /// <summary>
    /// Published when the EsiScheduler completes an HTTP fetch for an endpoint.
    /// Used by QueryMonitor (UI status) and diagnostic logging.
    /// </summary>
    public sealed class MonitorFetchCompletedEvent
    {
        /// <summary>The EVE character ID this fetch was for.</summary>
        public long CharacterId { get; init; }

        /// <summary>ESIAPICharacterMethods enum value identifying the endpoint.</summary>
        public long EndpointMethod { get; init; }

        /// <summary>HTTP status code of the response (200, 304, 401, 429, etc.).</summary>
        public int HttpStatusCode { get; init; }

        /// <summary>When the cache expires and the next fetch should occur.</summary>
        public System.DateTime CachedUntil { get; init; }

        /// <summary>HTTP ETag for conditional GET on subsequent requests (null if not provided).</summary>
        public string? ETag { get; init; }

        /// <summary>Whether this character still has pending fetches in progress.</summary>
        public bool IsUpdating { get; init; }
    }
}
