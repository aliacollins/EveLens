// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Scheduling
{
    /// <summary>
    /// Tracks authentication health for a character's ESI endpoints.
    /// </summary>
    internal enum AuthStatus
    {
        /// <summary>Character has valid OAuth tokens.</summary>
        Healthy,
        /// <summary>Character received 401/403 — all jobs suspended until re-auth.</summary>
        AuthFailed
    }

    /// <summary>
    /// Per-character authentication state tracking.
    /// </summary>
    internal sealed class CharacterAuthState
    {
        public AuthStatus Status { get; set; } = AuthStatus.Healthy;
        public int ConsecutiveFailures { get; set; }

        public void MarkFailed()
        {
            Status = AuthStatus.AuthFailed;
            ConsecutiveFailures++;
        }

        public void MarkHealthy()
        {
            Status = AuthStatus.Healthy;
            ConsecutiveFailures = 0;
        }
    }
}
