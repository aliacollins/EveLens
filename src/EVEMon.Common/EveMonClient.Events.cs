using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using EVEMon.Common.CustomEventArgs;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Models;
using EVEMon.Common.Notifications;
using EVEMon.Common.Serialization.PatchXml;
using EVEMon.Common.Services;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common
{
    public static partial class EveMonClient
    {
        #region Timer Tick Counter

        /// <summary>
        /// Counter for tiered timer system.
        /// </summary>
        private static int s_tickCounter;

        /// <summary>
        /// Re-entrancy guard for timer tick processing.
        /// Prevents cascading ticks when processing takes longer than 1 second
        /// (common with 60+ characters where hundreds of event handlers fire).
        /// </summary>
        private static bool s_tickProcessing;

        #endregion

        #region Events firing

        /// <summary>
        /// Occurs every second. Use for skill countdowns and visible UI updates only.
        /// </summary>
        public static event EventHandler? SecondTick;

        /// <summary>
        /// Occurs every 5 seconds. Use for API cache checks and moderate-frequency updates.
        /// </summary>
        public static event EventHandler? FiveSecondTick;

        /// <summary>
        /// Occurs every 30 seconds. Use for background tasks like settings save checks.
        /// </summary>
        public static event EventHandler? ThirtySecondTick;

        /// <summary>
        /// Occurs when the settings changed.
        /// </summary>
        public static event EventHandler? SettingsChanged;

        /// <summary>
        /// Occurs when the ESI key info have been updated.
        /// </summary>
        public static event EventHandler? ESIKeyInfoUpdated;

        /// <summary>
        /// Occurs when the EveIDToName list has been updated.
        /// </summary>
        public static event EventHandler? EveIDToNameUpdated;

        /// <summary>
        /// Fires the timer tick event to notify the subscribers.
        /// Uses tiered system to reduce overhead for 100+ character scenarios.
        /// </summary>
        internal static void UpdateOnOneSecondTick()
        {
            if (Closed)
                return;

            // Re-entrancy guard: if the previous tick is still processing (common with 60+
            // characters where hundreds of handlers fire synchronously), skip this tick.
            // The DispatcherTimer will fire again in 1 second and we'll catch up then.
            if (s_tickProcessing)
                return;

            s_tickProcessing = true;
            try
            {
                // Increment tick counter
                s_tickCounter++;

                // Fire tiered events
                // SecondTick - every 1 second (skill countdowns, visible UI)
                SecondTick?.ThreadSafeInvoke(null, EventArgs.Empty);

                // FiveSecondTick - every 5 seconds (API checks, cache expiry)
                if (s_tickCounter % 5 == 0)
                {
                    FiveSecondTick?.ThreadSafeInvoke(null, EventArgs.Empty);
                }

                // ThirtySecondTick - every 30 seconds (background tasks)
                if (s_tickCounter % 30 == 0)
                {
                    ThirtySecondTick?.ThreadSafeInvoke(null, EventArgs.Empty);
                    s_tickCounter = 0; // Reset to prevent overflow
                }
            }
            finally
            {
                s_tickProcessing = false;
            }
        }

        /// <summary>
        /// Called when settings changed.
        /// </summary>
        internal static void OnSettingsChanged()
        {
            if (Closed)
                return;

            Trace();
            UpdateSettings();
            SettingsChanged?.ThreadSafeInvoke(null, EventArgs.Empty);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(SettingsChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.SettingsChangedEvent.Instance);
        }

        /// <summary>
        /// Called when the scheduler changed.
        /// </summary>
        internal static void OnSchedulerChanged()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CommonEvents.SchedulerChangedEvent.Instance);
        }

        /// <summary>
        /// Called when the ESI key collection changed.
        /// </summary>
        internal static void OnESIKeyCollectionChanged()
        {
            if (Closed)
                return;

            Trace();
            EveMonClient.Characters.UpdateAccountStatuses();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(ESIKeyCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Called when the monitored state of an ESI key changed.
        /// </summary>
        internal static void OnESIKeyMonitoredChanged()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyMonitoredChangedEvent.Instance);
        }

        /// <summary>
        /// Called when the monitored characters changed.
        /// </summary>
        internal static void OnMonitoredCharactersChanged()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CommonEvents.MonitoredCharacterCollectionChangedEvent.Instance);
        }

        /// <summary>
        /// Called when the character collection changed.
        /// </summary>
        internal static void OnCharacterCollectionChanged()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CharacterCollectionChangedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.CharacterCollectionChangedEvent.Instance);
        }


        /// <summary>
        /// Called when the conquerable station list has been updated.
        /// </summary>
        internal static void OnConquerableStationListUpdated()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.ConquerableStationListUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the EVE factional warfare statistics have been updated.
        /// </summary>
        internal static void OnEveFactionalWarfareStatsUpdated()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.EveFactionalWarfareStatsUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the EveIDToName list has been updated.
        /// </summary>
        internal static void OnEveIDToNameUpdated()
        {
            if (Closed)
                return;

            Trace();
            EveIDToNameUpdated?.ThreadSafeInvoke(null, EventArgs.Empty);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.EveIDToNameUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the RefTypes list has been updated.
        /// </summary>
        internal static void OnRefTypesUpdated()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.RefTypesUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the NotificationRefTypes list has been updated.
        /// </summary>
        internal static void OnNotificationRefTypesUpdated()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.NotificationRefTypesUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the EveFlags list has been updated.
        /// </summary>
        internal static void OnEveFlagsUpdated()
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.EveFlagsUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the ESI key info is updated.
        /// </summary>
        /// <param name="esiKey">The ESI key.</param>
        internal static void OnESIKeyInfoUpdated(ESIKey esiKey)
        {
            if (Closed)
                return;

            Trace(esiKey.ToString());
            ESIKeyInfoUpdated?.ThreadSafeInvoke(null, EventArgs.Empty);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CommonEvents.ESIKeyInfoUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when an account status has been updated.
        /// </summary>
        /// <param name="esiKey">The ESI key.</param>
        internal static void OnAccountStatusUpdated(ESIKey esiKey)
        {
            if (Closed)
                return;

            Trace(esiKey.ToString());
            Characters.UpdateAccountStatuses();

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(CommonEvents.AccountStatusUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when the character list updated.
        /// </summary>
        /// <param name="esiKey">The ESI key.</param>
        internal static void OnCharacterListUpdated(ESIKey esiKey)
        {
            if (Closed)
                return;

            Trace(esiKey.ToString());

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterListUpdatedEvent(esiKey));
        }

        /// <summary>
        /// Called when the character implant set collection changed.
        /// </summary>
        internal static void OnCharacterImplantSetCollectionChanged(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterImplantSetCollectionChangedEvent(character));
        }

        /// <summary>
        /// Called when the character sheet updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Queue for batched event (coalesces rapid updates)
            s_updateBatcher?.QueueCharacterUpdate(character);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CharacterUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character info updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterInfoUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CharacterInfoUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterInfoUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character label is changed via the UI.
        /// </summary>
        /// <param name="character">The character.</param>
        public static void OnCharacterLabelChanged(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterLabelChangedEvent(character, Characters.GetKnownLabels()));
        }

        /// <summary>
        /// Called when the character skill queue updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterSkillQueueUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);
            character.UpdateAccountStatus();

            // Queue for batched event (coalesces rapid updates)
            s_updateBatcher?.QueueSkillQueueUpdate(character);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CharacterSkillQueueUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterSkillQueueUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character queued skills completed.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="skillsCompleted">The skills completed.</param>
        internal static void OnCharacterQueuedSkillsCompleted(Character character, IEnumerable<QueuedSkill> skillsCompleted)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.QueuedSkillsCompletedEvent(character, skillsCompleted));
        }

        /// <summary>
        /// Called when the character standings updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterStandingsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterStandingsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterStandingsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character factinal warfare stats updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterFactionalWarfareStatsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterFactionalWarfareStatsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character assets updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterAssetsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnAssetsUpdated();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterAssetsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterAssetsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when both character and corporation issued market orders of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnMarketOrdersUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.MarketOrdersUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the personal market orders of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedOrders">The ended orders.</param>
        internal static void OnCharacterMarketOrdersUpdated(Character character, IEnumerable<MarketOrder> endedOrders)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnCharacterMarketOrdersUpdated(endedOrders);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterMarketOrdersUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterMarketOrdersUpdatedEvent(character, endedOrders));
        }

        /// <summary>
        /// Called when both character and corporation issued contracts of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnContractsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.ContractsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the personal contracts of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedContracts">The ended contracts.</param>
        internal static void OnCharacterContractsUpdated(Character character, IEnumerable<Contract> endedContracts)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnCharacterContractsUpdated(endedContracts);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterContractsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContractsEndedEvent(character, endedContracts));
        }

        /// <summary>
        /// Called when the bid list of a personal contract has been downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterContractBidsDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContractBidsDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the item list of a personal contract has been downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterContractItemsDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContractItemsDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the character wallet journal updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterWalletJournalUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterWalletJournalUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character wallet transcations updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterWalletTransactionsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterWalletTransactionsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when both character and corporation issued industry jobs for a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnIndustryJobsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.IndustryJobsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character industry jobs for a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterIndustryJobsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnCharacterIndustryJobsUpdated();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterIndustryJobsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.IndustryJobsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the corporation issued industry jobs for a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCorporationIndustryJobsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnCorporationIndustryJobsUpdated();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationIndustryJobsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character's industry jobs completed.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jobsCompleted">The jobs completed.</param>
        internal static void OnCharacterIndustryJobsCompleted(Character character, IEnumerable<IndustryJob> jobsCompleted)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnCharacterIndustryJobsCompleted(jobsCompleted);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterIndustryJobsCompletedEvent(character, jobsCompleted));
        }

        /// <summary>
        /// Called when the character's planetary pins completed.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="pinsCompleted">The pins completed.</param>
        internal static void OnCharacterPlanetaryPinsCompleted(Character character, IEnumerable<PlanetaryPin> pinsCompleted)
        {
            if (Closed)
                return;

            Trace(character.Name);
            (character as CCPCharacter)?.OnPlanetaryPinsCompleted(pinsCompleted);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanetaryPinsCompletedEvent(character, pinsCompleted));
        }

        /// <summary>
        /// Called when the character research points updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterResearchPointsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterResearchUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterResearchPointsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character EVE mail messages updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterEVEMailMessagesUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CharacterMailUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterEVEMailMessagesUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character EVE mailing list updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterEVEMailingListsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterEVEMailingListsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character EVE mail message body downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterEVEMailBodyDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterEVEMailBodyDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the character EVE notifications updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterEVENotificationsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CharacterNotificationsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterEVENotificationsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character contacts updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterContactsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterContactsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContactsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character medals updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterMedalsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterMedalsUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterMedalsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the corporation medals updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCorporationMedalsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationMedalsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character upcoming calendar events updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterUpcomingCalendarEventsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterCalendarUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpcomingCalendarEventsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character calendar event attendees downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterCalendarEventAttendeesDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterCalendarEventAttendeesDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the character kill log updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterKillLogUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterKillLogUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterKillLogUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character planetary colonies updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterPlanetaryColoniesUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterPlanetaryUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanetaryColoniesUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character planetary pins updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterPlanetaryLayoutUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanetaryLayoutUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character loyalty point balances updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterLoyaltyPointsUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CharacterLoyaltyUpdatedEvent(character.CharacterID, character.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterLoyaltyPointsUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character portrait updated.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterPortraitUpdated(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPortraitUpdatedEvent(character));
        }

        /// <summary>
        /// Called when the character plan collection changed.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCharacterPlanCollectionChanged(Character character)
        {
            if (Closed)
                return;

            Trace(character.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanCollectionChangedEvent(character));
        }

        /// <summary>
        /// Called when the corporation market orders of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedOrders">The ended orders.</param>
        internal static void OnCorporationMarketOrdersUpdated(Character character, IEnumerable<MarketOrder> endedOrders)
        {
            if (Closed)
                return;

            Trace(character.CorporationName);
            (character as CCPCharacter)?.OnCorporationMarketOrdersUpdated(endedOrders);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationMarketOrdersEndedEvent(character, endedOrders));
        }

        /// <summary>
        /// Called when the corporation contracts of a character updated.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="endedContracts">The ended contracts.</param>
        internal static void OnCorporationContractsUpdated(Character character, IEnumerable<Contract> endedContracts)
        {
            if (Closed)
                return;

            Trace(character.CorporationName);
            (character as CCPCharacter)?.OnCorporationContractsUpdated(endedContracts);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationContractsEndedEvent(character, endedContracts));
        }

        /// <summary>
        /// Called when the bid list of a corporation contract has been downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCorporationContractBidsDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.CorporationName);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationContractBidsDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the item list of a corporation contract has been downloaded.
        /// </summary>
        /// <param name="character">The character.</param>
        internal static void OnCorporationContractItemsDownloaded(Character character)
        {
            if (Closed)
                return;

            Trace(character.CorporationName);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationContractItemsDownloadedEvent(character));
        }

        /// <summary>
        /// Called when the character's corporation industry jobs completed.
        /// </summary>
        /// <param name="character">The character.</param>
        /// <param name="jobsCompleted">The jobs completed.</param>
        internal static void OnCorporationIndustryJobsCompleted(Character character, IEnumerable<IndustryJob> jobsCompleted)
        {
            if (Closed)
                return;

            Trace(character.CorporationName);
            (character as CCPCharacter)?.OnCorporationIndustryJobsCompleted(jobsCompleted);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CorporationIndustryJobsCompletedEvent(character, jobsCompleted));
        }

        /// <summary>
        /// Called when a plan changed.
        /// </summary>
        /// <param name="plan">The plan.</param>
        internal static void OnPlanChanged(Plan plan)
        {
            if (Closed)
                return;

            Trace(plan.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.PlanChangedEvent(plan));
        }

        /// <summary>
        /// Called when a plan name changed.
        /// </summary>
        /// <param name="plan">The plan.</param>
        internal static void OnPlanNameChanged(Plan plan)
        {
            if (Closed)
                return;

            Trace(plan.Name);

            // Bridge to EventAggregator for new code
            // Settings.Save() is handled by SettingsSaveSubscriber
            AppServices.EventAggregator?.Publish(new CommonEvents.PlanNameChangedEvent(plan));
        }

        /// <summary>
        /// Called when the server status updated.
        /// </summary>
        /// <param name="server">The server.</param>
        /// <param name="previousStatus">The previous status.</param>
        /// <param name="status">The status.</param>
        internal static void OnServerStatusUpdated(EveServer server, ServerStatus previousStatus, ServerStatus status)
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(ServerStatusUpdatedEvent.Instance);
            AppServices.EventAggregator?.Publish(CommonEvents.ServerStatusUpdatedEvent.Instance);
        }

        /// <summary>
        /// Called when a notification is sent.
        /// </summary>
        /// <param name="notification">The notification.</param>
        internal static void OnNotificationSent(NotificationEventArgs notification)
        {
            if (Closed)
                return;

            Trace(notification.ToString());

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.NotificationSentEvent(notification));
        }

        /// <summary>
        /// Called when a notification gets invalidated.
        /// </summary>
        /// <param name="args">The <see cref="EVEMon.Common.Notifications.NotificationInvalidationEventArgs"/> instance containing the event data.</param>
        internal static void OnNotificationInvalidated(NotificationInvalidationEventArgs args)
        {
            if (Closed)
                return;

            Trace();

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.NotificationInvalidatedEvent(args));
        }

        /// <summary>
        /// Called when an update is available.
        /// </summary>
        /// <param name="forumUrl">The forum URL.</param>
        /// <param name="installerUrl">The installer URL.</param>
        /// <param name="updateMessage">The update message.</param>
        /// <param name="currentVersion">The current version.</param>
        /// <param name="newestVersion">The newest version.</param>
        /// <param name="md5Sum">The MD5 sum.</param>
        /// <param name="canAutoInstall">if set to <c>true</c> [can auto install].</param>
        /// <param name="installArgs">The install args.</param>
        internal static void OnUpdateAvailable(Uri? forumUrl, Uri? installerUrl, string? updateMessage,
            Version currentVersion, Version newestVersion, string? md5Sum,
            bool canAutoInstall, string? installArgs)
        {
            Trace($"({currentVersion} -> {newestVersion}, {canAutoInstall}, {installArgs})");
            var updateArgs = new UpdateAvailableEventArgs(forumUrl, installerUrl, updateMessage, currentVersion,
                newestVersion, md5Sum, canAutoInstall, installArgs);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.UpdateAvailableEvent(updateArgs));
        }

        /// <summary>
        /// Called when data update is available.
        /// </summary>
        /// <param name="changedFiles">The changed files.</param>
        internal static void OnDataUpdateAvailable(Collection<SerializableDatafile> changedFiles)
        {
            Trace($"(ChangedFiles = {changedFiles.Count})");
            var dataUpdateArgs = new DataUpdateAvailableEventArgs(changedFiles);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.DataUpdateAvailableEvent(dataUpdateArgs));
        }

        /// <summary>
        /// Called when we downloaded a loadouts feed from the provider.
        /// </summary>
        /// <param name="loadoutFeed">The loadout feed.</param>
        /// <param name="errorMessage">The error message.</param>
        internal static void OnLoadoutsFeedDownloaded(object loadoutFeed, string errorMessage)
        {
            var feedArgs = new LoadoutFeedEventArgs(loadoutFeed, errorMessage);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.LoadoutFeedUpdatedEvent(feedArgs));
        }

        /// <summary>
        /// Called when we downloaded a loadout from the provider.
        /// </summary>
        /// <param name="loadout">The loadout.</param>
        /// <param name="errorMessage">The error message.</param>
        internal static void OnLoadoutDownloaded(object loadout, string errorMessage)
        {
            var loadoutArgs = new LoadoutEventArgs(loadout, errorMessage);

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.LoadoutUpdatedEvent(loadoutArgs));
        }

        /// <summary>
        /// Called when prices downloaded.
        /// </summary>
        /// <param name="pricesFeed">The prices feed.</param>
        /// <param name="errormessage">The errormessage.</param>
        internal static void OnPricesDownloaded(object pricesFeed, string errormessage)
        {
            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(CommonEvents.ItemPricesUpdatedEvent.Instance);
        }

        #endregion


        #region Event Coalescing

        /// <summary>
        /// Called when the update batcher has collected character updates ready to fire.
        /// </summary>
        private static void OnBatchedCharacterUpdatesReady(object? sender, CharacterBatchEventArgs e)
        {
            if (Closed)
                return;

            Trace($"Batched update for {e.Count} characters");

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.CharactersBatchUpdatedEvent(e.Characters));
        }

        /// <summary>
        /// Called when the update batcher has collected skill queue updates ready to fire.
        /// </summary>
        private static void OnBatchedSkillQueueUpdatesReady(object? sender, CharacterBatchEventArgs e)
        {
            if (Closed)
                return;

            Trace($"Batched skill queue update for {e.Count} characters");

            // Bridge to EventAggregator for new code
            AppServices.EventAggregator?.Publish(new CommonEvents.SkillQueuesBatchUpdatedEvent(e.Characters));
        }

        #endregion

    }
}
