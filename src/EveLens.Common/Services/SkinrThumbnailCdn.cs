// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.Threading.Tasks;
using EveLens.Common.Net;

namespace EveLens.Common.Services
{
    /// <summary>
    /// The community preview shelf: ready-made design thumbnails served as static
    /// files from evelens.dev, so a design's card fills in about a second instead of
    /// after a local 10–30s render. Strictly read-only GET, strictly OPT-IN (the app
    /// never talks to non-CCP servers without the user's remembered yes), and every
    /// byte is validated as a real image before it touches the cache. The files are
    /// produced by the maintainer's own render pipeline — nothing user-uploaded is
    /// ever served, which is what keeps a central image store trustworthy.
    /// </summary>
    /// <remarks>
    /// The filename contract is <see cref="SkinrThumbnailCache.FileNameFor"/> —
    /// identical to the local cache, so the server directory IS a thumbnail cache
    /// (publish one with <c>tools/publish-hub-thumbs.ps1</c>). A miss is normal (a
    /// design the pipeline hasn't rendered yet) and falls back to local rendering.
    /// </remarks>
    public static class SkinrThumbnailCdn
    {
        /// <summary>
        /// Public base — the EveLens domain, end to end. Users auditing their network
        /// traffic see evelens.dev and nothing else; the host behind the CNAME
        /// (Railway today) can change without touching a single client.
        /// </summary>
        public const string BaseUrl = "https://hub.evelens.dev/thumbs/";

        // A 320px PNG thumbnail runs 30–120 KB; anything near this cap is not one.
        private const int MaxBytes = 512 * 1024;

        public static Uri UrlFor(string skinrId) =>
            new(BaseUrl + SkinrThumbnailCache.FileNameFor(skinrId));

        /// <summary>
        /// Fetches a design's thumbnail into the cache. Null on miss, oversize,
        /// non-image content, or any network unhappiness — all of which simply mean
        /// "render it locally instead".
        /// </summary>
        public static async Task<string?> TryFetchAsync(
            string skinrId, SkinrThumbnailCache cache)
        {
            if (string.IsNullOrEmpty(skinrId))
                return null;
            try
            {
                var result = await HttpWebClientService
                    .DownloadStreamAsync(UrlFor(skinrId), ReadCapped, null)
                    .ConfigureAwait(false);
                if (result.Error != null || result.Result == null)
                    return null;
                return cache.SaveBytes(skinrId, result.Result);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static byte[]? ReadCapped(Stream stream, ResponseParams response)
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[16 * 1024];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > MaxBytes)
                    return null;
                buffer.Write(chunk, 0, read);
            }
            return buffer.Length > 0 ? buffer.ToArray() : null;
        }
    }
}
