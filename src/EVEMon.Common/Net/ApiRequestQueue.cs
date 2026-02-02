using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EVEMon.Common.Net
{
    /// <summary>
    /// Provides request throttling for API calls to prevent rate limiting.
    /// Limits the number of concurrent requests and provides optional request spacing.
    /// </summary>
    public sealed class ApiRequestQueue : IDisposable
    {
        #region Fields

        /// <summary>
        /// Default maximum concurrent requests.
        /// ESI recommends no more than 20 concurrent connections.
        /// </summary>
        private const int DefaultMaxConcurrent = 20;

        /// <summary>
        /// Default minimum delay between requests in milliseconds.
        /// Provides spacing to avoid burst behavior.
        /// </summary>
        private const int DefaultMinDelayMs = 50;

        private readonly SemaphoreSlim _concurrencyLimiter;
        private readonly int _minDelayMs;
        private readonly object _statsLock = new object();

        private DateTime _lastRequestTime = DateTime.MinValue;
        private long _totalRequests;
        private long _activeRequests;
        private long _queuedRequests;
        private bool _disposed;

        #endregion


        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="ApiRequestQueue"/> class.
        /// </summary>
        /// <param name="maxConcurrent">Maximum number of concurrent requests.</param>
        /// <param name="minDelayMs">Minimum delay between requests in milliseconds.</param>
        public ApiRequestQueue(int maxConcurrent = DefaultMaxConcurrent, int minDelayMs = DefaultMinDelayMs)
        {
            if (maxConcurrent < 1)
                throw new ArgumentOutOfRangeException(nameof(maxConcurrent), "Must be at least 1");
            if (minDelayMs < 0)
                throw new ArgumentOutOfRangeException(nameof(minDelayMs), "Cannot be negative");

            _concurrencyLimiter = new SemaphoreSlim(maxConcurrent, maxConcurrent);
            _minDelayMs = minDelayMs;
            MaxConcurrent = maxConcurrent;
        }

        #endregion


        #region Properties

        /// <summary>
        /// Gets the maximum number of concurrent requests allowed.
        /// </summary>
        public int MaxConcurrent { get; }

        /// <summary>
        /// Gets the number of currently active requests.
        /// </summary>
        public long ActiveRequests
        {
            get
            {
                lock (_statsLock)
                {
                    return _activeRequests;
                }
            }
        }

        /// <summary>
        /// Gets the number of requests waiting in the queue.
        /// </summary>
        public long QueuedRequests
        {
            get
            {
                lock (_statsLock)
                {
                    return _queuedRequests;
                }
            }
        }

        /// <summary>
        /// Gets the total number of requests processed.
        /// </summary>
        public long TotalRequests
        {
            get
            {
                lock (_statsLock)
                {
                    return _totalRequests;
                }
            }
        }

        /// <summary>
        /// Gets the number of available slots for concurrent requests.
        /// </summary>
        public int AvailableSlots => _concurrencyLimiter.CurrentCount;

        #endregion


        #region Public Methods

        /// <summary>
        /// Enqueues an async request and waits for a slot to become available.
        /// </summary>
        /// <typeparam name="T">The return type of the request.</typeparam>
        /// <param name="request">The async request function to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>The result of the request.</returns>
        public async Task<T> EnqueueAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ApiRequestQueue));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Track queued requests
            lock (_statsLock)
            {
                _queuedRequests++;
            }

            try
            {
                // Wait for a slot to become available
                await _concurrencyLimiter.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // Update stats
                    lock (_statsLock)
                    {
                        _queuedRequests--;
                        _activeRequests++;
                        _totalRequests++;
                    }

                    // Apply minimum delay between requests (rate smoothing)
                    await ApplyMinDelayAsync(cancellationToken).ConfigureAwait(false);

                    // Execute the request
                    return await request().ConfigureAwait(false);
                }
                finally
                {
                    // Update stats and release slot
                    lock (_statsLock)
                    {
                        _activeRequests--;
                        _lastRequestTime = DateTime.UtcNow;
                    }
                    _concurrencyLimiter.Release();
                }
            }
            catch (OperationCanceledException)
            {
                // Update queued count if we were cancelled while waiting
                lock (_statsLock)
                {
                    if (_queuedRequests > 0)
                        _queuedRequests--;
                }
                throw;
            }
        }

        /// <summary>
        /// Enqueues an async request with no return value.
        /// </summary>
        /// <param name="request">The async request function to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        public async Task EnqueueAsync(Func<Task> request, CancellationToken cancellationToken = default)
        {
            await EnqueueAsync(async () =>
            {
                await request().ConfigureAwait(false);
                return true; // Dummy return value
            }, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Tries to enqueue a request without waiting if no slot is available.
        /// </summary>
        /// <typeparam name="T">The return type of the request.</typeparam>
        /// <param name="request">The async request function to execute.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        /// <returns>A tuple indicating success and the result (default if not successful).</returns>
        public async Task<(bool Success, T Result)> TryEnqueueAsync<T>(Func<Task<T>> request, CancellationToken cancellationToken = default)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ApiRequestQueue));
            if (request == null)
                throw new ArgumentNullException(nameof(request));

            // Try to acquire a slot without waiting
            if (!_concurrencyLimiter.Wait(0, cancellationToken))
            {
                return (false, default);
            }

            try
            {
                // Update stats
                lock (_statsLock)
                {
                    _activeRequests++;
                    _totalRequests++;
                }

                // Apply minimum delay between requests
                await ApplyMinDelayAsync(cancellationToken).ConfigureAwait(false);

                // Execute the request
                var result = await request().ConfigureAwait(false);
                return (true, result);
            }
            finally
            {
                // Update stats and release slot
                lock (_statsLock)
                {
                    _activeRequests--;
                    _lastRequestTime = DateTime.UtcNow;
                }
                _concurrencyLimiter.Release();
            }
        }

        /// <summary>
        /// Resets the statistics counters.
        /// </summary>
        public void ResetStatistics()
        {
            lock (_statsLock)
            {
                _totalRequests = 0;
            }
        }

        #endregion


        #region Private Methods

        /// <summary>
        /// Applies the minimum delay between requests if needed.
        /// </summary>
        private async Task ApplyMinDelayAsync(CancellationToken cancellationToken)
        {
            if (_minDelayMs <= 0)
                return;

            DateTime lastTime;
            lock (_statsLock)
            {
                lastTime = _lastRequestTime;
            }

            var timeSinceLastRequest = DateTime.UtcNow - lastTime;
            var remainingDelay = TimeSpan.FromMilliseconds(_minDelayMs) - timeSinceLastRequest;

            if (remainingDelay > TimeSpan.Zero)
            {
                await Task.Delay(remainingDelay, cancellationToken).ConfigureAwait(false);
            }
        }

        #endregion


        #region IDisposable

        /// <summary>
        /// Disposes the request queue.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            _concurrencyLimiter.Dispose();
        }

        #endregion
    }
}
