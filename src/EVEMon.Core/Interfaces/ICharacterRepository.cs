using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts access to the character collection.
    /// Replaces direct dependency on <c>EveMonClient.Characters</c> and
    /// <c>EveMonClient.MonitoredCharacters</c>.
    /// </summary>
    public interface ICharacterRepository
    {
        /// <summary>
        /// Gets all characters.
        /// </summary>
        IReadOnlyList<ICharacterIdentity> Characters { get; }

        /// <summary>
        /// Gets all monitored (actively updating) characters.
        /// </summary>
        IReadOnlyList<ICharacterIdentity> MonitoredCharacters { get; }

        /// <summary>
        /// Gets a character by its unique identifier.
        /// </summary>
        /// <param name="guid">The character's GUID.</param>
        /// <returns>The character, or null if not found.</returns>
        ICharacterIdentity GetByGuid(string guid);

        /// <summary>
        /// Gets the total number of characters.
        /// </summary>
        int Count { get; }
    }
}
