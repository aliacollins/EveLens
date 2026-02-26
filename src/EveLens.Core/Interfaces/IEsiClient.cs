// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Abstracts ESI (EVE Swagger Interface) API access with rate limiting.
    /// Provides a single change point for CCP API modifications and a throttle gate
    /// to prevent exceeding ESI's per-second request limits.
    /// </summary>
    /// <remarks>
    /// Internally backed by a <c>SemaphoreSlim</c>-based request queue that limits
    /// the number of concurrent in-flight HTTP requests. When the queue is not yet
    /// initialized (early startup), <see cref="EnqueueAsync{T}"/> falls back to
    /// direct execution of the operation.
    ///
    /// The <see cref="ActiveRequests"/> and <see cref="MaxConcurrentRequests"/> properties
    /// are used by <c>SmartQueryScheduler</c> to detect rate-limit pressure and pause
    /// background polling when active requests exceed 80% of the maximum.
    ///
    /// Production: <c>EsiClientService</c> in <c>EveLens.Common/Services/EsiClientService.cs</c>
    /// (delegates to <c>EveLensClient.ApiRequestQueue</c>).
    /// Testing: Provide a stub that executes operations immediately or returns canned results.
    /// </remarks>
    public interface IEsiClient
    {
        /// <summary>
        /// Gets the maximum number of concurrent API requests allowed.
        /// Defaults to 20 if the request queue is not yet initialized.
        /// </summary>
        int MaxConcurrentRequests { get; }

        /// <summary>
        /// Gets the number of API requests currently in flight (executing).
        /// Used by the query scheduler to calculate rate-limit pressure.
        /// </summary>
        long ActiveRequests { get; }

        /// <summary>
        /// Gets the number of API requests waiting in the queue for a concurrency slot.
        /// </summary>
        long QueuedRequests { get; }

        /// <summary>
        /// Enqueues an async API operation for rate-limited execution.
        /// The operation will be started when a concurrency slot becomes available.
        /// If the request queue is not initialized, falls back to direct execution.
        /// </summary>
        /// <typeparam name="T">The result type of the API operation.</typeparam>
        /// <param name="operation">The async operation to execute (typically an ESI HTTP call).</param>
        /// <returns>The result of the operation once it completes.</returns>
        Task<T> EnqueueAsync<T>(Func<Task<T>> operation);
    }
}
