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
    /// A single composable policy in the ESI fetch resilience pipeline.
    /// Each policy wraps the next — middleware pattern.
    /// </summary>
    internal interface IFetchPolicy
    {
        Task<FetchOutcome> ExecuteAsync(long characterId, Func<Task<FetchOutcome>> next);
    }
}
