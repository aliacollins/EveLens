namespace EVEMon.Core.Events
{
    /// <summary>
    /// Base class for character-related events bridged from EveMonClient.
    /// Uses only primitive types to avoid dependency on EVEMon.Common.
    /// </summary>
    public abstract class CharacterEventBase
    {
        public long CharacterID { get; }
        public string CharacterName { get; }

        protected CharacterEventBase(long characterId, string characterName)
        {
            CharacterID = characterId;
            CharacterName = characterName ?? string.Empty;
        }
    }

    // ---- Character update events ----

    public sealed class CharacterUpdatedEvent : CharacterEventBase
    {
        public CharacterUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterInfoUpdatedEvent : CharacterEventBase
    {
        public CharacterInfoUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterSkillQueueUpdatedEvent : CharacterEventBase
    {
        public CharacterSkillQueueUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterAssetsUpdatedEvent : CharacterEventBase
    {
        public CharacterAssetsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterMarketOrdersUpdatedEvent : CharacterEventBase
    {
        public CharacterMarketOrdersUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterContractsUpdatedEvent : CharacterEventBase
    {
        public CharacterContractsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterIndustryJobsUpdatedEvent : CharacterEventBase
    {
        public CharacterIndustryJobsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterMailUpdatedEvent : CharacterEventBase
    {
        public CharacterMailUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterNotificationsUpdatedEvent : CharacterEventBase
    {
        public CharacterNotificationsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterPlanetaryUpdatedEvent : CharacterEventBase
    {
        public CharacterPlanetaryUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterStandingsUpdatedEvent : CharacterEventBase
    {
        public CharacterStandingsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterResearchUpdatedEvent : CharacterEventBase
    {
        public CharacterResearchUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterKillLogUpdatedEvent : CharacterEventBase
    {
        public CharacterKillLogUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterContactsUpdatedEvent : CharacterEventBase
    {
        public CharacterContactsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterMedalsUpdatedEvent : CharacterEventBase
    {
        public CharacterMedalsUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterCalendarUpdatedEvent : CharacterEventBase
    {
        public CharacterCalendarUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    public sealed class CharacterLoyaltyUpdatedEvent : CharacterEventBase
    {
        public CharacterLoyaltyUpdatedEvent(long characterId, string characterName)
            : base(characterId, characterName) { }
    }

    // ---- Non-character events ----

    public sealed class SettingsChangedEvent
    {
        public static readonly SettingsChangedEvent Instance = new SettingsChangedEvent();
    }

    public sealed class ESIKeyCollectionChangedEvent
    {
        public static readonly ESIKeyCollectionChangedEvent Instance = new ESIKeyCollectionChangedEvent();
    }

    public sealed class ServerStatusUpdatedEvent
    {
        public static readonly ServerStatusUpdatedEvent Instance = new ServerStatusUpdatedEvent();
    }

    public sealed class CharacterCollectionChangedEvent
    {
        public static readonly CharacterCollectionChangedEvent Instance = new CharacterCollectionChangedEvent();
    }
}
