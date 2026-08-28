// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using EveLens.Common.Net;
using EveLens.Common.Serialization.Hub;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Fetches the hub's pre-resolved market catalog (designs.json) so the Paragon
    /// Hub pane can identify every design in one request instead of walking ESI
    /// recipes one-per-150ms per client — the walk that kept the whole window busy
    /// for minutes on first open. Strictly read-only GET against evelens.dev,
    /// strictly behind the same community-previews opt-in as the thumbnail shelf
    /// (one consent covers "talk to the EveLens hub", not one per feature).
    /// </summary>
    /// <remarks>
    /// The raw response is cached on disk and re-parsed from there; within
    /// <see cref="MaxAge"/> the network is never touched. A miss, a parse failure,
    /// or an unreachable hub all mean "no catalog" — the pane falls back to the
    /// client-side ESI walk exactly as before, so this can only ever make things
    /// faster, never break them.
    /// </remarks>
    public static class SkinrHubCatalog
    {
        /// <summary>Public base — the EveLens domain end to end, same posture as
        /// <see cref="SkinrThumbnailCdn"/>.</summary>
        public const string Url = "https://hub.evelens.dev/designs.json";

        /// <summary>The live market is ~3,400 designs at ~800 KB; 8 MB is not a catalog.</summary>
        private const int MaxBytes = 8 * 1024 * 1024;

        /// <summary>How long a cached catalog answers before the hub is asked again.
        /// Identity (name/hull/creator) is immutable; only prices drift, and the ESI
        /// listings feed supplies fresh prices anyway.</summary>
        private static readonly TimeSpan MaxAge = TimeSpan.FromHours(6);

        private static string CacheFile => Path.Combine(
            AppServices.ApplicationPaths.DataDirectory, "cache", "skinr",
            "hub-catalog.json");

        /// <summary>
        /// The catalog keyed by design id, or null when neither the disk cache nor
        /// the hub can supply one. Never throws.
        /// </summary>
        public static async Task<IReadOnlyDictionary<string, HubDesignInfo>?> TryGetAsync()
        {
            string? json = ReadFreshCache();
            if (json == null)
            {
                json = await DownloadAsync().ConfigureAwait(false);
                if (json != null)
                    WriteCache(json);
                else
                    json = ReadCacheIgnoringAge();   // stale beats absent
            }
            return json == null ? null : Parse(json);
        }

        /// <summary>The parse half, separated so tests can feed it fixtures.</summary>
        internal static IReadOnlyDictionary<string, HubDesignInfo>? Parse(string json)
        {
            try
            {
                HubDesignCatalog? catalog =
                    JsonSerializer.Deserialize<HubDesignCatalog>(json);
                if (catalog?.Designs == null || catalog.Designs.Count == 0)
                    return null;
                var map = new Dictionary<string, HubDesignInfo>(
                    catalog.Designs.Count, StringComparer.OrdinalIgnoreCase);
                foreach (HubDesignInfo design in catalog.Designs)
                {
                    if (!string.IsNullOrEmpty(design?.Id))
                        map[design!.Id] = design;
                }
                return map.Count > 0 ? map : null;
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrHubCatalog: parse failed: {ex.Message}");
                return null;
            }
        }

        private static async Task<string?> DownloadAsync()
        {
            try
            {
                var result = await HttpWebClientService.DownloadStreamAsync(
                    new Uri(Url), ReadCapped, null).ConfigureAwait(false);
                if (result.Error != null || result.Result == null)
                    return null;
                return Encoding.UTF8.GetString(result.Result);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrHubCatalog: download failed: {ex.Message}");
                return null;
            }
        }

        private static byte[]? ReadCapped(Stream stream, ResponseParams response)
        {
            using var buffer = new MemoryStream();
            var chunk = new byte[64 * 1024];
            int read;
            while ((read = stream.Read(chunk, 0, chunk.Length)) > 0)
            {
                if (buffer.Length + read > MaxBytes)
                    return null;
                buffer.Write(chunk, 0, read);
            }
            return buffer.Length > 0 ? buffer.ToArray() : null;
        }

        private static string? ReadFreshCache()
        {
            try
            {
                var info = new FileInfo(CacheFile);
                if (!info.Exists || DateTime.UtcNow - info.LastWriteTimeUtc > MaxAge)
                    return null;
                return File.ReadAllText(CacheFile);
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static string? ReadCacheIgnoringAge()
        {
            try
            {
                return File.Exists(CacheFile) ? File.ReadAllText(CacheFile) : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private static void WriteCache(string json)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(CacheFile)!);
                File.WriteAllText(CacheFile, json);
            }
            catch (Exception ex)
            {
                AppServices.TraceService?.Trace(
                    $"SkinrHubCatalog: cache write failed: {ex.Message}");
            }
        }
    }
}
