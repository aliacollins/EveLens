// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Scheduling.Resilience
{
    /// <summary>
    /// Classifies ESI HTTP status codes into actionable error categories.
    /// Pure classification — takes no action.
    /// </summary>
    internal static class ErrorClassifier
    {
        public enum ErrorClass
        {
            Success,
            Transient,
            /// <summary>Token expired (401) — should auto-refresh, not permanent.</summary>
            TokenExpired,
            /// <summary>Forbidden (403) — wrong scopes or character transfer, user action needed.</summary>
            AuthPermanent,
            RateLimit,
            Permanent,
            TokenRefresh,
            Skipped
        }

        public static ErrorClass Classify(int statusCode) => statusCode switch
        {
            200 or 304 => ErrorClass.Success,
            401 => ErrorClass.TokenExpired,     // Transient — token refresh will fix it
            403 => ErrorClass.AuthPermanent,     // Permanent — scopes revoked or character transferred
            429 => ErrorClass.RateLimit,
            >= 500 => ErrorClass.Transient,
            -1 => ErrorClass.TokenRefresh,
            0 => ErrorClass.Skipped,
            _ when statusCode < 0 => ErrorClass.Transient,
            _ => ErrorClass.Permanent
        };

        public static bool IsTransient(int statusCode) =>
            Classify(statusCode) is ErrorClass.Transient or ErrorClass.TokenRefresh;
    }
}
