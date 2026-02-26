// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Core.Interfaces;

namespace EveLens.Core.Events
{
    /// <summary>
    /// Published when a character is created via <see cref="ICharacterFactory"/>.
    /// Subscribers can use this to perform additional initialization (e.g., registering
    /// the character with the query scheduler, updating UI character lists).
    /// </summary>
    /// <remarks>
    /// Published by <c>CharacterFactory</c> in <c>EveLens.Common/Services/CharacterFactory.cs</c>
    /// immediately after the character is tracked in the factory's managed set.
    ///
    /// The <see cref="FromSerialized"/> flag distinguishes between characters loaded from
    /// saved settings (true) and newly created characters via SSO (false). New characters
    /// typically need <c>ForceUpdateBasicFeatures = true</c> for immediate ESI fetch.
    /// </remarks>
    public sealed class CharacterCreatedEvent
    {
        /// <summary>
        /// Gets the identity of the created character.
        /// </summary>
        public ICharacterIdentity Identity { get; }

        /// <summary>
        /// Gets a value indicating whether the character was created from serialized
        /// settings data (true) or is a brand-new character via SSO authentication (false).
        /// </summary>
        public bool FromSerialized { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterCreatedEvent"/> class.
        /// </summary>
        /// <param name="identity">The identity of the created character (must not be null).</param>
        /// <param name="fromSerialized">True if loaded from saved settings; false if newly created.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="identity"/> is null.</exception>
        public CharacterCreatedEvent(ICharacterIdentity identity, bool fromSerialized)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
            FromSerialized = fromSerialized;
        }
    }

    /// <summary>
    /// Published when a character is disposed via <see cref="ICharacterFactory.DisposeCharacter"/>.
    /// Subscribers should clean up any resources associated with the character
    /// (e.g., unregistering from the query scheduler, removing UI elements).
    /// </summary>
    /// <remarks>
    /// Published by <c>CharacterFactory</c> in <c>EveLens.Common/Services/CharacterFactory.cs</c>
    /// after the character is removed from the factory's managed set.
    /// </remarks>
    public sealed class CharacterDisposedEvent
    {
        /// <summary>
        /// Gets the identity of the disposed character.
        /// </summary>
        public ICharacterIdentity Identity { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterDisposedEvent"/> class.
        /// </summary>
        /// <param name="identity">The identity of the disposed character (must not be null).</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="identity"/> is null.</exception>
        public CharacterDisposedEvent(ICharacterIdentity identity)
        {
            Identity = identity ?? throw new ArgumentNullException(nameof(identity));
        }
    }

    /// <summary>
    /// Published when the active character tab changes in the main window.
    /// Used by the query tier system to activate Tier 1 (Detail) monitors
    /// for the visible character and deactivate them for background characters.
    /// </summary>
    public sealed class ActiveCharacterChangedEvent
    {
        /// <summary>
        /// Gets the character ID of the newly active character, or 0 if no character is selected.
        /// </summary>
        public long CharacterId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ActiveCharacterChangedEvent"/> class.
        /// </summary>
        /// <param name="characterId">The character ID of the active character, or 0 for none.</param>
        public ActiveCharacterChangedEvent(long characterId) => CharacterId = characterId;
    }
}
