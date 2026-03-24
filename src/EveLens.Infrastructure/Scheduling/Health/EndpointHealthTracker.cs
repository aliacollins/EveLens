// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Scheduling.Resilience;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;

namespace EveLens.Infrastructure.Scheduling.Health
{
    /// <summary>
    /// Tracks per-(character, endpoint) health using a state machine with a rolling time window.
    /// Replaces the event-based error notification path for scheduler-driven ESI endpoints.
    /// Publishes <see cref="HealthStateChangedEvent"/> only on state transitions — never on
    /// repeated same-state fetch results.
    /// </summary>
    /// <remarks>
    /// <b>Design principles:</b>
    /// <list type="bullet">
    ///   <item>Errors are states, not events. No dedup logic needed.</item>
    ///   <item>Dynamic window self-tunes from ESI cache headers.</item>
    ///   <item>Hysteresis (3 consecutive successes) prevents flapping.</item>
    ///   <item>Per-endpoint isolation — one endpoint's issues don't mask another.</item>
    /// </list>
    /// </remarks>
    public sealed class EndpointHealthTracker
    {
        private static readonly TimeSpan WindowFloor = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan WindowCeiling = TimeSpan.FromMinutes(30);
        private const int RingBufferCapacity = 20;
        private const int MinSamplesForTransition = 3;
        private const int ConsecutiveSuccessesForRecovery = 3;
        private const double DegradedThreshold = 0.30;
        private const double FailingThreshold = 0.70;
        private const int MinSamplesForFailing = 5;

        private readonly IEventAggregator? _eventAggregator;
        private readonly IDispatcher? _dispatcher;
        private readonly IDisposable? _tokenRefreshSub;
        private readonly ConcurrentDictionary<(long CharId, long Endpoint), EndpointState> _states = new();

        public EndpointHealthTracker(IEventAggregator? eventAggregator = null, IDispatcher? dispatcher = null)
        {
            _eventAggregator = eventAggregator;
            _dispatcher = dispatcher;

            // When a token refresh succeeds, reset Suspended state for that character
            _tokenRefreshSub = _eventAggregator?.Subscribe<ESIKeyTokenRefreshedEvent>(
                e => OnReAuthenticated(e.CharacterId));
        }

        /// <summary>
        /// Records a fetch outcome and evaluates health state transitions.
        /// Called from the scheduler's background thread after each fetch completes.
        /// </summary>
        public void Record(long characterId, long endpointMethod, FetchRecord record)
        {
            var key = (characterId, endpointMethod);
            var state = _states.GetOrAdd(key, _ => new EndpointState());

            EndpointHealth oldHealth;
            EndpointHealth newHealth;

            lock (state)
            {
                oldHealth = state.Health;
                state.Push(record);

                // Auth failures are immediate — no window evaluation needed
                var errorClass = ErrorClassifier.Classify(record.StatusCode);
                if (errorClass == ErrorClassifier.ErrorClass.Auth)
                {
                    state.Health = EndpointHealth.Suspended;
                    state.ConsecutiveSuccesses = 0;
                }
                else
                {
                    Evaluate(state, record.Timestamp);
                }

                newHealth = state.Health;
            }

            if (oldHealth != newHealth)
                PublishTransition(characterId, endpointMethod, oldHealth, newHealth);
        }

        /// <summary>Gets the current health state for a specific endpoint.</summary>
        public EndpointHealth GetHealth(long characterId, long endpointMethod)
        {
            return _states.TryGetValue((characterId, endpointMethod), out var state)
                ? state.Health
                : EndpointHealth.Healthy;
        }

        /// <summary>
        /// Aggregates all endpoint states for a character into a single health summary.
        /// </summary>
        public CharacterHealthSummary GetCharacterHealth(long characterId)
        {
            int healthy = 0, degraded = 0, failing = 0;
            DateTime? failingSince = null;
            bool anySuspended = false;

            foreach (var kvp in _states)
            {
                if (kvp.Key.CharId != characterId)
                    continue;

                switch (kvp.Value.Health)
                {
                    case EndpointHealth.Healthy:
                        healthy++;
                        break;
                    case EndpointHealth.Degraded:
                        degraded++;
                        break;
                    case EndpointHealth.Failing:
                        failing++;
                        if (kvp.Value.FailingSince.HasValue &&
                            (failingSince == null || kvp.Value.FailingSince < failingSince))
                            failingSince = kvp.Value.FailingSince;
                        break;
                    case EndpointHealth.Suspended:
                        anySuspended = true;
                        break;
                }
            }

            CharacterHealth overall;
            if (anySuspended)
                overall = CharacterHealth.Suspended;
            else if (failing > 0)
                overall = CharacterHealth.Failing;
            else if (degraded > 0)
                overall = CharacterHealth.Degraded;
            else
                overall = CharacterHealth.Healthy;

            return new CharacterHealthSummary
            {
                OverallHealth = overall,
                HealthyCount = healthy,
                DegradedCount = degraded,
                FailingCount = failing,
                FailingSince = failingSince
            };
        }

        /// <summary>
        /// Resets all endpoints for a character to Healthy after re-authentication.
        /// </summary>
        public void OnReAuthenticated(long characterId)
        {
            foreach (var kvp in _states)
            {
                if (kvp.Key.CharId != characterId)
                    continue;

                EndpointHealth oldHealth;
                lock (kvp.Value)
                {
                    oldHealth = kvp.Value.Health;
                    kvp.Value.Health = EndpointHealth.Healthy;
                    kvp.Value.ConsecutiveSuccesses = 0;
                    kvp.Value.FailingSince = null;
                }

                if (oldHealth != EndpointHealth.Healthy)
                    PublishTransition(characterId, kvp.Key.Endpoint, oldHealth, EndpointHealth.Healthy);
            }
        }

        /// <summary>Removes all state for a character (cleanup on unregister).</summary>
        public void RemoveCharacter(long characterId)
        {
            var keysToRemove = _states.Keys.Where(k => k.CharId == characterId).ToList();
            foreach (var key in keysToRemove)
                _states.TryRemove(key, out _);
        }

        #region State Machine Evaluation

        private static void Evaluate(EndpointState state, DateTime now)
        {
            var window = ComputeWindow(state.LastKnownCacheDuration);
            var cutoff = now - window;

            // Count outcomes within the rolling window
            int successes = 0, failures = 0;
            for (int i = 0; i < state.Count; i++)
            {
                var record = state.PeekAt(i);
                if (record.Timestamp < cutoff)
                    continue;

                var errorClass = ErrorClassifier.Classify(record.StatusCode);
                if (errorClass == ErrorClassifier.ErrorClass.Success)
                    successes++;
                else if (errorClass != ErrorClassifier.ErrorClass.Skipped)
                    failures++;
            }

            int total = successes + failures;

            // Track consecutive successes for hysteresis
            var latest = state.PeekAt(state.Count - 1);
            var latestClass = ErrorClassifier.Classify(latest.StatusCode);
            if (latestClass == ErrorClassifier.ErrorClass.Success)
                state.ConsecutiveSuccesses++;
            else if (latestClass != ErrorClassifier.ErrorClass.Skipped)
                state.ConsecutiveSuccesses = 0;

            // Apply state transitions
            switch (state.Health)
            {
                case EndpointHealth.Healthy:
                    if (total >= MinSamplesForTransition && failures >= MinSamplesForTransition)
                    {
                        double failRate = (double)failures / total;
                        if (failRate >= DegradedThreshold)
                        {
                            state.Health = EndpointHealth.Degraded;
                        }
                    }
                    break;

                case EndpointHealth.Degraded:
                    if (state.ConsecutiveSuccesses >= ConsecutiveSuccessesForRecovery)
                    {
                        state.Health = EndpointHealth.Healthy;
                        state.FailingSince = null;
                    }
                    else if (total >= MinSamplesForFailing)
                    {
                        double failRate = (double)failures / total;
                        if (failRate >= FailingThreshold)
                        {
                            state.Health = EndpointHealth.Failing;
                            state.FailingSince ??= now;
                        }
                    }
                    break;

                case EndpointHealth.Failing:
                    if (state.ConsecutiveSuccesses >= ConsecutiveSuccessesForRecovery)
                    {
                        state.Health = EndpointHealth.Healthy;
                        state.FailingSince = null;
                    }
                    break;

                case EndpointHealth.Suspended:
                    // Only exits via OnReAuthenticated() — no window-based recovery
                    break;
            }
        }

        private static TimeSpan ComputeWindow(TimeSpan cacheDuration)
        {
            var window = TimeSpan.FromTicks(cacheDuration.Ticks * 5);
            if (window < WindowFloor) return WindowFloor;
            if (window > WindowCeiling) return WindowCeiling;
            return window;
        }

        #endregion

        #region Event Publishing

        private void PublishTransition(long characterId, long endpointMethod,
            EndpointHealth oldHealth, EndpointHealth newHealth)
        {
            var summary = GetCharacterHealth(characterId);
            var evt = new HealthStateChangedEvent
            {
                CharacterId = characterId,
                EndpointMethod = endpointMethod,
                OldState = (int)oldHealth,
                NewState = (int)newHealth,
                HealthyCount = summary.HealthyCount,
                DegradedCount = summary.DegradedCount,
                FailingCount = summary.FailingCount,
                FailingSince = summary.FailingSince
            };

            if (_dispatcher != null)
                _dispatcher.Post(() => _eventAggregator?.Publish(evt));
            else
                _eventAggregator?.Publish(evt);
        }

        #endregion

        #region EndpointState — per-endpoint ring buffer + state

        internal sealed class EndpointState
        {
            private readonly FetchRecord[] _buffer = new FetchRecord[RingBufferCapacity];
            private int _head; // next write index
            private int _count;

            public EndpointHealth Health { get; set; } = EndpointHealth.Healthy;
            public int ConsecutiveSuccesses { get; set; }
            public TimeSpan LastKnownCacheDuration { get; set; } = TimeSpan.FromMinutes(2);
            public DateTime? FailingSince { get; set; }

            public int Count => _count;

            public void Push(FetchRecord record)
            {
                if (record.CacheDuration > TimeSpan.Zero)
                    LastKnownCacheDuration = record.CacheDuration;

                _buffer[_head] = record;
                _head = (_head + 1) % RingBufferCapacity;
                if (_count < RingBufferCapacity)
                    _count++;
            }

            /// <summary>
            /// Reads the record at logical index i (0 = oldest, Count-1 = newest).
            /// </summary>
            public FetchRecord PeekAt(int i)
            {
                if (i < 0 || i >= _count)
                    throw new ArgumentOutOfRangeException(nameof(i));

                int start = _count < RingBufferCapacity ? 0 : _head;
                int physicalIndex = (start + i) % RingBufferCapacity;
                return _buffer[physicalIndex];
            }
        }

        #endregion
    }
}
