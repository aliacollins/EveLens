// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Fetches game resources (ship models, textures, SOF materials) from CCP's own
    /// content-addressed CDN, on demand, with a local cache. EveLens never bundles or
    /// redistributes CCP art — each user's machine downloads from CCP directly, the
    /// same way the game client does.
    ///
    /// Resolution chain (walked fresh because hashes rotate with client builds):
    ///   binaries.eveonline.com/eveclient_TQ.json            → current build number
    ///   binaries.eveonline.com/eveonline_&lt;build&gt;.txt        → client file index
    ///   (entry app:/resfileindex.txt)                       → resource index
    ///   resources.eveonline.com/&lt;hash-path&gt;                 → the actual file
    /// </summary>
    public static class EveResourceService
    {
        private const string BinariesRoot = "https://binaries.eveonline.com";
        private const string ResourcesRoot = "https://resources.eveonline.com";

        private static readonly HttpClient s_http = new()
        {
            Timeout = TimeSpan.FromMinutes(3)
        };

        private static readonly SemaphoreSlim s_indexLock = new(1, 1);
        private static Dictionary<string, ResFileEntry> s_index;
        private static string s_indexBuild;

        /// <summary>Root of the local resource cache (lazily created).</summary>
        public static string CacheDirectory =>
            Path.Combine(EveLensClient.EveLensCacheDir, "resources");

        /// <summary>
        /// One line of a resfileindex: <c>res:/path,hash-path,md5,size,compressedSize</c>.
        /// Returns null for lines that don't parse (blank/trailing lines).
        /// </summary>
        public static ResFileEntry ParseIndexLine(string line)
        {
            if (string.IsNullOrWhiteSpace(line))
                return null;
            var parts = line.Split(',');
            if (parts.Length < 3 || !parts[0].StartsWith("res:/", StringComparison.OrdinalIgnoreCase))
                return null;
            return new ResFileEntry(
                parts[0].Trim().ToLowerInvariant(),
                parts[1].Trim(),
                parts[2].Trim());
        }

        /// <summary>
        /// Downloads (or serves from cache) the resource behind a <c>res:/</c> path.
        /// Returns the local file path, or null when the path isn't in the index or
        /// the download failed. Cache is content-addressed by CCP's md5, so a file
        /// that changed upstream is re-fetched automatically.
        /// </summary>
        /// <param name="resPath">The game resource path (res:/…).</param>
        /// <param name="progress">
        /// Optional download progress, 0.0–1.0 — drives the SKINR viewer's
        /// "Downloading hull… 42%" readout. Reports 1.0 on cache hits immediately.
        /// </param>
        /// <param name="ct">Cancellation.</param>
        public static async Task<string> GetResourceAsync(
            string resPath, IProgress<double> progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resPath))
                return null;

            try
            {
                var index = await GetIndexAsync(ct).ConfigureAwait(false);
                if (index == null || !index.TryGetValue(resPath.Trim().ToLowerInvariant(), out var entry))
                    return null;

                string local = Path.Combine(CacheDirectory, entry.Md5 + Path.GetExtension(entry.ResPath));
                if (File.Exists(local))
                {
                    progress?.Report(1.0);
                    return local;
                }

                Directory.CreateDirectory(CacheDirectory);

                using var response = await s_http.GetAsync(
                    $"{ResourcesRoot}/{entry.CdnPath}",
                    HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
                response.EnsureSuccessStatusCode();

                long? total = response.Content.Headers.ContentLength;
                string tempFile = local + ".part";
                long written = 0;

                await using (var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
                await using (var target = File.Create(tempFile))
                {
                    var buffer = new byte[81920];
                    int read;
                    while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                    {
                        await target.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                        written += read;
                        if (total > 0)
                            progress?.Report(Math.Min(1.0, (double)written / total.Value));
                    }
                }

                File.Move(tempFile, local, overwrite: true);
                progress?.Report(1.0);

                AppServices.TraceService?.Trace(
                    $"EveResource: fetched {entry.ResPath} ({written:N0} bytes)");
                return local;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"EveResource: fetch failed for {resPath}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// All indexed <c>res:/</c> paths matching a predicate — e.g. every file under
        /// a ship's model directory. Empty when the index is unavailable.
        /// </summary>
        public static async Task<IReadOnlyList<string>> FindResourcesAsync(
            Func<string, bool> predicate, CancellationToken ct = default)
        {
            var index = await GetIndexAsync(ct).ConfigureAwait(false);
            if (index == null)
                return Array.Empty<string>();
            return index.Keys.Where(predicate).ToList();
        }

        private static async Task<Dictionary<string, ResFileEntry>> GetIndexAsync(CancellationToken ct)
        {
            await s_indexLock.WaitAsync(ct).ConfigureAwait(false);
            try
            {
                // Which build is current? (tiny request; also our staleness check)
                string clientJson = await s_http.GetStringAsync(
                    $"{BinariesRoot}/eveclient_TQ.json", ct).ConfigureAwait(false);
                string build = ExtractBuildNumber(clientJson);
                if (build == null)
                    return s_index; // keep whatever we have

                if (s_index != null && s_indexBuild == build)
                    return s_index;

                // Client file index → resfileindex entry
                string clientIndex = await s_http.GetStringAsync(
                    $"{BinariesRoot}/eveonline_{build}.txt", ct).ConfigureAwait(false);
                string indexLine = clientIndex
                    .Split('\n')
                    .FirstOrDefault(l => l.StartsWith("app:/resfileindex.txt,", StringComparison.OrdinalIgnoreCase));
                if (indexLine == null)
                    return s_index;

                string indexCdnPath = indexLine.Split(',')[1].Trim();
                string resIndexText = await s_http.GetStringAsync(
                    $"{BinariesRoot}/{indexCdnPath}", ct).ConfigureAwait(false);

                var map = new Dictionary<string, ResFileEntry>(StringComparer.Ordinal);
                foreach (var line in resIndexText.Split('\n'))
                {
                    var entry = ParseIndexLine(line);
                    if (entry != null)
                        map[entry.ResPath] = entry;
                }

                s_index = map;
                s_indexBuild = build;
                AppServices.TraceService?.Trace(
                    $"EveResource: index loaded for build {build} ({map.Count:N0} entries)");
                return s_index;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"EveResource: index load failed: {ex.Message}");
                return s_index;
            }
            finally
            {
                s_indexLock.Release();
            }
        }

        /// <summary>Pulls the build number out of eveclient_TQ.json without a JSON DTO.</summary>
        public static string ExtractBuildNumber(string clientJson)
        {
            if (string.IsNullOrEmpty(clientJson))
                return null;
            var match = System.Text.RegularExpressions.Regex.Match(
                clientJson, "\"build\"\\s*:\\s*\"?(\\d+)\"?");
            return match.Success ? match.Groups[1].Value : null;
        }
    }

    /// <summary>One resource index entry: game path, CDN hash path, content md5.</summary>
    public sealed class ResFileEntry
    {
        public string ResPath { get; }
        public string CdnPath { get; }
        public string Md5 { get; }

        public ResFileEntry(string resPath, string cdnPath, string md5)
        {
            ResPath = resPath;
            CdnPath = cdnPath;
            Md5 = md5;
        }
    }
}
