namespace EVEMon.Common.Scheduling
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
