using System.Collections.Generic;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Provides read-only access to the character collection.
    /// Separated from <see cref="ICharacterWriter"/> so that consumers that only need to
    /// enumerate or look up characters do not depend on mutation operations.
    /// </summary>
    /// <remarks>
    /// All list-returning properties create snapshot copies (via <c>ToList().AsReadOnly()</c>),
    /// so they are safe to iterate while the underlying collection changes. However, they
    /// represent a point-in-time view and may become stale.
    ///
    /// Production: <c>CharacterRepositoryService</c> in <c>EVEMon.Common/Services/CharacterRepositoryService.cs</c>
    /// (via the combined <see cref="ICharacterRepository"/> interface).
    /// Testing: Implement with a simple <c>List&lt;ICharacterIdentity&gt;</c>.
    /// </remarks>
    public interface ICharacterReader
    {
        /// <summary>
        /// Gets a snapshot of all characters (both monitored and unmonitored).
        /// Returns an empty list if the collection is not yet initialized.
        /// </summary>
        IReadOnlyList<ICharacterIdentity> Characters { get; }

        /// <summary>
        /// Gets a snapshot of all actively monitored characters.
        /// Monitored characters have their ESI data polled on the scheduler tick cycle.
        /// Returns an empty list if the collection is not yet initialized.
        /// </summary>
        IReadOnlyList<ICharacterIdentity> MonitoredCharacters { get; }

        /// <summary>
        /// Looks up a character by its string GUID (e.g., <c>Character.Guid.ToString()</c>).
        /// Returns null if no character matches the given GUID.
        /// </summary>
        /// <param name="guid">The character's GUID as a string.</param>
        /// <returns>The matching character identity, or null if not found.</returns>
        ICharacterIdentity GetByGuid(string guid);

        /// <summary>
        /// Gets the total number of characters in the collection.
        /// Returns 0 if the collection is not yet initialized.
        /// </summary>
        int Count { get; }

        /// <summary>
        /// Gets all distinct label strings assigned to characters across the collection.
        /// Labels are user-defined grouping tags displayed in the UI sidebar.
        /// </summary>
        IEnumerable<string> GetKnownLabels();

        /// <summary>
        /// Checks whether the specified character is in the monitored set.
        /// Returns false if the character is null or the collection is uninitialized.
        /// </summary>
        /// <param name="character">The character identity to check.</param>
        /// <returns>True if the character is currently monitored.</returns>
        bool IsMonitored(ICharacterIdentity character);
    }
}
