// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using EveLens.Common.Extensions;
using EveLens.Common.Net;
using EveLens.Common.Serialization;
using EveLens.Common.Serialization.Esi;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Fetches SKINR data from ESI's date-versioned routes (2026-08-18 compat date).
    /// Unlike the classic <c>/latest/</c> routes, these live at the ESI root and
    /// select their version via the <c>X-Compatibility-Date</c> header.
    ///
    /// Public (no token): design recipes and the global Paragon Hub listing feed.
    /// Scoped (<c>esi.cosmetic.char:read</c>): a character's SKINR inventory,
    /// component licenses, and their own hub listings.
    /// </summary>
    public static class EsiSkinrService
    {
        private const string EsiRoot = "https://esi.evetech.net";

        /// <summary>
        /// The compatibility date these DTOs were written against. Bump ONLY together
        /// with re-verifying every SKINR DTO against the spec for the new date.
        /// </summary>
        internal const string CompatibilityDate = "2026-08-18";

        private static RequestParams BuildParams(string authToken = null)
        {
            var rp = new RequestParams
            {
                AcceptEncoded = true,
                CustomHeaders = new Dictionary<string, string>
                {
                    ["X-Compatibility-Date"] = CompatibilityDate
                }
            };
            if (!string.IsNullOrEmpty(authToken))
                rp.Authentication = authToken;
            return rp;
        }

        /// <summary>
        /// The full public recipe of a design: hull, tier, and the complete
        /// nanocoating/pattern layout — enough to visualize it.
        /// </summary>
        public static Task<JsonResult<EsiSkinrRecipe>> GetDesignAsync(string skinrId)
        {
            skinrId.ThrowIfNull(nameof(skinrId));
            var url = new Uri($"{EsiRoot}/cosmetics/skinr/{WebUtility.UrlEncode(skinrId)}");
            return Util.DownloadJsonAsync<EsiSkinrRecipe>(url, BuildParams());
        }

        /// <summary>
        /// One page of the public Paragon Hub feed. Null starts from the newest;
        /// pass the previous page's <c>Cursor.Before</c> to walk OLDER listings —
        /// the feed is newest-first, so <c>after</c> asks for listings newer than
        /// the newest and returns nothing (measured live: the "10 listings total"
        /// bug). The default page size is 10; <paramref name="limit"/> raises it
        /// (100 verified working).
        /// </summary>
        public static Task<JsonResult<EsiSkinrListingsPage>> GetHubListingsAsync(
            string cursorBefore = null, int limit = 0)
        {
            var query = new List<string>();
            if (limit > 0)
                query.Add("limit=" + limit);
            if (!string.IsNullOrEmpty(cursorBefore))
                query.Add("before=" + WebUtility.UrlEncode(cursorBefore));
            string qs = query.Count > 0 ? "?" + string.Join("&", query) : string.Empty;
            var url = new Uri($"{EsiRoot}/paragon-hub/skinr{qs}");
            return Util.DownloadJsonAsync<EsiSkinrListingsPage>(url, BuildParams());
        }

        /// <summary>SKINR design licenses the character owns (scoped).</summary>
        public static Task<JsonResult<EsiSkinrInventory>> GetCharacterSkinrsAsync(
            long characterId, string authToken)
        {
            authToken.ThrowIfNull(nameof(authToken));
            var url = new Uri($"{EsiRoot}/characters/{characterId}/cosmetics/skinr");
            return Util.DownloadJsonAsync<EsiSkinrInventory>(url, BuildParams(authToken));
        }

        /// <summary>Component licenses (nanocoatings/patterns) the character owns (scoped).</summary>
        public static Task<JsonResult<EsiSkinrComponentInventory>> GetCharacterComponentsAsync(
            long characterId, string authToken)
        {
            authToken.ThrowIfNull(nameof(authToken));
            var url = new Uri($"{EsiRoot}/characters/{characterId}/cosmetics/skinr/components");
            return Util.DownloadJsonAsync<EsiSkinrComponentInventory>(url, BuildParams(authToken));
        }

        /// <summary>The character's own Paragon Hub listings (scoped).</summary>
        public static Task<JsonResult<EsiSkinrListingsPage>> GetOwnListingsAsync(
            long characterId, string authToken, string cursorAfter = null)
        {
            authToken.ThrowIfNull(nameof(authToken));
            string query = string.IsNullOrEmpty(cursorAfter)
                ? string.Empty
                : "?after=" + WebUtility.UrlEncode(cursorAfter);
            var url = new Uri($"{EsiRoot}/characters/{characterId}/paragon-hub/skinr{query}");
            return Util.DownloadJsonAsync<EsiSkinrListingsPage>(url, BuildParams(authToken));
        }
    }
}
