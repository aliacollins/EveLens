namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides access to application directory and file paths.
    /// Replaces direct dependency on <c>EveMonClient.EVEMonDataDir</c>,
    /// <c>EveMonClient.XmlCacheDir</c>, <c>EveMonClient.ImageCacheDir</c>,
    /// and related static path properties.
    /// </summary>
    /// <remarks>
    /// Path values are snapshotted once during <c>AppServices.Bootstrap()</c> via
    /// <c>ApplicationPathsAdapter.SnapshotFromEveMonClient()</c>. After the snapshot,
    /// the adapter no longer reads from <c>EveMonClient</c> at runtime, making it safe
    /// for use in any layer without coupling to the static god object.
    ///
    /// Before the snapshot (early bootstrap or tests), the adapter falls back to reading
    /// from <c>EveMonClient</c> static properties, returning <c>string.Empty</c> if those
    /// are also null.
    ///
    /// All paths are absolute and use the platform's directory separator.
    /// Typical root: <c>%APPDATA%\EVEMon\</c> on Windows.
    ///
    /// Production: <c>ApplicationPathsAdapter</c> in <c>EVEMon.Common/Services/ApplicationPathsAdapter.cs</c>.
    /// Testing: Provide a stub pointing to a temp directory, or use the adapter
    /// without calling <c>SnapshotFromEveMonClient()</c> (paths will be empty strings).
    /// </remarks>
    public interface IApplicationPaths
    {
        /// <summary>
        /// Gets the root application data directory (e.g., <c>%APPDATA%\EVEMon</c>).
        /// Contains settings, cache subdirectories, and log files.
        /// </summary>
        string DataDirectory { get; }

        /// <summary>
        /// Gets the XML cache directory path, used for caching ESI API responses
        /// and static data files.
        /// </summary>
        string XmlCacheDirectory { get; }

        /// <summary>
        /// Gets the image cache directory path, used for caching character portraits,
        /// item icons, and other downloaded images.
        /// </summary>
        string ImageCacheDirectory { get; }

        /// <summary>
        /// Gets the full path to the settings JSON file (e.g., <c>%APPDATA%\EVEMon\settings.json</c>).
        /// </summary>
        string SettingsFilePath { get; }

        /// <summary>
        /// Gets the full path to the trace/log file for diagnostic output.
        /// </summary>
        string TraceFilePath { get; }
    }
}
