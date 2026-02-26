// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Stores application paths as snapshot values captured at startup.
    /// Paths are computed once by <c>EveLensClient.InitializeFileSystemPaths()</c>
    /// and then snapshotted here by <c>AppServices.Bootstrap()</c>, breaking
    /// the runtime dependency on the EveLensClient static properties.
    /// Falls back to EveLensClient if snapshot hasn't been taken yet
    /// (e.g., during early bootstrap or in tests).
    /// </summary>
    public sealed class ApplicationPathsAdapter : IApplicationPaths
    {
        private string? _dataDirectory;
        private string? _xmlCacheDirectory;
        private string? _imageCacheDirectory;
        private string? _settingsFilePath;
        private string? _traceFilePath;

        public string DataDirectory => _dataDirectory ?? EveLensClient.EveLensDataDir ?? string.Empty;

        public string XmlCacheDirectory => _xmlCacheDirectory ?? EveLensClient.EveLensXmlCacheDir ?? string.Empty;

        public string ImageCacheDirectory => _imageCacheDirectory ?? EveLensClient.EveLensImageCacheDir ?? string.Empty;

        public string SettingsFilePath => _settingsFilePath ?? EveLensClient.SettingsFileNameFullPath ?? string.Empty;

        public string TraceFilePath => _traceFilePath ?? EveLensClient.TraceFileNameFullPath ?? string.Empty;

        /// <summary>
        /// Snapshots the current paths from EveLensClient static properties.
        /// Called once during <c>AppServices.Bootstrap()</c> after filesystem
        /// paths have been initialized. After this call, the adapter no longer
        /// reads from EveLensClient at runtime.
        /// </summary>
        internal void SnapshotFromEveLensClient()
        {
            _dataDirectory = EveLensClient.EveLensDataDir;
            _xmlCacheDirectory = EveLensClient.EveLensXmlCacheDir;
            _imageCacheDirectory = EveLensClient.EveLensImageCacheDir;
            _settingsFilePath = EveLensClient.SettingsFileNameFullPath;
            _traceFilePath = EveLensClient.TraceFileNameFullPath;
        }
    }
}
