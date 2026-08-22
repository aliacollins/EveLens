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
    ///   (entry app:/resfileindex.txt)                       → resource index (art, models)
    ///   (entry app:/resfileindex_Windows.txt)               → compiled-shader index
    ///   resources.eveonline.com/&lt;hash-path&gt;                 → the actual file
    ///
    /// There are two indexes, and needing both is not obvious. The base index carries every
    /// model, texture and SOF definition — 122,344 entries — and none of the compiled shaders.
    /// Those live in a second, much smaller index of 3,222 entries, every one of them under
    /// <c>res:/graphics/effect.dx11/</c> or <c>effect.dx12/</c>.
    ///
    /// Fetching only the base index costs a whole day to diagnose, because nothing fails. The
    /// engine resolves the hull, loads its geometry, builds the design, reports fourteen mesh
    /// effects with their shader filenames and twenty-eight rebound samplers — and then renders
    /// a completely black frame, because not one of those effects has a shader behind it. There
    /// is no error to find: an effect with no compiled shader simply draws nothing.
    /// </summary>
    public static class EveResourceService
    {
        private const string BinariesRoot = "https://binaries.eveonline.com";
        private const string ResourcesRoot = "https://resources.eveonline.com";

        /// <summary>The client-manifest entry naming the art/model/definition index.</summary>
        private const string BaseIndexEntry = "app:/resfileindex.txt";

        /// <summary>
        /// The client-manifest entry naming the compiled-shader index. Without it the renderer
        /// produces black frames with no diagnostic — see the chain description above.
        /// </summary>
        /// <remarks>
        /// The <c>_Windows</c> in the name is about the graphics backend, not the user's
        /// operating system: what it indexes is Direct3D 11 and 12 shader bytecode, which is
        /// what Trinity's renderer consumes wherever it runs. So this is the correct entry to
        /// ask for regardless of host platform, and there is no macOS or Linux sibling to pick
        /// between — the manifest publishes exactly three indexes, and the third is a prefetch
        /// subset of the first.
        /// </remarks>
        private const string ShaderIndexEntry = "app:/resfileindex_Windows.txt";

        private static readonly HttpClient s_http = new()
        {
            Timeout = TimeSpan.FromMinutes(3)
        };

        private static readonly SemaphoreSlim s_indexLock = new(1, 1);
        private static Dictionary<string, ResFileEntry> s_index;
        private static string s_indexBuild;

        /// <summary>
        /// Every index file on disk for the loaded build, in the order the sidecar should add
        /// them. Plural because CCP splits the index in two; see <see cref="ShaderIndexEntry"/>.
        /// </summary>
        private static List<string> s_indexFiles = new();

        /// <summary>
        /// EveLens's cache root, the parent of every directory this service owns.
        /// </summary>
        /// <remarks>
        /// Taken from <see cref="AppServices.ApplicationPaths"/> rather than
        /// <c>EveLensClient.EveLensCacheDir</c> (Law 14). Both resolve to the same folder in
        /// production — <c>ApplicationPathsAdapter</c> snapshots it from EveLensClient at
        /// startup — but going through the interface means a test can point this service at a
        /// temporary directory, and a service that cannot be redirected cannot be integration
        /// tested at all without writing into the user's real cache.
        /// </remarks>
        private static string CacheRoot =>
            Path.Combine(AppServices.ApplicationPaths.DataDirectory, "cache");

        /// <summary>Root of the local resource cache (lazily created).</summary>
        public static string CacheDirectory => Path.Combine(CacheRoot, "resources");

        /// <summary>
        /// Where verbatim copies of CCP's <c>resfileindex</c> live, one file per client build.
        /// </summary>
        /// <remarks>
        /// Deliberately NOT inside <see cref="CacheDirectory"/>. That directory is
        /// content-addressed and LRU-pruned, and the index is neither — pruning it would
        /// silently cost the renderer its ability to resolve a single <c>res:/</c> path while
        /// looking like ordinary cache hygiene.
        ///
        /// The index is kept as a file rather than only in memory because the render sidecar
        /// hands it to Blue's <c>remoteFileCache.AddFileIndex</c>, which reads text — an
        /// in-process dictionary is not something another process can be given. Persisting it
        /// also means a restart does not re-download ~10 MB, and an offline start still
        /// resolves resources that are already cached.
        /// </remarks>
        public static string IndexDirectory => Path.Combine(CacheRoot, "resources-index");

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

        // Single-flight: concurrent requests for the same resource share one download
        private static readonly Dictionary<string, Task<string>> s_inFlight = new();
        private static readonly object s_inFlightLock = new();

        /// <summary>Soft cap for the local cache; oldest files pruned past this.</summary>
        public const long CacheSoftLimitBytes = 2L * 1024 * 1024 * 1024; // 2 GB

        /// <summary>
        /// Downloads (or serves from cache) the resource behind a <c>res:/</c> path.
        /// Returns the local file path, or null when the path isn't in the index or
        /// the download failed after retries.
        ///
        /// Reliability contract:
        ///  - cache is content-addressed by CCP's own md5 — an upstream change is a
        ///    different filename, never an overwrite of good data
        ///  - downloads land in a .part temp file, are MD5-VERIFIED against the index,
        ///    then atomically moved — a torn download can never enter the cache
        ///  - transient failures retry twice with backoff
        ///  - concurrent callers for the same resource share a single download
        /// </summary>
        /// <param name="resPath">The game resource path (res:/…).</param>
        /// <param name="progress">
        /// Optional download progress, 0.0–1.0 — drives the SKINR viewer's
        /// "Downloading hull… 42%" readout. Reports 1.0 on cache hits immediately.
        /// </param>
        /// <param name="ct">Cancellation.</param>
        public static Task<string> GetResourceAsync(
            string resPath, IProgress<double> progress = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(resPath))
                return Task.FromResult<string>(null);

            string key = resPath.Trim().ToLowerInvariant();
            lock (s_inFlightLock)
            {
                if (s_inFlight.TryGetValue(key, out var running))
                    return running;

                var task = FetchCoreAsync(key, progress, ct);
                s_inFlight[key] = task;
                _ = task.ContinueWith(_ =>
                {
                    lock (s_inFlightLock) s_inFlight.Remove(key);
                }, TaskScheduler.Default);
                return task;
            }
        }

        private static async Task<string> FetchCoreAsync(
            string resPath, IProgress<double> progress, CancellationToken ct)
        {
            try
            {
                var index = await GetIndexAsync(ct).ConfigureAwait(false);
                if (index == null || !index.TryGetValue(resPath, out var entry))
                    return null;

                string local = Path.Combine(CacheDirectory, entry.Md5 + Path.GetExtension(entry.ResPath));
                if (File.Exists(local))
                {
                    progress?.Report(1.0);
                    return local;
                }

                Directory.CreateDirectory(CacheDirectory);
                CleanupPartFiles();

                for (int attempt = 1; attempt <= 3; attempt++)
                {
                    try
                    {
                        await DownloadVerifiedAsync(entry, local, progress, ct).ConfigureAwait(false);
                        PruneCacheIfNeeded();
                        return local;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch (Exception ex) when (attempt < 3)
                    {
                        AppServices.TraceService?.Trace(
                            $"EveResource: attempt {attempt} failed for {entry.ResPath}: {ex.Message} — retrying");
                        await Task.Delay(TimeSpan.FromSeconds(attempt * 2), ct).ConfigureAwait(false);
                    }
                }
                return null; // all retries threw; the final throw was swallowed by the filter above
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"EveResource: fetch failed for {resPath}: {ex.Message}");
                return null;
            }
        }

        private static async Task DownloadVerifiedAsync(
            ResFileEntry entry, string local, IProgress<double> progress, CancellationToken ct)
        {
            using var response = await s_http.GetAsync(
                $"{ResourcesRoot}/{entry.CdnPath}",
                HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            long? total = response.Content.Headers.ContentLength;
            string tempFile = local + ".part";
            long written = 0;

            using var md5 = System.Security.Cryptography.MD5.Create();
            await using (var source = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false))
            await using (var target = File.Create(tempFile))
            {
                var buffer = new byte[81920];
                int read;
                while ((read = await source.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    md5.TransformBlock(buffer, 0, read, null, 0);
                    await target.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    written += read;
                    if (total > 0)
                        progress?.Report(Math.Min(1.0, (double)written / total.Value));
                }
                md5.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            }

            string actualMd5 = Convert.ToHexString(md5.Hash!).ToLowerInvariant();
            if (!string.Equals(actualMd5, entry.Md5, StringComparison.OrdinalIgnoreCase))
            {
                File.Delete(tempFile);
                throw new InvalidDataException(
                    $"MD5 mismatch for {entry.ResPath}: expected {entry.Md5}, got {actualMd5}");
            }

            File.Move(tempFile, local, overwrite: true);
            progress?.Report(1.0);
            AppServices.TraceService?.Trace(
                $"EveResource: fetched + verified {entry.ResPath} ({written:N0} bytes)");
        }

        /// <summary>Removes torn .part files from crashed/cancelled downloads.</summary>
        private static void CleanupPartFiles()
        {
            try
            {
                foreach (var part in Directory.EnumerateFiles(CacheDirectory, "*.part"))
                {
                    // Only reap parts old enough that no live download can own them
                    if (DateTime.UtcNow - File.GetLastWriteTimeUtc(part) > TimeSpan.FromHours(1))
                        File.Delete(part);
                }
            }
            catch { /* cache hygiene must never break a fetch */ }
        }

        /// <summary>Oldest-first prune when the cache exceeds the soft limit.</summary>
        private static void PruneCacheIfNeeded()
        {
            try
            {
                var files = new DirectoryInfo(CacheDirectory).GetFiles();
                long totalSize = files.Sum(f => f.Length);
                if (totalSize <= CacheSoftLimitBytes)
                    return;

                foreach (var file in files.OrderBy(f => f.LastAccessTimeUtc))
                {
                    try { totalSize -= file.Length; file.Delete(); }
                    catch { /* file in use — skip */ }
                    if (totalSize <= CacheSoftLimitBytes * 9 / 10)
                        break;
                }
                AppServices.TraceService?.Trace("EveResource: cache pruned to soft limit");
            }
            catch { /* cache hygiene must never break a fetch */ }
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

        /// <summary>
        /// An on-disk copy of CCP's resource index, downloading it if needed. Returns null
        /// only when there is no index at all — neither cached nor reachable.
        /// </summary>
        /// <remarks>
        /// This is what the render sidecar is launched with. It is a separate entry point from
        /// <see cref="GetIndexAsync"/> because the two consumers need different things from the
        /// same download: this one needs the bytes on disk for another process, the other needs
        /// a lookup table in this one.
        /// </remarks>
        public static async Task<string> GetIndexFileAsync(CancellationToken ct = default)
        {
            var files = await GetIndexFilesAsync(ct).ConfigureAwait(false);
            return files.Count > 0 ? files[0] : null;
        }

        /// <summary>
        /// Every index file the renderer needs, in the order it should add them. Empty only when
        /// there is no index at all — neither cached nor reachable.
        /// </summary>
        /// <remarks>
        /// The base index comes first and the shader index second, which is the order CCP's own
        /// manifest lists them in. It does not actually matter — the two name disjoint sets of
        /// paths, so neither can shadow the other — but a caller reading the list has a better
        /// chance of understanding it if the big one is where they expect.
        /// </remarks>
        public static async Task<IReadOnlyList<string>> GetIndexFilesAsync(
            CancellationToken ct = default)
        {
            await GetIndexAsync(ct).ConfigureAwait(false);

            var live = s_indexFiles.Where(File.Exists).ToList();
            return live.Count > 0 ? live : LocalIndexFiles();
        }

        /// <summary>Newest persisted index file, or null when none has ever been written.</summary>
        private static string NewestLocalIndexFile() => LocalIndexFiles().FirstOrDefault();

        /// <summary>
        /// Every persisted index file for the newest build on disk. The offline path.
        /// </summary>
        /// <remarks>
        /// Grouped by build rather than simply "all of them", because two builds' indexes in one
        /// directory would hand the renderer a mix of current and rotated hash paths, and a
        /// rotated hash path is a 404 that looks like a missing resource. Pruning normally
        /// prevents that; this is the belt to its braces, since pruning is allowed to fail when a
        /// file is locked.
        /// </remarks>
        private static List<string> LocalIndexFiles()
        {
            try
            {
                if (!Directory.Exists(IndexDirectory))
                    return new List<string>();

                var files = new DirectoryInfo(IndexDirectory)
                    .GetFiles("resfileindex-*.txt")
                    .OrderByDescending(f => f.LastWriteTimeUtc)
                    .ToList();
                if (files.Count == 0)
                    return new List<string>();

                string build = BuildOf(files[0].Name);

                // Longest name last: "resfileindex-<build>.txt" sorts before
                // "resfileindex-<build>-shaders.txt", which is the manifest's own order.
                return files
                    .Where(f => BuildOf(f.Name) == build)
                    .OrderBy(f => f.Name.Length)
                    .Select(f => f.FullName)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        /// <summary>The build token out of <c>resfileindex-&lt;build&gt;[-suffix].txt</c>.</summary>
        private static string BuildOf(string fileName)
        {
            string stem = Path.GetFileNameWithoutExtension(fileName);
            var parts = stem.Split('-');
            return parts.Length >= 2 ? parts[1] : stem;
        }

        /// <summary>
        /// Writes one index file for a build, atomically, and returns the path. Never throws:
        /// a failure here costs the sidecar its index file but must not break in-process
        /// resource fetching, which already has the parsed table.
        /// </summary>
        /// <param name="suffix">
        /// Null for the base index, otherwise a discriminator so a build's several indexes can
        /// coexist. This used to be absent, and its absence was load-bearing in the wrong
        /// direction: the pruning below deletes anything that is not the file just written, so a
        /// second index written under the same name pattern deleted the first.
        /// </param>
        private static string PersistIndex(string build, string suffix, string text)
        {
            try
            {
                Directory.CreateDirectory(IndexDirectory);
                string name = suffix == null
                    ? $"resfileindex-{build}.txt"
                    : $"resfileindex-{build}-{suffix}.txt";
                string target = Path.Combine(IndexDirectory, name);
                if (File.Exists(target) && new FileInfo(target).Length > 0)
                    return target;

                string temp = target + ".part";
                File.WriteAllText(temp, text);
                File.Move(temp, target, overwrite: true);

                // One build's worth is all anyone needs; older builds are dead weight and a
                // stale one winning the offline fallback would be a confusing bug. Scoped to
                // *other builds* — every index for this build has to survive.
                foreach (var old in new DirectoryInfo(IndexDirectory).GetFiles("resfileindex-*.txt"))
                {
                    if (BuildOf(old.Name) != build)
                    {
                        try { old.Delete(); } catch { /* in use — leave it */ }
                    }
                }
                return target;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"EveResource: could not persist index for build {build}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Loads the newest persisted index into memory. The offline path: without it, a start
        /// with no network leaves every already-cached resource unreachable, which reads to the
        /// user as a corrupt cache rather than as "you are offline".
        /// </summary>
        private static Dictionary<string, ResFileEntry> LoadIndexFromDisk()
        {
            var files = LocalIndexFiles();
            if (files.Count == 0)
                return null;
            try
            {
                var map = new Dictionary<string, ResFileEntry>(StringComparer.Ordinal);
                foreach (string file in files)
                {
                    foreach (var line in File.ReadLines(file))
                    {
                        var entry = ParseIndexLine(line);
                        if (entry != null)
                            map[entry.ResPath] = entry;
                    }
                }
                if (map.Count == 0)
                    return null;
                s_indexFiles = files;
                AppServices.TraceService?.Trace(
                    $"EveResource: index loaded from disk ({map.Count:N0} entries across " +
                    $"{files.Count} file{(files.Count == 1 ? "" : "s")})");
                return map;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"EveResource: local index unreadable: {ex.Message}");
                return null;
            }
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
                    return s_index ??= LoadIndexFromDisk();

                if (s_index != null && s_indexBuild == build)
                    return s_index;

                // Client file index → the index entries within it
                string clientIndex = await s_http.GetStringAsync(
                    $"{BinariesRoot}/eveonline_{build}.txt", ct).ConfigureAwait(false);
                string[] manifest = clientIndex.Split('\n');

                string basePath = FindManifestEntry(manifest, BaseIndexEntry);
                if (basePath == null)
                    return s_index ??= LoadIndexFromDisk();

                var map = new Dictionary<string, ResFileEntry>(StringComparer.Ordinal);
                var files = new List<string>();

                string baseText = await s_http.GetStringAsync(
                    $"{BinariesRoot}/{basePath}", ct).ConfigureAwait(false);
                int baseCount = Merge(map, baseText);
                string baseFile = PersistIndex(build, null, baseText);
                if (baseFile != null)
                    files.Add(baseFile);

                // The shader index. A failure here is not fatal to resource fetching — models and
                // textures resolve perfectly well without it — so it must not take down the base
                // index that already loaded. It IS fatal to rendering, hence the loud warning:
                // the alternative is a black frame with nothing anywhere to explain it.
                int shaderCount = 0;
                string shaderPath = FindManifestEntry(manifest, ShaderIndexEntry);
                if (shaderPath == null)
                {
                    AppServices.TraceService?.Trace(
                        $"EveResource: build {build} publishes no {ShaderIndexEntry} — 3D renders " +
                        "will be black because no compiled shader can be resolved");
                }
                else
                {
                    try
                    {
                        string shaderText = await s_http.GetStringAsync(
                            $"{BinariesRoot}/{shaderPath}", ct).ConfigureAwait(false);
                        shaderCount = Merge(map, shaderText);
                        string shaderFile = PersistIndex(build, "shaders", shaderText);
                        if (shaderFile != null)
                            files.Add(shaderFile);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        AppServices.TraceService?.Trace(
                            "EveResource: shader index unavailable, 3D renders will be black: " +
                            ex.Message);
                    }
                }

                s_index = map;
                s_indexBuild = build;
                s_indexFiles = files;
                AppServices.TraceService?.Trace(
                    $"EveResource: index loaded for build {build} ({baseCount:N0} resources, " +
                    $"{shaderCount:N0} shaders)");
                return s_index;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace($"EveResource: index load failed: {ex.Message}");
                return s_index ??= LoadIndexFromDisk();
            }
            finally
            {
                s_indexLock.Release();
            }
        }

        /// <summary>
        /// The CDN hash path for a named entry in the client manifest, or null if absent.
        /// </summary>
        /// <remarks>
        /// Matched on <c>name + ","</c> rather than the bare name, because
        /// <c>app:/resfileindex.txt</c> is a prefix of <c>app:/resfileindex_Windows.txt</c> — a
        /// plain <c>StartsWith</c> on the name alone would happily return the shader index when
        /// asked for the base one, depending only on manifest line order.
        /// </remarks>
        private static string FindManifestEntry(string[] manifest, string entry)
        {
            string prefix = entry + ",";
            string line = manifest.FirstOrDefault(
                l => l.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (line == null)
                return null;
            var parts = line.Split(',');
            return parts.Length >= 2 ? parts[1].Trim() : null;
        }

        /// <summary>Folds an index file's text into the lookup table; returns lines accepted.</summary>
        private static int Merge(Dictionary<string, ResFileEntry> map, string text)
        {
            int added = 0;
            foreach (var line in text.Split('\n'))
            {
                var entry = ParseIndexLine(line);
                if (entry == null)
                    continue;
                map[entry.ResPath] = entry;
                added++;
            }
            return added;
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
