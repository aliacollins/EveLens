// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Extensions;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;

namespace EveLens.Common.Models.Collections
{
    public sealed class CharacterIdentityIgnoreList : ReadonlyCollection<CharacterIdentity>
    {
        private readonly ESIKey m_owner;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="apiKey"></param>
        internal CharacterIdentityIgnoreList(ESIKey apiKey)
        {
            m_owner = apiKey;
        }

        /// <summary>
        /// Checks whether the given character's associated identity is contained in this list.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">character</exception>
        public bool Contains(Character character)
        {
            character.ThrowIfNull(nameof(character));

            return Contains(character.Identity);
        }

        /// <summary>
        /// Removes this character and attempts to return a CCP character.
        /// The resulting character will be the existing one matching this id, or if it does not exist, a new character.
        /// If the identity was not in the collection, the method won't attempt to create a new character and will return either the existing one or null.
        /// </summary>
        /// <param name="identity">The identity.</param>
        /// <exception cref="System.ArgumentNullException">id</exception>
        public void Remove(CharacterIdentity identity)
        {
            identity.ThrowIfNull(nameof(identity));

            // If the id was not in list, returns the existing character or null if it does not exist
            if (!Items.Remove(identity))
                return;

            // If character exists, returns it
            if (identity.CCPCharacter != null)
                return;

            // Create a new CCP character
            AppServices.Characters.Add(AppServices.CharacterFactory.CreateCCPCharacter(identity));
        }

        /// <summary>
        /// Adds a character to the ignore list and, if it belonged to this API key, removes it from the global collection
        /// (all associated data and plans won't be written on next serialization !).
        /// </summary>
        /// <param name="character">The character.</param>
        /// <exception cref="System.ArgumentNullException">character</exception>
        public void Add(Character character)
        {
            character.ThrowIfNull(nameof(character));

            CharacterIdentity id = character.Identity;
            if (Items.Contains(id))
                return;

            Items.Add(id);

            // If the identity was belonging to this API key, remove the character (won't be serialized anymore !)
            if (id.ESIKeys.Contains(m_owner))
                AppServices.Characters.Remove(character);
        }

        /// <summary>
        /// Imports the deserialization objects.
        /// </summary>
        /// <param name="serialIDList"></param>
        internal void Import(IEnumerable<SerializableCharacterIdentity> serialIDList)
        {
            Items.Clear();
            foreach (CharacterIdentity id in serialIDList.Select(
                serialID => AppServices.CharacterIdentities[serialID.ID] ??
                            AppServices.CharacterIdentities.Add(serialID.ID, serialID.Name)))
            {
                Items.Add(id);
            }
        }

        /// <summary>
        /// Create serialization objects.
        /// </summary>
        /// <returns></returns>
        internal IEnumerable<SerializableCharacterIdentity> Export()
            => Items.Select(id => new SerializableCharacterIdentity
            {
                ID = id.CharacterID,
                Name = id.CharacterName,
            });
    }
}
