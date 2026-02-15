namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts access to application directory paths.
    /// Replaces direct dependency on <c>EveMonClient.EVEMonDataDir</c>,
    /// <c>EveMonClient.XmlCacheDir</c>, and <c>EveMonClient.ImageCacheDir</c>.
    /// </summary>
    public interface IApplicationPaths
    {
        /// <summary>
        /// Gets the path to the application data directory.
        /// </summary>
        string DataDirectory { get; }

        /// <summary>
        /// Gets the path to the XML cache directory.
        /// </summary>
        string XmlCacheDirectory { get; }

        /// <summary>
        /// Gets the path to the image cache directory.
        /// </summary>
        string ImageCacheDirectory { get; }

        /// <summary>
        /// Gets the path to the settings file.
        /// </summary>
        string SettingsFilePath { get; }

        /// <summary>
        /// Gets the path to the trace/log file.
        /// </summary>
        string TraceFilePath { get; }
    }
}
