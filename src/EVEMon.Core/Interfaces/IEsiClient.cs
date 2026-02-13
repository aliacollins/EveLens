using System;
using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts ESI (EVE Swagger Interface) API access.
    /// Provides a single change point for CCP API modifications.
    /// </summary>
    public interface IEsiClient
    {
        /// <summary>
        /// Gets the maximum number of concurrent API requests.
        /// </summary>
        int MaxConcurrentRequests { get; }

        /// <summary>
        /// Gets the number of currently active requests.
        /// </summary>
        long ActiveRequests { get; }

        /// <summary>
        /// Gets the number of requests waiting in the queue.
        /// </summary>
        long QueuedRequests { get; }

        /// <summary>
        /// Enqueues an API operation with rate limiting.
        /// </summary>
        /// <typeparam name="T">The result type.</typeparam>
        /// <param name="operation">The async operation to execute.</param>
        /// <returns>The operation result.</returns>
        Task<T> EnqueueAsync<T>(Func<Task<T>> operation);
    }
}
