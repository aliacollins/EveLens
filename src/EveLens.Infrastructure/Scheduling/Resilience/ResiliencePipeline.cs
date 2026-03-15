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
    /// Composes <see cref="IFetchPolicy"/> instances into a middleware chain.
    /// Policies execute in order: first added = outermost wrapper.
    /// </summary>
    internal sealed class ResiliencePipeline
    {
        private readonly IFetchPolicy[] _policies;

        public ResiliencePipeline(params IFetchPolicy[] policies)
        {
            _policies = policies ?? Array.Empty<IFetchPolicy>();
        }

        public Task<FetchOutcome> ExecuteAsync(long characterId, Func<Task<FetchOutcome>> fetch)
        {
            // Build chain from inside out: last policy wraps fetch, first policy wraps everything
            Func<Task<FetchOutcome>> chain = fetch;
            for (int i = _policies.Length - 1; i >= 0; i--)
            {
                var policy = _policies[i];
                var next = chain;
                chain = () => policy.ExecuteAsync(characterId, next);
            }
            return chain();
        }
    }
}
