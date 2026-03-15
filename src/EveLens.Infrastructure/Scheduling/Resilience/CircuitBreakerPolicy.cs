// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Scheduling.Resilience
{
    /// <summary>
    /// Per-character circuit breaker. Isolates failure state so one character's
    /// ESI problems don't affect other characters.
    ///
    /// States:
    ///   Closed  — requests flow normally, failures counted.
    ///   Open    — requests fail fast (StatusCode=0), no HTTP call.
    ///   HalfOpen — one probe request allowed to test recovery.
    ///
    /// Replaces the global <c>EsiErrors.IsErrorCountExceeded</c> gate.
    /// </summary>
    internal sealed class CircuitBreakerPolicy : IFetchPolicy
    {
        private readonly ConcurrentDictionary<long, CircuitState> _circuits = new();
        private readonly int _failureThreshold;
        private readonly TimeSpan _openDuration;

        public CircuitBreakerPolicy(int failureThreshold = 5, TimeSpan? openDuration = null)
        {
            _failureThreshold = failureThreshold;
            _openDuration = openDuration ?? TimeSpan.FromMinutes(2);
        }

        public async Task<FetchOutcome> ExecuteAsync(long characterId, Func<Task<FetchOutcome>> next)
        {
            var circuit = _circuits.GetOrAdd(characterId, _ => new CircuitState());

            // Open circuit — fail fast unless ready for half-open probe
            if (circuit.ConsecutiveFailures >= _failureThreshold)
            {
                if (DateTime.UtcNow - circuit.OpenedAt < _openDuration)
                {
                    // Still open — allow one probe request via Interlocked
                    if (Interlocked.CompareExchange(ref circuit.Probing, 1, 0) != 0)
                        return new FetchOutcome { StatusCode = 0 }; // fail fast
                }
                // else: open duration elapsed → half-open, let request through
            }

            var outcome = await next().ConfigureAwait(false);
            var errorClass = ErrorClassifier.Classify(outcome.StatusCode);

            if (errorClass == ErrorClassifier.ErrorClass.Success)
            {
                circuit.RecordSuccess();
                Interlocked.Exchange(ref circuit.Probing, 0); // Reset — circuit closes
            }
            else if (errorClass is ErrorClassifier.ErrorClass.Transient or ErrorClassifier.ErrorClass.TokenRefresh)
            {
                circuit.RecordFailure(_failureThreshold);
                // Don't reset Probing — failed probe keeps circuit blocking further requests
            }
            else
            {
                // Auth/RateLimit/Permanent don't affect circuit state
                Interlocked.Exchange(ref circuit.Probing, 0);
            }

            return outcome;
        }

        /// <summary>
        /// Removes circuit state for a character (called on unregister).
        /// </summary>
        public void RemoveCharacter(long characterId) =>
            _circuits.TryRemove(characterId, out _);

        /// <summary>
        /// Resets circuit state for a character (called on re-auth).
        /// </summary>
        public void ResetCharacter(long characterId)
        {
            if (_circuits.TryGetValue(characterId, out var circuit))
                circuit.RecordSuccess();
        }

        private sealed class CircuitState
        {
            public int ConsecutiveFailures;
            public DateTime OpenedAt;
            public int Probing; // 0 = not probing, 1 = probe in flight

            public void RecordSuccess()
            {
                Interlocked.Exchange(ref ConsecutiveFailures, 0);
                OpenedAt = DateTime.MinValue;
            }

            public void RecordFailure(int threshold)
            {
                int failures = Interlocked.Increment(ref ConsecutiveFailures);
                if (failures == threshold)
                    OpenedAt = DateTime.UtcNow;
            }
        }
    }
}
