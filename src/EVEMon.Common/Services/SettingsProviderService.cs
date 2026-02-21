// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Threading.Tasks;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for the static <see cref="Settings"/> class.
    /// Implements <see cref="ISettingsProvider"/> by delegating to the existing static properties.
    /// </summary>
    internal sealed class SettingsProviderService : ISettingsProvider
    {
        /// <inheritdoc />
        public string SSOClientID => Settings.SSOClientID;

        /// <inheritdoc />
        public string SSOClientSecret => Settings.SSOClientSecret;

        /// <inheritdoc />
        public bool IsRestoring => Settings.IsRestoring;

        /// <inheritdoc />
        public bool MigrationFromOtherForkDetected => Settings.MigrationFromOtherForkDetected;

        /// <inheritdoc />
        public void Save()
        {
            Settings.Save();
        }

        /// <inheritdoc />
        public Task SaveImmediateAsync()
        {
            return Settings.SaveImmediateAsync();
        }
    }
}
