// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Scheduling.Resilience
{
    /// <summary>
    /// Retries transient ESI errors (500+, timeouts) with exponential backoff + jitter.
    /// Only transient failures are retried — auth, rate limit, and permanent errors pass through.
    /// The scheduler never sees transient errors unless all retries are exhausted.
    /// </summary>
    internal sealed class RetryPolicy : IFetchPolicy
    {
        private readonly int _maxRetries;

        public RetryPolicy(int maxRetries = 2)
        {
            _maxRetries = maxRetries;
        }

        public async Task<FetchOutcome> ExecuteAsync(long characterId, Func<Task<FetchOutcome>> next)
        {
            FetchOutcome outcome = default;
            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                outcome = await next().ConfigureAwait(false);

                if (!ErrorClassifier.IsTransient(outcome.StatusCode))
                    return outcome;

                // Don't delay after the last attempt
                if (attempt < _maxRetries)
                    await Task.Delay(GetBackoffDelay(attempt)).ConfigureAwait(false);
            }

            // All retries exhausted — return the last (failed) outcome
            return outcome;
        }

        private static TimeSpan GetBackoffDelay(int attempt)
        {
            // Exponential backoff: ~1.5s, ~3s — with jitter to prevent thundering herd
            int baseMs = (1 << attempt) * 1500;
            int jitter = Random.Shared.Next(0, 500);
            return TimeSpan.FromMilliseconds(baseMs + jitter);
        }
    }
}
