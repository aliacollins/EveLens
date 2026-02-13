using System;
using EVEMon.Core.Interfaces;

namespace EVEMon.Core.Events
{
    /// <summary>
    /// Published when a character is created via <see cref="ICharacterFactory"/>.
    /// </summary>
    public sealed class CharacterCreatedEvent
    {
        /// <summary>
        /// Gets the identity of the created character.
        /// </summary>
        public ICharacterIdentity Identity { get; }

        /// <summary>
        /// Gets a value indicating whether the character was created from serialized data
        /// (as opposed to being newly created).
        /// </summary>
        public bool FromSerialized { get; }

        public CharacterCreatedEvent(ICharacterIdentity identity, bool fromSerialized)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            FromSerialized = fromSerialized;
        }
    }

    /// <summary>
    /// Published when a character is disposed via <see cref="ICharacterFactory"/>.
    /// </summary>
    public sealed class CharacterDisposedEvent
    {
        /// <summary>
        /// Gets the identity of the disposed character.
        /// </summary>
        public ICharacterIdentity Identity { get; }

        public CharacterDisposedEvent(ICharacterIdentity identity)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }
}
