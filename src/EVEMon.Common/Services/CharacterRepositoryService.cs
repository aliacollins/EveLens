// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for <see cref="AppServices.Characters"/> and
    /// <see cref="AppServices.MonitoredCharacters"/>.
    /// Implements <see cref="ICharacterRepository"/> by delegating to the existing collections.
    /// </summary>
    internal sealed class CharacterRepositoryService : ICharacterRepository
    {
        /// <inheritdoc />
        public IReadOnlyList<ICharacterIdentity> Characters
        {
            get
            {
                var chars = AppServices.Characters;
                if (chars == null)
                    return new List<ICharacterIdentity>();
                return chars.OfType<ICharacterIdentity>().ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<ICharacterIdentity> MonitoredCharacters
        {
            get
            {
                var monitored = AppServices.MonitoredCharacters;
                if (monitored == null)
                    return new List<ICharacterIdentity>();
                return monitored.OfType<ICharacterIdentity>().ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public ICharacterIdentity GetByGuid(string guid)
        {
            return AppServices.Characters?[guid] as ICharacterIdentity;
        }

        /// <inheritdoc />
        public int Count => AppServices.Characters?.Count ?? 0;

        /// <inheritdoc />
        public IEnumerable<string> GetKnownLabels()
        {
            return AppServices.Characters?.GetKnownLabels() ?? Enumerable.Empty<string>();
        }

        /// <inheritdoc />
        public bool IsMonitored(ICharacterIdentity character)
        {
            var monitored = AppServices.MonitoredCharacters;
            if (monitored == null || character == null)
                return false;
            return monitored.Any(c => c.Guid == character.Guid);
        }

        /// <inheritdoc />
        public void SetMonitored(ICharacterIdentity character, bool value)
        {
            if (character == null)
                return;
            // Delegate to the real Character model's Monitored setter via the collection
            var realCharacter = AppServices.Characters?[character.Guid.ToString()];
            if (realCharacter != null)
                AppServices.MonitoredCharacters.OnCharacterMonitoringChanged(realCharacter, value);
        }
    }
}
