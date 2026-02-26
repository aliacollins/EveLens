// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Models;
using EveLens.Common.Notifications;

namespace EveLens.Common.Events
{
    // =========================================================================
    // Base class for character-scoped events (carries the rich Character model)
    // =========================================================================

    public abstract class CharacterEventBase
    {
        public Character Character { get; }

        protected CharacterEventBase(Character character)
        {
            Character = character;
        }
    }

    // =========================================================================
    // A. Parameterless singleton events (no payload)
    // =========================================================================

    public sealed class SettingsChangedEvent
    {
        public static readonly SettingsChangedEvent Instance = new();
        private SettingsChangedEvent() { }
    }

    public sealed class ESIKeyCollectionChangedEvent
    {
        public static readonly ESIKeyCollectionChangedEvent Instance = new();
        private ESIKeyCollectionChangedEvent() { }
    }

    public sealed class CharacterCollectionChangedEvent
    {
        public static readonly CharacterCollectionChangedEvent Instance = new();
        private CharacterCollectionChangedEvent() { }
    }

    public sealed class MonitoredCharacterCollectionChangedEvent
    {
        public static readonly MonitoredCharacterCollectionChangedEvent Instance = new();
        private MonitoredCharacterCollectionChangedEvent() { }
    }

    public sealed class SchedulerChangedEvent
    {
        public static readonly SchedulerChangedEvent Instance = new();
        private SchedulerChangedEvent() { }
    }

    public sealed class ESIKeyMonitoredChangedEvent
    {
        public static readonly ESIKeyMonitoredChangedEvent Instance = new();
        private ESIKeyMonitoredChangedEvent() { }
    }

    public sealed class AccountStatusUpdatedEvent
    {
        public static readonly AccountStatusUpdatedEvent Instance = new();
        private AccountStatusUpdatedEvent() { }
    }

    public sealed class ConquerableStationListUpdatedEvent
    {
        public static readonly ConquerableStationListUpdatedEvent Instance = new();
        private ConquerableStationListUpdatedEvent() { }
    }

    public sealed class EveFactionalWarfareStatsUpdatedEvent
    {
        public static readonly EveFactionalWarfareStatsUpdatedEvent Instance = new();
        private EveFactionalWarfareStatsUpdatedEvent() { }
    }

    public sealed class EveIDToNameUpdatedEvent
    {
        public static readonly EveIDToNameUpdatedEvent Instance = new();
        private EveIDToNameUpdatedEvent() { }
    }

    public sealed class RefTypesUpdatedEvent
    {
        public static readonly RefTypesUpdatedEvent Instance = new();
        private RefTypesUpdatedEvent() { }
    }

    public sealed class NotificationRefTypesUpdatedEvent
    {
        public static readonly NotificationRefTypesUpdatedEvent Instance = new();
        private NotificationRefTypesUpdatedEvent() { }
    }

    public sealed class EveFlagsUpdatedEvent
    {
        public static readonly EveFlagsUpdatedEvent Instance = new();
        private EveFlagsUpdatedEvent() { }
    }

    public sealed class ESIKeyInfoUpdatedEvent
    {
        public static readonly ESIKeyInfoUpdatedEvent Instance = new();
        private ESIKeyInfoUpdatedEvent() { }
    }

    public sealed class ItemPricesUpdatedEvent
    {
        public static readonly ItemPricesUpdatedEvent Instance = new();
        private ItemPricesUpdatedEvent() { }
    }

    public sealed class ServerStatusUpdatedEvent
    {
        public static readonly ServerStatusUpdatedEvent Instance = new();
        private ServerStatusUpdatedEvent() { }
    }

    // =========================================================================
    // B. Character-scoped events (carry Character model, no extra payload)
    // =========================================================================

    public sealed class CharacterUpdatedEvent : CharacterEventBase
    {
        public CharacterUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterInfoUpdatedEvent : CharacterEventBase
    {
        public CharacterInfoUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterSkillQueueUpdatedEvent : CharacterEventBase
    {
        public CharacterSkillQueueUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterStandingsUpdatedEvent : CharacterEventBase
    {
        public CharacterStandingsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterAssetsUpdatedEvent : CharacterEventBase
    {
        public CharacterAssetsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterFactionalWarfareStatsUpdatedEvent : CharacterEventBase
    {
        public CharacterFactionalWarfareStatsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class MarketOrdersUpdatedEvent : CharacterEventBase
    {
        public MarketOrdersUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class ContractsUpdatedEvent : CharacterEventBase
    {
        public ContractsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class IndustryJobsUpdatedEvent : CharacterEventBase
    {
        public IndustryJobsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterWalletJournalUpdatedEvent : CharacterEventBase
    {
        public CharacterWalletJournalUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterWalletTransactionsUpdatedEvent : CharacterEventBase
    {
        public CharacterWalletTransactionsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterResearchPointsUpdatedEvent : CharacterEventBase
    {
        public CharacterResearchPointsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterEVEMailMessagesUpdatedEvent : CharacterEventBase
    {
        public CharacterEVEMailMessagesUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterEVEMailingListsUpdatedEvent : CharacterEventBase
    {
        public CharacterEVEMailingListsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterEVEMailBodyDownloadedEvent : CharacterEventBase
    {
        public CharacterEVEMailBodyDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterEVENotificationsUpdatedEvent : CharacterEventBase
    {
        public CharacterEVENotificationsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterContactsUpdatedEvent : CharacterEventBase
    {
        public CharacterContactsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterMedalsUpdatedEvent : CharacterEventBase
    {
        public CharacterMedalsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationMedalsUpdatedEvent : CharacterEventBase
    {
        public CorporationMedalsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterUpcomingCalendarEventsUpdatedEvent : CharacterEventBase
    {
        public CharacterUpcomingCalendarEventsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterCalendarEventAttendeesDownloadedEvent : CharacterEventBase
    {
        public CharacterCalendarEventAttendeesDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterKillLogUpdatedEvent : CharacterEventBase
    {
        public CharacterKillLogUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterPlanetaryColoniesUpdatedEvent : CharacterEventBase
    {
        public CharacterPlanetaryColoniesUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterPlanetaryLayoutUpdatedEvent : CharacterEventBase
    {
        public CharacterPlanetaryLayoutUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterLoyaltyPointsUpdatedEvent : CharacterEventBase
    {
        public CharacterLoyaltyPointsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterPortraitUpdatedEvent : CharacterEventBase
    {
        public CharacterPortraitUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterPlanCollectionChangedEvent : CharacterEventBase
    {
        public CharacterPlanCollectionChangedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterImplantSetCollectionChangedEvent : CharacterEventBase
    {
        public CharacterImplantSetCollectionChangedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterContractBidsDownloadedEvent : CharacterEventBase
    {
        public CharacterContractBidsDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationContractBidsDownloadedEvent : CharacterEventBase
    {
        public CorporationContractBidsDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CharacterContractItemsDownloadedEvent : CharacterEventBase
    {
        public CharacterContractItemsDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationContractItemsDownloadedEvent : CharacterEventBase
    {
        public CorporationContractItemsDownloadedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationIndustryJobsUpdatedEvent : CharacterEventBase
    {
        public CorporationIndustryJobsUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationMarketOrdersUpdatedEvent : CharacterEventBase
    {
        public CorporationMarketOrdersUpdatedEvent(Character character) : base(character) { }
    }

    public sealed class CorporationContractsUpdatedEvent : CharacterEventBase
    {
        public CorporationContractsUpdatedEvent(Character character) : base(character) { }
    }

    // =========================================================================
    // C. Specialized events (character + additional payload)
    // =========================================================================

    public sealed class QueuedSkillsCompletedEvent : CharacterEventBase
    {
        public ReadOnlyCollection<QueuedSkill> CompletedSkills { get; }

        public QueuedSkillsCompletedEvent(Character character, IEnumerable<QueuedSkill> completedSkills)
            : base(character)
        {
            CompletedSkills = completedSkills.ToList().AsReadOnly();
        }
    }

    public sealed class CharacterMarketOrdersUpdatedEvent : CharacterEventBase
    {
        public IEnumerable<MarketOrder> EndedOrders { get; }

        public CharacterMarketOrdersUpdatedEvent(Character character, IEnumerable<MarketOrder> endedOrders)
            : base(character)
        {
            EndedOrders = endedOrders;
        }
    }

    public sealed class CorporationMarketOrdersEndedEvent : CharacterEventBase
    {
        public IEnumerable<MarketOrder> EndedOrders { get; }

        public CorporationMarketOrdersEndedEvent(Character character, IEnumerable<MarketOrder> endedOrders)
            : base(character)
        {
            EndedOrders = endedOrders;
        }
    }

    public sealed class CharacterContractsEndedEvent : CharacterEventBase
    {
        public IEnumerable<Contract> EndedContracts { get; }

        public CharacterContractsEndedEvent(Character character, IEnumerable<Contract> endedContracts)
            : base(character)
        {
            EndedContracts = endedContracts;
        }
    }

    public sealed class CorporationContractsEndedEvent : CharacterEventBase
    {
        public IEnumerable<Contract> EndedContracts { get; }

        public CorporationContractsEndedEvent(Character character, IEnumerable<Contract> endedContracts)
            : base(character)
        {
            EndedContracts = endedContracts;
        }
    }

    public sealed class CharacterIndustryJobsCompletedEvent : CharacterEventBase
    {
        public ReadOnlyCollection<IndustryJob> CompletedJobs { get; }

        public CharacterIndustryJobsCompletedEvent(Character character, IEnumerable<IndustryJob> completedJobs)
            : base(character)
        {
            CompletedJobs = completedJobs.ToList().AsReadOnly();
        }
    }

    public sealed class CorporationIndustryJobsCompletedEvent : CharacterEventBase
    {
        public ReadOnlyCollection<IndustryJob> CompletedJobs { get; }

        public CorporationIndustryJobsCompletedEvent(Character character, IEnumerable<IndustryJob> completedJobs)
            : base(character)
        {
            CompletedJobs = completedJobs.ToList().AsReadOnly();
        }
    }

    public sealed class CharacterPlanetaryPinsCompletedEvent : CharacterEventBase
    {
        public ReadOnlyCollection<PlanetaryPin> CompletedPins { get; }

        public CharacterPlanetaryPinsCompletedEvent(Character character, IEnumerable<PlanetaryPin> completedPins)
            : base(character)
        {
            CompletedPins = completedPins.ToList().AsReadOnly();
        }
    }

    // =========================================================================
    // D. Batch events
    // =========================================================================

    public sealed class CharactersBatchUpdatedEvent
    {
        public IReadOnlyList<Character> Characters { get; }
        public int Count => Characters.Count;

        public CharactersBatchUpdatedEvent(IReadOnlyList<Character> characters)
        {
            Characters = characters;
        }
    }

    public sealed class SkillQueuesBatchUpdatedEvent
    {
        public IReadOnlyList<Character> Characters { get; }
        public int Count => Characters.Count;

        public SkillQueuesBatchUpdatedEvent(IReadOnlyList<Character> characters)
        {
            Characters = characters;
        }
    }

    // =========================================================================
    // E. Wrapper events (wrap existing EventArgs types)
    // =========================================================================

    public sealed class CharacterLabelChangedEvent
    {
        public Character Character { get; }
        public IEnumerable<string> AllLabels { get; }

        public CharacterLabelChangedEvent(Character character, IEnumerable<string> allLabels)
        {
            Character = character;
            AllLabels = allLabels;
        }
    }

    public sealed class PlanChangedEvent
    {
        public Plan Plan { get; }

        public PlanChangedEvent(Plan plan)
        {
            Plan = plan;
        }
    }

    public sealed class PlanNameChangedEvent
    {
        public Plan Plan { get; }

        public PlanNameChangedEvent(Plan plan)
        {
            Plan = plan;
        }
    }

    public sealed class NotificationSentEvent
    {
        public NotificationEventArgs Args { get; }

        public NotificationSentEvent(NotificationEventArgs args)
        {
            Args = args;
        }
    }

    public sealed class NotificationInvalidatedEvent
    {
        public NotificationInvalidationEventArgs Args { get; }

        public NotificationInvalidatedEvent(NotificationInvalidationEventArgs args)
        {
            Args = args;
        }
    }

    public sealed class CharacterListUpdatedEvent
    {
        public ESIKey ESIKey { get; }

        public CharacterListUpdatedEvent(ESIKey esiKey)
        {
            ESIKey = esiKey;
        }
    }

    public sealed class UpdateAvailableEvent
    {
        public UpdateAvailableEventArgs Args { get; }

        public UpdateAvailableEvent(UpdateAvailableEventArgs args)
        {
            Args = args;
        }
    }

    public sealed class DataUpdateAvailableEvent
    {
        public DataUpdateAvailableEventArgs Args { get; }

        public DataUpdateAvailableEvent(DataUpdateAvailableEventArgs args)
        {
            Args = args;
        }
    }

    public sealed class LoadoutFeedUpdatedEvent
    {
        public LoadoutFeedEventArgs Args { get; }

        public LoadoutFeedUpdatedEvent(LoadoutFeedEventArgs args)
        {
            Args = args;
        }
    }

    public sealed class LoadoutUpdatedEvent
    {
        public LoadoutEventArgs Args { get; }

        public LoadoutUpdatedEvent(LoadoutEventArgs args)
        {
            Args = args;
        }
    }
}
