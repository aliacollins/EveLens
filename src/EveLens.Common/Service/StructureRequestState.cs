// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Service
{
    /// <summary>
    /// Represents the state of a structure lookup request.
    /// </summary>
    internal enum StructureRequestState
    {
        /// <summary>No request has been attempted.</summary>
        Pending,

        /// <summary>Request is currently in flight.</summary>
        InProgress,

        /// <summary>Request completed successfully.</summary>
        Completed,

        /// <summary>All available characters tried, none had access.</summary>
        Inaccessible,

        /// <summary>Structure confirmed destroyed (404 from ESI).</summary>
        Destroyed
    }
}
