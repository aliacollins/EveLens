// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EveLens.Common.Enumerations.CCPAPI;

namespace EveLens.Common.Constants
{
    /// <summary>
    /// The X-Compatibility-Date each compatibility-routed ESI method was written against.
    /// </summary>
    /// <remarks>
    /// CCP's newer routes (the SKINR family is the first EveLens consumes) are versioned by
    /// date header instead of a /vN path prefix. The date is a per-DTO contract: bump an
    /// entry ONLY together with re-verifying every DTO that method deserializes against the
    /// spec for the new date. Methods absent from this table are path-versioned and send no
    /// header.
    /// </remarks>
    public static class EsiCompatibilityDates
    {
        /// <summary>The date the SKINR DTO family was verified against.</summary>
        public const string Skinr = "2026-08-18";

        private static readonly Dictionary<Enum, string> s_dates = new()
        {
            { ESIAPICharacterMethods.SkinrLicenses, Skinr },
            { ESIAPICharacterMethods.SkinrComponents, Skinr },
        };

        /// <summary>
        /// The compatibility date for the given ESI method, or null when the method is
        /// path-versioned.
        /// </summary>
        public static string ForMethod(Enum method)
            => method != null && s_dates.TryGetValue(method, out string date) ? date : null;
    }
}
