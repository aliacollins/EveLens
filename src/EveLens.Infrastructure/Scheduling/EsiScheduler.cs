// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Common.Scheduling.Resilience;
using EveLens.Core.Enumerations;
using EveLens.Infrastructure.Scheduling.Health;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace EveLens.Common.Scheduling
{
    /// <summary>
    /// Ground-up ESI fetch scheduler. Drives all character data fetching via a priority queue
    /// on a background Task, with commands from the UI thread via ConcurrentQueue.
    /// </summary>
    internal sealed class EsiScheduler : IEsiScheduler
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEventAggregator _eventAggregator;
        private readonly IEsiClient _esiClient;
        private readonly ILogger? _logger;

        private readonly PriorityQueue<(FetchJob Job, long Version), DateTime> _queue = new();
        private readonly ConcurrentQueue<SchedulerCommand> _commands = new();
        private readonly Dictionary<long, long> _generations = new();
        private readonly Dictionary<long, CharacterAuthState> _authStates = new();
        private readonly Dictionary<long, List<FetchJob>> _jobsByCharacter = new();
        private readonly Dictionary<(long, long), FetchJob> _jobLookup = new();
        private readonly TokenTracker _tokenTracker = new();
        private readonly SemaphoreSlim _concurrencyGate = new(20, 20);
        private readonly CancellationTokenSource _cts = new();
        private readonly SemaphoreSlim _wakeSignal = new(0, 1);

        // Resilience pipeline — middleware chain applied to every fetch
        private readonly CharacterAlivePolicy _alivePolicy = new();
        private readonly CircuitBreakerPolicy _circuitBreaker = new();
        private readonly ResiliencePipeline _pipeline;

        // Health tracking — state machine per (character, endpoint)
        private EndpointHealthTracker? _healthTracker;

        private long _visibleCharacterId;
        private int _activeFetches;
        private int _characterIndex;
        private bool _disposed;
        private Task? _dispatchLoop;

        // Persisted state callback — set by SessionCache
        internal Action<IReadOnlyDictionary<(long, long), FetchJob>>? OnPersistState { get; set; }

        /// <summary>Injects the health tracker after construction (avoids circular dependency with AppServices).</summary>
        internal void SetHealthTracker(EndpointHealthTracker tracker) => _healthTracker = tracker;

        public EsiScheduler(
            IDispatcher dispatcher,
            IEventAggregator eventAggregator,
            IEsiClient esiClient,
            ILogger<EsiScheduler>? logger = null)
        {
            _dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
            _logger = logger;

            // Compose resilience pipeline: alive check → circuit breaker → fetch
            // No retry — with 60+ characters, retry amplifies ESI error budget burn.
            // The scheduler's 2-minute re-enqueue handles recovery naturally.
            _pipeline = new ResiliencePipeline(
                _alivePolicy,
                _circuitBreaker);

            _dispatchLoop = Task.Run(() => RunAsync(_cts.Token));
        }

        public int QueueDepth
        {
            get { lock (_queue) return _queue.Count; }
        }

        public int ActiveFetches => _activeFetches;

        public DateTime? GetNextFetchTime()
        {
            lock (_queue)
            {
                return _queue.TryPeek(out _, out var dueTime) ? dueTime : null;
            }
        }

        public DateTime? GetNextFetchTime(long characterId)
        {
            lock (_queue)
            {
                if (!_jobsByCharacter.TryGetValue(characterId, out var jobs) || jobs.Count == 0)
                    return null;
                var min = DateTime.MaxValue;
                foreach (var job in jobs)
                    if (job.CachedUntil < min) min = job.CachedUntil;
                return min == DateTime.MaxValue ? null : min;
            }
        }

        #region Public API (UI thread — pushes commands)

        public void RegisterCharacter(long characterId, IReadOnlyList<EndpointRegistration> endpoints)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.Register, CharacterId = characterId, Endpoints = endpoints });
        }

        public void UnregisterCharacter(long characterId)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.Unregister, CharacterId = characterId });
        }

        public void SetVisibleCharacter(long characterId)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.SetVisible, CharacterId = characterId });
        }

        public void ForceRefresh(long characterId, long endpointMethod = -1)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.ForceRefresh, CharacterId = characterId, EndpointMethod = endpointMethod });
        }

        public void OnCharacterReAuthenticated(long characterId)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.ReAuth, CharacterId = characterId });
        }

        public void RestoreState(long characterId, IReadOnlyList<CachedEndpointState> states)
        {
            EnqueueCommand(new SchedulerCommand { Type = CommandType.RestoreState, CharacterId = characterId, PersistedStates = states });
        }

        public void PersistState()
        {
            // Snapshot the dictionary — safe after Dispose() has stopped the dispatch loop
            var snapshot = new Dictionary<(long, long), FetchJob>(_jobLookup);
            OnPersistState?.Invoke(snapshot);
        }

        private void EnqueueCommand(SchedulerCommand cmd) { _commands.Enqueue(cmd); Wake(); }

        #endregion

        #region Dispatch Loop (background thread)

        private async Task RunAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    // 1. Drain command queue
                    ProcessCommands();

                    // 2. Check global error budget
                    // (EsiErrors is static — checked directly in fetch)

                    // 3. Drain all overdue jobs
                    int dispatched = 0;
                    while (dispatched < 20)
                    {
                        FetchJob? job;
                        long version;
                        DateTime dueTime;
                        lock (_queue)
                        {
                            if (!_queue.TryPeek(out var entry, out dueTime))
                                break;
                            if (dueTime > DateTime.UtcNow)
                                break;
                            _queue.Dequeue();
                            job = entry.Job;
                            version = entry.Version;
                        }

                        // Skip stale queue entries (job was re-enqueued with newer version)
                        if (version != job.ScheduleVersion)
                            continue;

                        // Skip stale jobs (character generation changed)
                        if (_generations.TryGetValue(job.CharacterId, out long gen) && job.Generation != gen)
                            continue;
                        if (job.IsRemoved)
                            continue;

                        // Skip auth-failed characters
                        if (_authStates.TryGetValue(job.CharacterId, out var authState) &&
                            authState.Status == AuthStatus.AuthFailed)
                            continue;

                        // Skip if already in-flight (prevents duplicate concurrent fetches)
                        if (job.IsInFlight)
                        {
                            // Re-enqueue without version bump so it retries after current fetch completes
                            lock (_queue) { _queue.Enqueue((job, job.ScheduleVersion), DateTime.UtcNow.AddSeconds(5)); }
                            continue;
                        }

                        // Check rate limit
                        if (!_tokenTracker.CanFetch(job.CharacterId, job.RateGroup))
                        {
                            var refillTime = _tokenTracker.NextRefillTime(job.CharacterId, job.RateGroup);
                            Enqueue(job, refillTime);
                            continue;
                        }

                        // Fire-and-forget the fetch
                        _ = ExecuteFetchAsync(job, ct);
                        dispatched++;
                    }

                    // 4. Sleep until next due item or wake signal
                    TimeSpan sleepDuration;
                    lock (_queue)
                    {
                        if (_queue.TryPeek(out var _, out var nextDue))
                        {
                            var delay = nextDue - DateTime.UtcNow;
                            sleepDuration = delay > TimeSpan.Zero ? delay : TimeSpan.Zero;
                            if (sleepDuration > TimeSpan.FromSeconds(5))
                                sleepDuration = TimeSpan.FromSeconds(5);
                        }
                        else
                        {
                            sleepDuration = TimeSpan.FromSeconds(1);
                        }
                    }

                    if (sleepDuration > TimeSpan.Zero)
                    {
                        try
                        {
                            await _wakeSignal.WaitAsync(sleepDuration, ct).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) when (ct.IsCancellationRequested)
                        {
                            break;
                        }
                    }
                }
                catch (OperationCanceledException) when (ct.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "EsiScheduler dispatch loop error");
                    await Task.Delay(1000, ct).ConfigureAwait(false);
                }
            }
        }

        private async Task ExecuteFetchAsync(FetchJob job, CancellationToken ct)
        {
            job.IsInFlight = true;
            await _concurrencyGate.WaitAsync(ct).ConfigureAwait(false);
            Interlocked.Increment(ref _activeFetches);
            try
            {
                // Execute through resilience pipeline:
                // CharacterAlivePolicy → CircuitBreakerPolicy → RetryPolicy → raw fetch
                // Transient errors (500+, timeouts) are retried silently.
                // The scheduler only sees final outcomes after retry exhaustion.
                var outcome = await _pipeline.ExecuteAsync(
                    job.CharacterId, () => job.ExecuteAsync(job.ETag)).ConfigureAwait(false);

                // Update job state from the response
                if (outcome.ETag != null)
                    job.ETag = outcome.ETag;

                // Update rate limit tracking
                if (outcome.RateLimitRemaining.HasValue)
                    _tokenTracker.Update(job.CharacterId, job.RateGroup,
                        outcome.RateLimitRemaining, null);

                // Record outcome for health state tracking
                _healthTracker?.Record(job.CharacterId, job.EndpointMethod,
                    new FetchRecord
                    {
                        Timestamp = DateTime.UtcNow,
                        StatusCode = outcome.StatusCode,
                        CacheDuration = outcome.CachedUntil > DateTime.UtcNow
                            ? outcome.CachedUntil - DateTime.UtcNow
                            : TimeSpan.FromMinutes(2)
                    });

                // Handle final outcome — only reached after pipeline retries are exhausted
                switch (outcome.StatusCode)
                {
                    case 200:
                        job.ConsecutiveNotModified = 0;
                        job.CachedUntil = outcome.CachedUntil;
                        Enqueue(job, outcome.CachedUntil + FetchPolicy.GetJitter(job.Priority));
                        break;

                    case 304: // Not Modified — 1 token cost, no data change
                        job.ConsecutiveNotModified++;
                        if (job.ConsecutiveNotModified >= 5)
                        {
                            job.ETag = null;
                            job.ConsecutiveNotModified = 0;
                        }
                        var nextDue304 = outcome.CachedUntil;
                        var minWait = DateTime.UtcNow.AddSeconds(30);
                        if (nextDue304 < minWait && job.CachedUntil > DateTime.UtcNow)
                            nextDue304 = job.CachedUntil;
                        else if (nextDue304 < minWait)
                            nextDue304 = minWait;
                        job.CachedUntil = nextDue304;
                        Enqueue(job, nextDue304 + FetchPolicy.GetJitter(job.Priority));
                        break;

                    case 401:
                    case 403: // Auth failed — suspend all jobs for this character
                        if (_authStates.TryGetValue(job.CharacterId, out var authState))
                            authState.MarkFailed();
                        BumpGeneration(job.CharacterId);
                        break;

                    case 429: // Rate limited — respect Retry-After
                        var retryAfter = outcome.RetryAfterSeconds ?? 60;
                        Enqueue(job, DateTime.UtcNow.AddSeconds(retryAfter));
                        break;

                    case 0: // Circuit open or character dead — short backoff, probe again soon
                        Enqueue(job, DateTime.UtcNow.AddMinutes(2));
                        break;

                    default: // Persistent failure after all retries exhausted
                        Enqueue(job, DateTime.UtcNow.AddMinutes(5));
                        break;
                }

                // Publish fetch completed event for UI status tracking
                _dispatcher.Post(() => _eventAggregator.Publish(new MonitorFetchCompletedEvent
                {
                    CharacterId = job.CharacterId,
                    EndpointMethod = job.EndpointMethod,
                    HttpStatusCode = outcome.StatusCode,
                    CachedUntil = job.CachedUntil,
                    IsUpdating = _activeFetches > 1,
                }));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Fetch failed for char {CharId} endpoint {Method}",
                    job.CharacterId, job.EndpointMethod);

                // Re-enqueue with backoff
                Enqueue(job, DateTime.UtcNow.AddMinutes(2));
            }
            finally
            {
                job.IsInFlight = false;
                Interlocked.Decrement(ref _activeFetches);
                _concurrencyGate.Release();
            }
        }

        #endregion

        #region Command Processing (dispatch loop thread only)

        private void ProcessCommands()
        {
            while (_commands.TryDequeue(out var cmd))
            {
                switch (cmd.Type)
                {
                    case CommandType.Register: ProcessRegister(cmd); break;
                    case CommandType.Unregister: ProcessUnregister(cmd.CharacterId); break;
                    case CommandType.SetVisible: ProcessSetVisible(cmd.CharacterId); break;
                    case CommandType.ForceRefresh: ProcessForceRefresh(cmd.CharacterId, cmd.EndpointMethod); break;
                    case CommandType.ReAuth: ProcessReAuth(cmd.CharacterId); break;
                    case CommandType.RestoreState: ProcessRestoreState(cmd.CharacterId, cmd.PersistedStates); break;
                }
            }
        }

        private void ProcessRegister(SchedulerCommand cmd)
        {
            var charId = cmd.CharacterId;

            // Initialize generation and auth state
            if (!_generations.ContainsKey(charId))
                _generations[charId] = 0;
            if (!_authStates.ContainsKey(charId))
                _authStates[charId] = new CharacterAuthState();

            // Mark character alive in resilience pipeline
            _alivePolicy.Register(charId);

            bool isVisible = charId == Interlocked.Read(ref _visibleCharacterId);
            int charIndex = _characterIndex++;

            var planned = ColdStartPlanner.Plan(
                charId, charIndex, isVisible, cmd.Endpoints!, null);

            var jobs = new List<FetchJob>(planned.Count);
            foreach (var (job, dueTime) in planned)
            {
                job.Generation = _generations[charId];
                _jobLookup[job.Key] = job;
                jobs.Add(job);
                Enqueue(job, dueTime);
            }
            _jobsByCharacter[charId] = jobs;

            _logger?.LogInformation("Registered char {CharId} with {Count} endpoints",
                charId, cmd.Endpoints!.Count);
        }

        private void ProcessUnregister(long charId)
        {
            BumpGeneration(charId);
            _authStates.Remove(charId);
            _tokenTracker.RemoveCharacter(charId);
            _alivePolicy.Unregister(charId);
            _circuitBreaker.RemoveCharacter(charId);
            _healthTracker?.RemoveCharacter(charId);

            if (_jobsByCharacter.TryGetValue(charId, out var jobs))
            {
                foreach (var job in jobs)
                {
                    job.IsRemoved = true;
                    _jobLookup.Remove(job.Key);
                }
                _jobsByCharacter.Remove(charId);
            }

            _logger?.LogInformation("Unregistered char {CharId}", charId);
        }

        private void ProcessSetVisible(long charId)
        {
            var oldVisible = Interlocked.Exchange(ref _visibleCharacterId, charId);
            if (oldVisible == charId)
                return;

            // Demote old visible character's jobs
            if (_jobsByCharacter.TryGetValue(oldVisible, out var oldJobs))
            {
                foreach (var job in oldJobs)
                    job.Priority = FetchPriority.Background;
            }

            // Promote new visible character's jobs and re-enqueue overdue ones
            if (_jobsByCharacter.TryGetValue(charId, out var newJobs))
            {
                foreach (var job in newJobs)
                {
                    job.Priority = FetchPriority.Active;
                    // If cache expired, enqueue at now for immediate fetch
                    if (job.CachedUntil <= DateTime.UtcNow)
                        Enqueue(job, DateTime.UtcNow);
                }
            }

            _logger?.LogDebug("Tab switch: {OldChar} → {NewChar}", oldVisible, charId);
        }

        private void ProcessForceRefresh(long charId, long endpointMethod)
        {
            if (_jobsByCharacter.TryGetValue(charId, out var jobs))
            {
                if (endpointMethod == -1)
                {
                    // Force all endpoints — bump character generation to invalidate all queued entries
                    BumpGeneration(charId);
                    foreach (var job in jobs)
                    {
                        job.Generation = _generations[charId];
                        job.ETag = null; // Clear ETag so fetch does a full GET, not conditional
                        Enqueue(job, DateTime.UtcNow);
                    }
                }
                else
                {
                    // Force single endpoint — only re-enqueue that job (ScheduleVersion handles dedup)
                    foreach (var job in jobs)
                    {
                        if (job.EndpointMethod == endpointMethod)
                        {
                            job.ETag = null; // Clear ETag so fetch does a full GET, not conditional
                            Enqueue(job, DateTime.UtcNow);
                            break;
                        }
                    }
                }
            }
        }

        private void ProcessReAuth(long charId)
        {
            if (_authStates.TryGetValue(charId, out var auth))
                auth.MarkHealthy();
            _circuitBreaker.ResetCharacter(charId);
            _healthTracker?.OnReAuthenticated(charId);

            // Re-enqueue all jobs for this character
            if (_jobsByCharacter.TryGetValue(charId, out var jobs))
            {
                foreach (var job in jobs)
                {
                    job.IsRemoved = false;
                    Enqueue(job, DateTime.UtcNow);
                }
            }
        }

        private void ProcessRestoreState(long charId, IReadOnlyList<CachedEndpointState>? states)
        {
            if (states == null)
                return;

            foreach (var state in states)
            {
                var key = (charId, state.Method);
                if (_jobLookup.TryGetValue(key, out var job))
                {
                    // Do NOT restore ETags — the first fetch after launch should always be
                    // a clean GET. CCP's cache nodes may have rotated between sessions, and
                    // stale ETags cause perpetual 304s even when data has changed.
                    // Only restore CachedUntil to avoid re-fetching data that hasn't expired.
                    job.ETag = null;
                    job.CachedUntil = state.CachedUntil ?? default;
                }
            }
        }

        #endregion

        #region Helpers

        private void Enqueue(FetchJob job, DateTime dueTime)
        {
            job.ScheduleVersion++;
            lock (_queue)
            {
                _queue.Enqueue((job, job.ScheduleVersion), dueTime);
            }
        }

        private void BumpGeneration(long charId)
        {
            if (_generations.ContainsKey(charId))
                _generations[charId]++;
            else
                _generations[charId] = 1;
        }

        private void BumpJobGeneration(FetchJob job)
        {
            BumpGeneration(job.CharacterId);
            job.Generation = _generations[job.CharacterId];
        }

        private void Wake()
        {
            // Release the wake signal if it's not already signaled
            if (_wakeSignal.CurrentCount == 0)
            {
                try { _wakeSignal.Release(); }
                catch (SemaphoreFullException) { /* already signaled */ }
            }
        }

        #endregion

        #region Command Types

        private enum CommandType
        {
            Register,
            Unregister,
            SetVisible,
            ForceRefresh,
            ReAuth,
            RestoreState,
        }

        private sealed class SchedulerCommand
        {
            public CommandType Type { get; init; }
            public long CharacterId { get; init; }
            public long EndpointMethod { get; init; }
            public IReadOnlyList<EndpointRegistration>? Endpoints { get; init; }
            public IReadOnlyList<CachedEndpointState>? PersistedStates { get; init; }
        }

        #endregion

        #region Dispose

        public void Dispose()
        {
            if (_disposed)
                return;
            _disposed = true;

            _cts.Cancel();
            Wake(); // Unblock the sleep

            try
            {
                _dispatchLoop?.Wait(TimeSpan.FromSeconds(3));
            }
            catch (AggregateException) { }

            _cts.Dispose();
            _concurrencyGate.Dispose();
            _wakeSignal.Dispose();
        }

        #endregion
    }
}
