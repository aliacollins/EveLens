// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Enumerations
{
    /// <summary>
    /// Classifies why an SSO/OAuth token request failed, so consumers can give the user an
    /// accurate, actionable message instead of a single generic "Error logging in to EVE SSO"
    /// for every failure mode (Issue #94).
    /// </summary>
    /// <remarks>
    /// The reason originates in CCP's token endpoint response body (e.g.
    /// <c>{"error":"invalid_grant"}</c>), which the HTTP layer captures on
    /// <c>HttpWebClientServiceException.ResponseBody</c>. The previous code discarded it and
    /// every failure looked identical to the user.
    /// </remarks>
    public enum SsoTokenError
    {
        /// <summary>No error — the token request succeeded.</summary>
        None = 0,

        /// <summary>
        /// The refresh token was rejected (rotated, expired, or revoked). Retrying can never
        /// succeed; the character must be re-authenticated.
        /// </summary>
        InvalidGrant,

        /// <summary>
        /// The application credentials (client ID / secret) were rejected, or the request was
        /// malformed. Affects the app/settings, not a single token.
        /// </summary>
        InvalidClient,

        /// <summary>
        /// A network, timeout, or server-side (5xx) failure. Not the token's fault — safe to
        /// retry silently without alarming the user.
        /// </summary>
        Transient,

        /// <summary>
        /// The request failed for an unrecognized reason (e.g. a 200 with an unparseable body).
        /// Surfaced with the generic SSO message.
        /// </summary>
        Unknown
    }
}
