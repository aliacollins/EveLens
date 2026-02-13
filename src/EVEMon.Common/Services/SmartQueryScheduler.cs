using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Adaptive query scheduler that replaces <see cref="QueryMonitor.CentralQueryScheduler"/>
    /// with testable, DI-based scheduling. Features:
    /// <list type="bullet">
    ///   <item>Priority scheduling: visible character processes every tick</item>
    ///   <item>Background round-robin: one background character per tick cycle</item>
    ///   <item>Adaptive polling: backs off for queryables returning Not Modified</item>
    ///   <item>Rate limit awareness: pauses background work when ESI is under pressure</item>
    ///   <item>Staggered startup: prevents thundering herd on registration</item>
    /// </list>
    /// </summary>
    internal sealed class SmartQueryScheduler : IQueryScheduler
    {
        private const int BasePollingIntervalMs = 5000;
        private const int AdaptiveThreshold = 3;
        private const int MaxAdaptiveMultiplier = 4;
        private const int BaseStartupDelayMs = 75;
        private const int MaxRandomStartupDelayMs = 250;
        private const double RateLimitThreshold = 0.8;

        private readonly IDispatcher _dispatcher;
        private readonly IEsiClient _esiClient;
        private readonly object _lock = new object();

        private readonly List<QueryableEntry> _entries = new List<QueryableEntry>();
        private long _visibleCharacterId;
        private int _backgroundRoundRobinIndex;
        private long _apiCallsInWindow;
        private long _totalTickCount;
        private bool _disposed;
        private readonly Random _random;

        /// <summary>
        /// Creates a new SmartQueryScheduler.
        /// </summary>
        /// <param name="dispatcher">Dispatcher for scheduling recurring ticks.</param>
        /// <param name="esiClient">ESI client for rate limit information.</param>
        public SmartQueryScheduler(IDispatcher dispatcher, IEsiClient esiClient)
            : this(dispatcher, esiClient, new Random())
        {
        }

        /// <summary>
        /// Creates a new SmartQueryScheduler with an explicit random source (for testing).
        /// </summary>
        internal SmartQueryScheduler(IDispatcher dispatcher, IEsiClient esiClient, Random random)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
            _random = random ?? throw new ArgumentNullException(nameof(random));

            // Schedule the first tick; each tick re-schedules the next
            ScheduleNextTick();
        }

        /// <inheritdoc />
        public int RegisteredCount
        {
            get { lock (_lock) { return _entries.Count; } }
        }

        /// <inheritdoc />
        public int CurrentPollingIntervalMs => BasePollingIntervalMs;

        /// <inheritdoc />
        public long ApiCallsInWindow => _apiCallsInWindow;

        /// <inheritdoc />
        public bool IsRateLimitPaused
        {
            get
            {
                int max = _esiClient.MaxConcurrentRequests;
                if (max <= 0)
                    return false;

                return (double)_esiClient.ActiveRequests / max > RateLimitThreshold;
            }
        }

        /// <inheritdoc />
        public void Register(IScheduledQueryable queryable)
        {
            if (queryable == null || _disposed)
                return;

            lock (_lock)
            {
                if (_entries.Any(e => e.Queryable == queryable))
                    return;

                int index = _entries.Count;
                long startupDelayMs = (long)index * BaseStartupDelayMs + _random.Next(MaxRandomStartupDelayMs + 1);
                long elapsedMsAtRegistration = _totalTickCount * BasePollingIntervalMs;
                var entry = new QueryableEntry(queryable, startupDelayMs, elapsedMsAtRegistration);
                _entries.Add(entry);
            }
        }

        /// <inheritdoc />
        public void Unregister(IScheduledQueryable queryable)
        {
            if (queryable == null)
                return;

            lock (_lock)
            {
                int idx = _entries.FindIndex(e => e.Queryable == queryable);
                if (idx >= 0)
                {
                    _entries.RemoveAt(idx);

                    // Adjust round-robin index if needed
                    if (_backgroundRoundRobinIndex >= _entries.Count && _entries.Count > 0)
                        _backgroundRoundRobinIndex = 0;
                }
            }
        }

        /// <inheritdoc />
        public void SetVisibleCharacter(long characterId)
        {
            _visibleCharacterId = characterId;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            lock (_lock)
            {
                _entries.Clear();
            }
        }

        /// <summary>
        /// Schedules the next tick via the dispatcher.
        /// </summary>
        private void ScheduleNextTick()
        {
            _dispatcher.Schedule(TimeSpan.FromMilliseconds(BasePollingIntervalMs), OnTick);
        }

        /// <summary>
        /// Main tick handler. Processes the visible character and one background character per tick.
        /// </summary>
        private void OnTick()
        {
            if (_disposed)
                return;

            _totalTickCount++;

            try
            {
                long currentElapsedMs = _totalTickCount * BasePollingIntervalMs;
                ProcessVisibleCharacter(currentElapsedMs);
                ProcessOneBackgroundCharacter(currentElapsedMs);
            }
            finally
            {
                // Always schedule the next tick unless disposed
                if (!_disposed)
                    ScheduleNextTick();
            }
        }

        /// <summary>
        /// Processes the visible character's queryables every tick (priority scheduling).
        /// </summary>
        private void ProcessVisibleCharacter(long currentElapsedMs)
        {
            long visibleId = _visibleCharacterId;
            if (visibleId == 0)
                return;

            QueryableEntry[] snapshot;
            lock (_lock)
            {
                snapshot = _entries.Where(e => e.Queryable.CharacterID == visibleId).ToArray();
            }

            foreach (var entry in snapshot)
            {
                if (!entry.IsStartupDelayElapsed(currentElapsedMs))
                    continue;

                if (!entry.DecrementAndCheckReady())
                    continue;

                entry.Queryable.ProcessTick();
                _apiCallsInWindow++;
                UpdateAdaptiveState(entry);
            }
        }

        /// <summary>
        /// Processes one background character per tick in round-robin fashion.
        /// Skipped when rate limit pause is active.
        /// </summary>
        private void ProcessOneBackgroundCharacter(long currentElapsedMs)
        {
            if (IsRateLimitPaused)
                return;

            long visibleId = _visibleCharacterId;

            QueryableEntry entryToProcess = null;
            lock (_lock)
            {
                int count = _entries.Count;
                if (count == 0)
                    return;

                // Try up to count times to find a processable background entry
                for (int attempt = 0; attempt < count; attempt++)
                {
                    if (_backgroundRoundRobinIndex >= count)
                        _backgroundRoundRobinIndex = 0;

                    var candidate = _entries[_backgroundRoundRobinIndex];
                    _backgroundRoundRobinIndex++;

                    if (candidate.Queryable.CharacterID == visibleId)
                        continue;

                    if (!candidate.IsStartupDelayElapsed(currentElapsedMs))
                        continue;

                    if (!candidate.DecrementAndCheckReady())
                        continue;

                    entryToProcess = candidate;
                    break;
                }
            }

            if (entryToProcess != null)
            {
                entryToProcess.Queryable.ProcessTick();
                _apiCallsInWindow++;
                UpdateAdaptiveState(entryToProcess);
            }
        }

        /// <summary>
        /// Updates the adaptive multiplier based on the queryable's Not Modified count.
        /// Also resets the countdown timer when the multiplier changes.
        /// </summary>
        private static void UpdateAdaptiveState(QueryableEntry entry)
        {
            int notModified = entry.Queryable.ConsecutiveNotModifiedCount;

            if (notModified == 0)
            {
                entry.AdaptiveMultiplier = 1;
                entry.TicksUntilNextProcess = 0;
                return;
            }

            if (notModified >= AdaptiveThreshold)
            {
                int newMultiplier = entry.AdaptiveMultiplier * 2;
                if (newMultiplier > MaxAdaptiveMultiplier)
                    newMultiplier = MaxAdaptiveMultiplier;

                if (newMultiplier != entry.AdaptiveMultiplier)
                {
                    entry.AdaptiveMultiplier = newMultiplier;
                    // Set countdown so the new interval takes effect immediately
                    entry.TicksUntilNextProcess = newMultiplier - 1;
                }
            }
        }

        /// <summary>
        /// Tracks per-queryable state for adaptive polling and staggered startup.
        /// Uses a countdown pattern for adaptive back-off: when the multiplier is N,
        /// the queryable is processed once every N ticks.
        /// </summary>
        private sealed class QueryableEntry
        {
            private readonly long _startupDelayMs;
            private readonly long _registeredAtElapsedMs;

            public QueryableEntry(IScheduledQueryable queryable, long startupDelayMs, long registeredAtElapsedMs)
            {
                Queryable = queryable;
                _startupDelayMs = startupDelayMs;
                _registeredAtElapsedMs = registeredAtElapsedMs;
                AdaptiveMultiplier = 1;
            }

            public IScheduledQueryable Queryable { get; }
            public int AdaptiveMultiplier { get; set; }
            public int TicksUntilNextProcess { get; set; }

            /// <summary>
            /// Decrements the countdown and returns true if this entry should be processed now.
            /// When the countdown reaches zero, it resets to the current adaptive multiplier.
            /// </summary>
            public bool DecrementAndCheckReady()
            {
                if (TicksUntilNextProcess <= 0)
                {
                    // Ready to process; reset countdown for next cycle
                    TicksUntilNextProcess = AdaptiveMultiplier - 1;
                    return true;
                }

                TicksUntilNextProcess--;
                return false;
            }

            /// <summary>
            /// Whether the staggered startup delay has elapsed based on scheduler time.
            /// </summary>
            public bool IsStartupDelayElapsed(long currentElapsedMs)
            {
                return (currentElapsedMs - _registeredAtElapsedMs) >= _startupDelayMs;
            }
        }
    }
}
