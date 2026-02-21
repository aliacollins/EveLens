// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Threading.Tasks;
using EVEMon.Core.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Scheduling
{
    /// <summary>
    /// Represents a single scheduled ESI fetch operation in the priority queue.
    /// Only accessed from the dispatch loop thread (no synchronization needed).
    /// </summary>
    internal sealed class FetchJob
    {
        public long CharacterId { get; init; }
        public long EndpointMethod { get; init; }
        public long Generation { get; set; }
        public FetchPriority Priority { get; set; }
        public string? RateGroup { get; init; }
        public string? ETag { get; set; }
        public DateTime CachedUntil { get; set; }
        public int ConsecutiveNotModified { get; set; }
        public bool IsRemoved { get; set; }
        /// <summary>
        /// True while an HTTP fetch is in-flight for this job. Prevents the dispatch loop
        /// from dispatching a duplicate concurrent fetch for the same endpoint.
        /// </summary>
        public bool IsInFlight { get; set; }
        /// <summary>
        /// Monotonically increasing schedule version. Incremented each time the job is
        /// re-enqueued. Stale queue entries with old versions are skipped on dequeue.
        /// </summary>
        public long ScheduleVersion { get; set; }

        /// <summary>
        /// Async delegate that performs the full ESI fetch cycle: HTTP + callback + return metadata.
        /// Created by the orchestrator with the correct typed closure.
        /// </summary>
        public required Func<string?, Task<FetchOutcome>> ExecuteAsync { get; init; }

        /// <summary>
        /// Creates a unique key for this job (characterId, endpointMethod).
        /// </summary>
        public (long, long) Key => (CharacterId, EndpointMethod);
    }
}
