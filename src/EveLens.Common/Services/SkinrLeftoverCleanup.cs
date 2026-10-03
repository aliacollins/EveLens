// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Deletes the files the SKINR viewer stored under the data directory: its render
    /// runtime, resource and geometry caches, thumbnails, and cached SKINR ESI data.
    /// </summary>
    public sealed class SkinrLeftoverCleanup
    {
        private static readonly string[] s_directories =
        {
            "skinr-runtime",
            Path.Combine("cache", "skinr"),
            Path.Combine("cache", "resources"),
            Path.Combine("cache", "resources-index"),
        };

        private static readonly string[] s_characterCacheFiles =
        {
            "skinr_licenses.json",
            "skinr_components.json",
        };

        private readonly string _dataDirectory;
        private readonly ITraceService? _trace;

        public SkinrLeftoverCleanup(string dataDirectory, ITraceService? trace = null)
        {
            _dataDirectory = dataDirectory;
            _trace = trace;
        }

        /// <summary>
        /// Removes every leftover that exists. Never throws; items that cannot be removed
        /// are traced and skipped.
        /// </summary>
        /// <returns>The number of directories and files removed.</returns>
        public int Run()
        {
            if (string.IsNullOrWhiteSpace(_dataDirectory) || !Directory.Exists(_dataDirectory))
                return 0;

            int removed = 0;
            foreach (string relative in s_directories)
            {
                string path = Path.Combine(_dataDirectory, relative);
                if (Directory.Exists(path) && TryDelete(path, () => Directory.Delete(path, recursive: true)))
                    removed++;
            }

            string characters = Path.Combine(_dataDirectory, "cache", "characters");
            if (Directory.Exists(characters))
            {
                foreach (string characterDirectory in SafeEnumerateDirectories(characters))
                {
                    foreach (string name in s_characterCacheFiles)
                    {
                        string file = Path.Combine(characterDirectory, name);
                        if (File.Exists(file) && TryDelete(file, () => File.Delete(file)))
                            removed++;
                    }
                }
            }

            if (removed > 0)
                _trace?.Trace($"SkinrLeftoverCleanup: removed {removed} SKINR item(s)", printMethod: false);

            return removed;
        }

        private bool TryDelete(string path, Action delete)
        {
            try
            {
                delete();
                return true;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _trace?.Trace($"SkinrLeftoverCleanup: could not remove {path}: {ex.Message}", printMethod: false);
                return false;
            }
        }

        private string[] SafeEnumerateDirectories(string path)
        {
            try
            {
                return Directory.GetDirectories(path);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _trace?.Trace($"SkinrLeftoverCleanup: could not list {path}: {ex.Message}", printMethod: false);
                return Array.Empty<string>();
            }
        }
    }
}
