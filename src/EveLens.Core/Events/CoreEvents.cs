// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Events
{
    /// <summary>
    /// Base class for character-scoped events bridged from <c>EveLensClient</c>.
    /// Uses only primitive types (<c>long</c>, <c>string</c>) so that the Core assembly
    /// does not depend on the <c>Character</c> model in <c>EveLens.Common</c>.
    /// </summary>
    /// <remarks>
    /// Each concrete subclass represents a specific ESI data type update for a character.
    /// Events carry the character's EVE ID and name so subscribers can filter or display
    /// information without needing the full <c>Character</c> object.
    ///
    /// These are published by <c>EveLensClientCharacterServices</c> (in
    /// <c>EveLens.Common/Services/</c>) when the corresponding <c>QueryMonitor</c> completes.
    /// They are the Core-layer equivalents of the typed events in
    /// <c>EveLens.Common.Events</c> (which carry the full <c>Character</c> reference).
    /// </remarks>
    public abstract class CharacterEventBase
    {
        /// <summary>
        /// Gets the EVE Online character ID of the character whose data was updated.
        /// </summary>
        public long CharacterID { get; }

        /// <summary>
        /// Gets the display name of the character whose data was updated.
        /// Never null (defaults to <c>string.Empty</c> if the name was null at construction).
        /// </summary>
        public string CharacterName { get; }

        /// <summary>
        /// Initializes a new instance of a character event.
        /// </summary>
        /// <param name="characterId">The EVE Online character ID.</param>
        /// <param name="characterName">The character's display name (null is coerced to empty).</param>
        protected CharacterEventBase(long characterId, string characterName)
        {
            CharacterID = characterId;
            CharacterName = characterName ?? string.Empty;
        }
    }

    // ---- Character update events ----
    // Each event is published when the corresponding ESI query monitor completes for a character.

    /// <summary>
    /// Published when a character's core data (skills, attributes, wallet) has been updated
    /// from ESI. Triggered after the character sheet query monitor completes.
    /// </summary>
    public sealed class CharacterUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's public info (corporation, alliance, security status)
    /// has been updated from ESI.
    /// </summary>
    public sealed class CharacterInfoUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterInfoUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's skill queue has been updated from ESI.
    /// Also triggers <c>Character.UpdateAccountStatus()</c> to recalculate Alpha/Omega state.
    /// </summary>
    public sealed class CharacterSkillQueueUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterSkillQueueUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's asset list has been updated from ESI.
    /// </summary>
    public sealed class CharacterAssetsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterAssetsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's market orders have been updated from ESI.
    /// </summary>
    public sealed class CharacterMarketOrdersUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterMarketOrdersUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's contracts have been updated from ESI.
    /// </summary>
    public sealed class CharacterContractsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterContractsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's industry jobs have been updated from ESI.
    /// </summary>
    public sealed class CharacterIndustryJobsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterIndustryJobsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's EVE mail has been updated from ESI.
    /// </summary>
    public sealed class CharacterMailUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterMailUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's in-game notifications have been updated from ESI.
    /// </summary>
    public sealed class CharacterNotificationsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterNotificationsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's planetary interaction (PI) data has been updated from ESI.
    /// </summary>
    public sealed class CharacterPlanetaryUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterPlanetaryUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's standings (NPC corporation/faction) have been updated from ESI.
    /// </summary>
    public sealed class CharacterStandingsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterStandingsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's research agents (datacores) have been updated from ESI.
    /// </summary>
    public sealed class CharacterResearchUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterResearchUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's kill log (killmails) has been updated from ESI.
    /// </summary>
    public sealed class CharacterKillLogUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterKillLogUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's contact list has been updated from ESI.
    /// </summary>
    public sealed class CharacterContactsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterContactsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's medals have been updated from ESI.
    /// </summary>
    public sealed class CharacterMedalsUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterMedalsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's calendar events have been updated from ESI.
    /// </summary>
    public sealed class CharacterCalendarUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterCalendarUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    /// <summary>
    /// Published when a character's loyalty point balances have been updated from ESI.
    /// </summary>
    public sealed class CharacterLoyaltyUpdatedEvent : CharacterEventBase
    {
        /// <summary>Initializes a new instance with the character's ID and name.</summary>
        public CharacterLoyaltyUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    // ---- Non-character (global) events ----

    /// <summary>
    /// Published when application settings have been changed and saved.
    /// Subscribers should re-read any cached setting values.
    /// </summary>
    /// <remarks>
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// Parameterless because the event signals "settings changed" without specifying which setting.
    /// </remarks>
    public sealed class SettingsChangedEvent
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly SettingsChangedEvent Instance = new SettingsChangedEvent();
    }

    /// <summary>
    /// Published when the ESI key collection has changed (key added, removed, or updated).
    /// UI should refresh any key-dependent views (e.g., character list, account management).
    /// </summary>
    /// <remarks>
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// </remarks>
    public sealed class ESIKeyCollectionChangedEvent
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly ESIKeyCollectionChangedEvent Instance = new ESIKeyCollectionChangedEvent();
    }

    /// <summary>
    /// Published when the EVE server status has been updated (online/offline, player count).
    /// Fired after the thirty-second server status check completes.
    /// </summary>
    /// <remarks>
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// </remarks>
    public sealed class ServerStatusUpdatedEvent
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly ServerStatusUpdatedEvent Instance = new ServerStatusUpdatedEvent();
    }

    /// <summary>
    /// Published when the character collection has changed (character added or removed).
    /// UI should refresh character lists, overview panels, and monitored character sets.
    /// </summary>
    /// <remarks>
    /// Uses the singleton pattern via <see cref="Instance"/> to avoid allocation.
    /// Distinct from individual <see cref="CharacterUpdatedEvent"/> which signals data refresh
    /// for an existing character.
    /// </remarks>
    public sealed class CharacterCollectionChangedEvent
    {
        /// <summary>Shared singleton instance.</summary>
        public static readonly CharacterCollectionChangedEvent Instance = new CharacterCollectionChangedEvent();
    }
}
