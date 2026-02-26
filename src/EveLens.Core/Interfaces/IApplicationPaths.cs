// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Provides access to application directory and file paths.
    /// Replaces direct dependency on <c>EveLensClient.EveLensDataDir</c>,
    /// <c>EveLensClient.XmlCacheDir</c>, <c>EveLensClient.ImageCacheDir</c>,
    /// and related static path properties.
    /// </summary>
    /// <remarks>
    /// Path values are snapshotted once during <c>AppServices.Bootstrap()</c> via
    /// <c>ApplicationPathsAdapter.SnapshotFromEveLensClient()</c>. After the snapshot,
    /// the adapter no longer reads from <c>EveLensClient</c> at runtime, making it safe
    /// for use in any layer without coupling to the static god object.
    ///
    /// Before the snapshot (early bootstrap or tests), the adapter falls back to reading
    /// from <c>EveLensClient</c> static properties, returning <c>string.Empty</c> if those
    /// are also null.
    ///
    /// All paths are absolute and use the platform's directory separator.
    /// Typical root: <c>%APPDATA%\EveLens\</c> on Windows.
    ///
    /// Production: <c>ApplicationPathsAdapter</c> in <c>EveLens.Common/Services/ApplicationPathsAdapter.cs</c>.
    /// Testing: Provide a stub pointing to a temp directory, or use the adapter
    /// without calling <c>SnapshotFromEveLensClient()</c> (paths will be empty strings).
    /// </remarks>
    public interface IApplicationPaths
    {
        /// <summary>
        /// Gets the root application data directory (e.g., <c>%APPDATA%\EveLens</c>).
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
        /// Gets the full path to the settings JSON file (e.g., <c>%APPDATA%\EveLens\settings.json</c>).
        /// </summary>
        string SettingsFilePath { get; }

        /// <summary>
        /// Gets the full path to the trace/log file for diagnostic output.
        /// </summary>
        string TraceFilePath { get; }
    }
}
