using System;
using System.Collections.Concurrent;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Factory for creating and disposing character instances.
    /// Provides a testable abstraction over CCPCharacter construction.
    /// </summary>
    /// <remarks>
    /// This is the NEW code path (Strangler Fig pattern). The old CCPCharacter constructor
    /// continues to work unchanged. This factory provides the abstraction layer for new code.
    ///
    /// Design decisions:
    /// - Tracks character identities via ConcurrentDictionary for thread safety.
    /// - Publishes lifecycle events via IEventAggregator for decoupled notification.
    /// - Separates construction from initialization: the factory creates/tracks the character
    ///   but does NOT auto-subscribe to events. An explicit Initialize() pattern means tests
    ///   can create characters without triggering event subscriptions.
    /// - CreateCCPCharacter / CreateCCPCharacterFromSerialized are the SINGLE entry points
    ///   for all CCPCharacter instantiation (production code should not call new CCPCharacter directly).
    ///
    /// Institutional knowledge preserved:
    /// - Lazy collection initialization pattern (character collections are lazy until accessed).
    /// - ForceUpdateBasicFeatures flag for new characters (triggers immediate API fetch).
    /// - Alpha/Omega detection: SP > EveConstants.MaxAlphaSkillTraining = Omega,
    ///   ActiveLevel &lt; Level = Alpha, training rate 0.8-1.2x margin.
    /// </remarks>
    internal sealed class CharacterFactory : ICharacterFactory
    {
        private readonly ICharacterRepository _repository;
        private readonly IEventAggregator _eventAggregator;
        private readonly ICharacterServices _services;

        /// <summary>
        /// Tracks managed character identities by their EVE Online character ID.
        /// Thread-safe for concurrent character creation scenarios (e.g., bulk import).
        /// </summary>
        private readonly ConcurrentDictionary<long, ManagedCharacterEntry> _managedCharacters
            = new ConcurrentDictionary<long, ManagedCharacterEntry>();

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterFactory"/> class.
        /// </summary>
        /// <param name="repository">The character repository for looking up existing characters.</param>
        /// <param name="eventAggregator">The event aggregator for publishing lifecycle events.</param>
        /// <param name="services">The character services for CCPCharacter construction.</param>
        public CharacterFactory(ICharacterRepository repository, IEventAggregator eventAggregator,
            ICharacterServices services = null)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _services = services;
        }

        /// <inheritdoc />
        public int ManagedCount => _managedCharacters.Count;

        /// <inheritdoc />
        public ICharacterIdentity CreateFromSerialized(ICharacterIdentity identity, object serializedData)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (serializedData == null)
                throw new ArgumentNullException(nameof(serializedData));

            var entry = new ManagedCharacterEntry(identity, fromSerialized: true);
            _managedCharacters[identity.CharacterID] = entry;

            _eventAggregator.Publish(new CharacterCreatedEvent(identity, fromSerialized: true));

            return identity;
        }

        /// <inheritdoc />
        public ICharacterIdentity CreateNew(ICharacterIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            // ForceUpdateBasicFeatures = true for new characters.
            // This flag is stored in the entry so consumers can check it when
            // wiring up the actual CCPCharacter in the old code path.
            var entry = new ManagedCharacterEntry(identity, fromSerialized: false)
            {
                ForceUpdateBasicFeatures = true
            };
            _managedCharacters[identity.CharacterID] = entry;

            _eventAggregator.Publish(new CharacterCreatedEvent(identity, fromSerialized: false));

            return identity;
        }

        /// <summary>
        /// Creates a new CCPCharacter for a brand-new identity (no serialized data).
        /// This is the single entry point for creating new CCP characters.
        /// </summary>
        /// <param name="identity">The character identity.</param>
        /// <returns>A new CCPCharacter instance, tracked by this factory.</returns>
        internal CCPCharacter CreateCCPCharacter(CharacterIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            var services = _services ?? EveMonClientCharacterServices.Instance;
            var character = new CCPCharacter(identity, services);

            var entry = new ManagedCharacterEntry(character, fromSerialized: false)
            {
                ForceUpdateBasicFeatures = true
            };
            _managedCharacters[identity.CharacterID] = entry;
            _eventAggregator.Publish(new CharacterCreatedEvent(character, fromSerialized: false));

            return character;
        }

        /// <summary>
        /// Creates a CCPCharacter from serialized settings data.
        /// This is the single entry point for deserializing CCP characters.
        /// </summary>
        /// <param name="identity">The character identity.</param>
        /// <param name="serial">The serialized character data.</param>
        /// <returns>A new CCPCharacter instance, tracked by this factory.</returns>
        internal CCPCharacter CreateCCPCharacterFromSerialized(CharacterIdentity identity, SerializableCCPCharacter serial)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));
            if (serial == null)
                throw new ArgumentNullException(nameof(serial));

            var services = _services ?? EveMonClientCharacterServices.Instance;
            var character = new CCPCharacter(identity, serial, services);

            var entry = new ManagedCharacterEntry(character, fromSerialized: true);
            _managedCharacters[identity.CharacterID] = entry;
            _eventAggregator.Publish(new CharacterCreatedEvent(character, fromSerialized: true));

            return character;
        }

        /// <inheritdoc />
        public void DisposeCharacter(ICharacterIdentity identity)
        {
            if (identity == null)
                throw new ArgumentNullException(nameof(identity));

            if (_managedCharacters.TryRemove(identity.CharacterID, out _))
            {
                _eventAggregator.Publish(new CharacterDisposedEvent(identity));
            }
        }

        /// <summary>
        /// Internal entry tracking a managed character's metadata.
        /// </summary>
        private sealed class ManagedCharacterEntry
        {
            public ICharacterIdentity Identity { get; }
            public bool FromSerialized { get; }

            /// <summary>
            /// When true, the character should fetch basic features immediately
            /// on first query cycle. Set for newly created characters (not deserialized ones).
            /// </summary>
            public bool ForceUpdateBasicFeatures { get; set; }

            public ManagedCharacterEntry(ICharacterIdentity identity, bool fromSerialized)
            {
                Identity = identity;
                FromSerialized = fromSerialized;
            }
        }
    }
}
