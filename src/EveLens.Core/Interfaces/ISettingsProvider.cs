// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Threading.Tasks;

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Abstracts access to application settings for SSO credentials, save operations,
    /// and migration state. Replaces direct dependency on the static <c>Settings</c> class
    /// in <c>EveLens.Common</c>.
    /// </summary>
    /// <remarks>
    /// The <see cref="Save"/> method is batched: multiple rapid calls coalesce into a single
    /// write via <c>SmartSettingsManager</c> (debounce window). Use <see cref="SaveImmediateAsync"/>
    /// for critical saves that must flush to disk immediately (e.g., before shutdown).
    ///
    /// Production: <c>SettingsProviderService</c> in <c>EveLens.Common/Services/SettingsProviderService.cs</c>
    /// (delegates to the static <c>Settings</c> class).
    /// Testing: Provide a stub that stores values in memory.
    /// </remarks>
    public interface ISettingsProvider
    {
        /// <summary>
        /// Gets the SSO Client ID for ESI OAuth2 authentication.
        /// Read from the application settings; empty string if not configured.
        /// </summary>
        string SSOClientID { get; }

        /// <summary>
        /// Gets the SSO Client Secret for ESI OAuth2 authentication.
        /// Read from the application settings; empty string if not configured.
        /// </summary>
        string SSOClientSecret { get; }

        /// <summary>
        /// Gets a value indicating whether settings are currently being restored (imported).
        /// While true, certain UI updates and auto-save operations should be suppressed.
        /// </summary>
        bool IsRestoring { get; }

        /// <summary>
        /// Gets a value indicating whether a migration from another fork (e.g., peterhaneve)
        /// was detected during the last settings load. Used to show a one-time migration notice.
        /// </summary>
        bool MigrationFromOtherForkDetected { get; }

        /// <summary>
        /// Queues a batched settings save. Multiple calls within the debounce window
        /// of <c>SmartSettingsManager</c> coalesce into a single write to disk.
        /// </summary>
        void Save();

        /// <summary>
        /// Saves settings to disk immediately, bypassing the batching/debounce window.
        /// Use for critical save points such as application shutdown.
        /// </summary>
        /// <returns>A task that completes when the save is flushed to disk.</returns>
        Task SaveImmediateAsync();
    }
}
