using System.Collections.Generic;
using System.Linq;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Strangler Fig wrapper for <see cref="EveMonClient.Characters"/> and
    /// <see cref="EveMonClient.MonitoredCharacters"/>.
    /// Implements <see cref="ICharacterRepository"/> by delegating to the existing collections.
    /// </summary>
    internal sealed class CharacterRepositoryService : ICharacterRepository
    {
        /// <inheritdoc />
        public IReadOnlyList<ICharacterIdentity> Characters
        {
            get
            {
                var chars = EveMonClient.Characters;
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
                var monitored = EveMonClient.MonitoredCharacters;
                if (monitored == null)
                    return new List<ICharacterIdentity>();
                return monitored.OfType<ICharacterIdentity>().ToList().AsReadOnly();
            }
        }

        /// <inheritdoc />
        public ICharacterIdentity GetByGuid(string guid)
        {
            return EveMonClient.Characters?[guid] as ICharacterIdentity;
        }

        /// <inheritdoc />
        public int Count => EveMonClient.Characters?.Count ?? 0;
    }
}
