using System.Threading.Tasks;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts access to application settings.
    /// Replaces direct dependency on the static <c>Settings</c> class.
    /// </summary>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Gets the SSO Client ID for ESI authentication.
        /// </summary>
        string SSOClientID { get; }

        /// <summary>
        /// Gets the SSO Client Secret for ESI authentication.
        /// </summary>
        string SSOClientSecret { get; }

        /// <summary>
        /// Gets a value indicating whether settings are currently being restored.
        /// </summary>
        bool IsRestoring { get; }

        /// <summary>
        /// Gets a value indicating whether a migration from another fork was detected.
        /// </summary>
        bool MigrationFromOtherForkDetected { get; }

        /// <summary>
        /// Saves settings asynchronously (batched).
        /// </summary>
        void Save();

        /// <summary>
        /// Saves settings immediately (bypasses batching).
        /// </summary>
        Task SaveImmediateAsync();
    }
}
