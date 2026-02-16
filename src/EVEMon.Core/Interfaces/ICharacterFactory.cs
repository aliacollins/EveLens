using System;

namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Factory for creating, tracking, and disposing character instances.
    /// Wraps <c>CCPCharacter</c> construction with dependency injection and lifecycle event
    /// publishing, providing the Strangler Fig migration path away from direct
    /// <c>new CCPCharacter()</c> calls.
    /// </summary>
    /// <remarks>
    /// Design decisions:
    /// <list type="bullet">
    ///   <item>Tracks managed characters in a <c>ConcurrentDictionary</c> keyed by EVE character ID.</item>
    ///   <item>Publishes <c>CharacterCreatedEvent</c> and <c>CharacterDisposedEvent</c> via
    ///         <c>IEventAggregator</c> for decoupled lifecycle notification.</item>
    ///   <item>Separates construction from initialization: the factory creates and tracks the
    ///         character but does NOT auto-subscribe it to events. Tests can create characters
    ///         without triggering timer subscriptions.</item>
    ///   <item><see cref="CreateNew"/> sets <c>ForceUpdateBasicFeatures = true</c> so the character
    ///         fetches its data on the first query cycle.</item>
    /// </list>
    ///
    /// Institutional knowledge: Alpha/Omega detection uses
    /// <c>SP > EveConstants.MaxAlphaSkillTraining</c> for Omega,
    /// <c>ActiveLevel &lt; Level</c> for Alpha, with a 0.8x-1.2x training rate margin.
    ///
    /// Production: <c>CharacterFactory</c> in <c>EVEMon.Common/Services/CharacterFactory.cs</c>.
    /// Testing: Use the factory with a <c>NullCharacterServices</c> (from
    /// <c>EVEMon.Tests/TestDoubles/</c>) to create characters without <c>EveMonClient</c>.
    /// </remarks>
    public interface ICharacterFactory
    {
        /// <summary>
        /// Creates a character from serialized settings data (deserialization path).
        /// Publishes <c>CharacterCreatedEvent</c> with <c>FromSerialized = true</c>.
        /// </summary>
        /// <param name="identity">The character identity to associate.</param>
        /// <param name="serializedData">The serialized data (runtime type: <c>SerializableCCPCharacter</c>).</param>
        /// <returns>The character identity of the created character.</returns>
        ICharacterIdentity CreateFromSerialized(ICharacterIdentity identity, object serializedData);

        /// <summary>
        /// Creates a new character with default settings (new-character path).
        /// Sets <c>ForceUpdateBasicFeatures = true</c> to trigger an immediate ESI data fetch.
        /// Publishes <c>CharacterCreatedEvent</c> with <c>FromSerialized = false</c>.
        /// </summary>
        /// <param name="identity">The character identity to associate.</param>
        /// <returns>The character identity of the created character.</returns>
        ICharacterIdentity CreateNew(ICharacterIdentity identity);

        /// <summary>
        /// Disposes a character, removing it from the managed set and publishing
        /// <c>CharacterDisposedEvent</c>. No-op if the character is not tracked.
        /// </summary>
        /// <param name="identity">The character identity to dispose.</param>
        void DisposeCharacter(ICharacterIdentity identity);

        /// <summary>
        /// Gets the number of characters currently tracked by this factory.
        /// Useful for diagnostics and test assertions.
        /// </summary>
        int ManagedCount { get; }
    }
}
