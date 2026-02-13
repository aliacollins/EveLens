using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Factory for creating and disposing character instances.
    /// Wraps CCPCharacter construction with dependency injection and testability.
    /// </summary>
    public interface ICharacterFactory
    {
        /// <summary>
        /// Creates a character from serialized settings data.
        /// </summary>
        /// <param name="identity">The character identity.</param>
        /// <param name="serializedData">The serialized character data (SerializableCCPCharacter).</param>
        /// <returns>The character identity of the created character.</returns>
        ICharacterIdentity CreateFromSerialized(ICharacterIdentity identity, object serializedData);

        /// <summary>
        /// Creates a new character with default settings.
        /// Sets ForceUpdateBasicFeatures to true for immediate data fetch.
        /// </summary>
        /// <param name="identity">The character identity.</param>
        /// <returns>The character identity of the created character.</returns>
        ICharacterIdentity CreateNew(ICharacterIdentity identity);

        /// <summary>
        /// Disposes a character, cleaning up event subscriptions and resources.
        /// </summary>
        /// <param name="identity">The character identity to dispose.</param>
        void DisposeCharacter(ICharacterIdentity identity);

        /// <summary>
        /// Gets the number of characters managed by this factory.
        /// </summary>
        int ManagedCount { get; }
    }
}
