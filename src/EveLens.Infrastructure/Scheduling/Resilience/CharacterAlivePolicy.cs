// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Scheduling.Resilience
{
    /// <summary>
    /// Short-circuits fetch attempts for characters that have been unregistered.
    /// Prevents stale callbacks from accessing deleted character objects.
    /// First policy in the chain — cheapest check, biggest short-circuit.
    /// </summary>
    internal sealed class CharacterAlivePolicy : IFetchPolicy
    {
        private readonly ConcurrentDictionary<long, bool> _alive = new();

        public Task<FetchOutcome> ExecuteAsync(long characterId, Func<Task<FetchOutcome>> next)
        {
            if (!_alive.TryGetValue(characterId, out bool alive) || !alive)
                return Task.FromResult(new FetchOutcome { StatusCode = 0 });

            return next();
        }

        public void Register(long characterId) => _alive[characterId] = true;
        public void Unregister(long characterId) => _alive[characterId] = false;
        public void Remove(long characterId) => _alive.TryRemove(characterId, out _);
    }
}
