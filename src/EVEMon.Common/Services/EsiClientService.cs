using System;
using System.Threading.Tasks;
using EVEMon.Common.Net;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for <see cref="EveMonClient.ApiRequestQueue"/>.
    /// Implements <see cref="IEsiClient"/> by delegating to the existing rate limiter.
    /// </summary>
    internal sealed class EsiClientService : IEsiClient
    {
        /// <inheritdoc />
        public int MaxConcurrentRequests => EveMonClient.ApiRequestQueue?.MaxConcurrent ?? 20;

        /// <inheritdoc />
        public long ActiveRequests => EveMonClient.ApiRequestQueue?.ActiveRequests ?? 0;

        /// <inheritdoc />
        public long QueuedRequests => EveMonClient.ApiRequestQueue?.QueuedRequests ?? 0;

        /// <inheritdoc />
        public Task<T> EnqueueAsync<T>(Func<Task<T>> operation)
        {
            var queue = EveMonClient.ApiRequestQueue;
            if (queue != null)
                return queue.EnqueueAsync(operation);

            // Fallback: execute directly if queue not initialized
            return operation();
        }
    }
}
