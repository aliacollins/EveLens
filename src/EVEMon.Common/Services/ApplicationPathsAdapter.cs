using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Stores application paths as snapshot values captured at startup.
    /// Paths are computed once by <c>EveMonClient.InitializeFileSystemPaths()</c>
    /// and then snapshotted here by <c>AppServices.Bootstrap()</c>, breaking
    /// the runtime dependency on the EveMonClient static properties.
    /// Falls back to EveMonClient if snapshot hasn't been taken yet
    /// (e.g., during early bootstrap or in tests).
    /// </summary>
    public sealed class ApplicationPathsAdapter : IApplicationPaths
    {
        private string? _dataDirectory;
        private string? _xmlCacheDirectory;
        private string? _imageCacheDirectory;
        private string? _settingsFilePath;
        private string? _traceFilePath;

        public string DataDirectory => _dataDirectory ?? EveMonClient.EVEMonDataDir ?? string.Empty;

        public string XmlCacheDirectory => _xmlCacheDirectory ?? EveMonClient.EVEMonXmlCacheDir ?? string.Empty;

        public string ImageCacheDirectory => _imageCacheDirectory ?? EveMonClient.EVEMonImageCacheDir ?? string.Empty;

        public string SettingsFilePath => _settingsFilePath ?? EveMonClient.SettingsFileNameFullPath ?? string.Empty;

        public string TraceFilePath => _traceFilePath ?? EveMonClient.TraceFileNameFullPath ?? string.Empty;

        /// <summary>
        /// Snapshots the current paths from EveMonClient static properties.
        /// Called once during <c>AppServices.Bootstrap()</c> after filesystem
        /// paths have been initialized. After this call, the adapter no longer
        /// reads from EveMonClient at runtime.
        /// </summary>
        internal void SnapshotFromEveMonClient()
        {
            _dataDirectory = EveMonClient.EVEMonDataDir;
            _xmlCacheDirectory = EveMonClient.EVEMonXmlCacheDir;
            _imageCacheDirectory = EveMonClient.EVEMonImageCacheDir;
            _settingsFilePath = EveMonClient.SettingsFileNameFullPath;
            _traceFilePath = EveMonClient.TraceFileNameFullPath;
        }
    }
}
