using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides read-only access to the character collection.
    /// Separated from <see cref="ICharacterWriter"/> to allow consumers
    /// to depend only on read operations.
    /// </summary>
    public interface ICharacterReader
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

        /// <summary>
        /// Gets all distinct labels assigned to characters.
        /// </summary>
        IEnumerable<string> GetKnownLabels();

        /// <summary>
        /// Gets whether the specified character is currently monitored.
        /// </summary>
        /// <param name="character">The character identity to check.</param>
        /// <returns>True if the character is monitored.</returns>
        bool IsMonitored(ICharacterIdentity character);
    }
}
