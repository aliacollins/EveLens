using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Extensions;
using EVEMon.Common.Helpers;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Net;
using EVEMon.Common.QueryMonitor;
using EVEMon.Common.Serialization.Esi;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Service;
using EVEMon.Common.Threading;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Production-grade character query orchestrator that replaces CharacterDataQuerying.
    /// Creates and drives all 27 ESI query monitors for a character, handles all callbacks,
    /// and implements both ICharacterQueryManager (for scheduling tests) and
    /// ICharacterDataQuerying (for CCPCharacter integration).
    /// </summary>
    internal sealed class CharacterQueryOrchestrator : ICharacterQueryManager, ICharacterDataQuerying
    {
        #region Constants

        // Well-known data type constants matching ESIAPICharacterMethods enum values.
        // Using int instead of the enum to avoid dependency from EVEMon.Core on EVEMon.Common.
        internal const int DataType_CharacterSheet = 0;
        internal const int DataType_Skills = 1;
        internal const int DataType_SkillQueue = 2;
        internal const int DataType_Implants = 3;
        internal const int DataType_Attributes = 4;

        /// <summary>
        /// The set of data types that are auto-created on construction (basic features).
        /// </summary>
        private static readonly int[] BasicFeatureDataTypes = new[]
        {
            DataType_CharacterSheet,
            DataType_Skills,
            DataType_SkillQueue
        };

        /// <summary>
        /// Prerequisite dictionary: a data type key cannot be processed until all
        /// its prerequisite data types have completed at least once.
        /// Preserves the Implants -> Attributes chain from CharacterDataQuerying.
        /// </summary>
        private static readonly Dictionary<int, int[]> Prerequisites = new Dictionary<int, int[]>
        {
            { DataType_Attributes, new[] { DataType_Implants } }
        };

        #endregion


        #region Production Mode Fields

        private readonly CCPCharacter? m_ccpCharacter;
        private readonly bool _isProductionMode;

        private CharacterQueryMonitor<EsiAPISkillQueue>? m_charSkillQueueMonitor;
        private CharacterQueryMonitor<EsiAPISkills>? m_charSkillsMonitor;
        private CharacterQueryMonitor<EsiAPIMarketOrders>? m_charMarketOrdersMonitor;
        private QueryMonitor<EsiAPIContracts>? m_charContractsMonitor;
        private CharacterQueryMonitor<EsiAPIIndustryJobs>? m_charIndustryJobsMonitor;
        private List<IQueryMonitorEx>? m_characterQueryMonitors;
        private List<IQueryMonitorEx>? m_basicFeaturesMonitors;
        private bool m_characterSheetUpdating;

        // Responses from the attribute results since we handle it manually
        private ResponseParams? m_attrResponse;
        // Result from the character skill queue to handle a pathological case where skill
        // queues were not-modified but need to be re-imported due to a skills list change
        private EsiAPISkillQueue? m_lastQueue;
        // Responses from the market order history results since we handle it manually
        private ResponseParams? m_orderHistoryResponse;

        // Adapter for SmartQueryScheduler registration
        private ScheduledQueryableAdapter? m_schedulerAdapter;

        // Staggered startup fields - prevents all characters from querying at once
        private static int s_characterStartupIndex = 0;
        private static readonly Random s_random = new Random();
        private DateTime m_startupDelayUntil;
        private bool m_startupDelayCompleted = false;

        private const int StartupDelayPerCharacterMs = 75;
        private const int StartupRandomJitterMs = 250;

        #endregion


        #region Test Mode Fields

        private readonly IQueryScheduler? _scheduler;
        private readonly IEsiClient? _esiClient;
        private readonly IEventAggregator? _eventAggregator;
        private readonly long _characterId;
        private readonly string _characterName;
        private readonly object _lock = new object();
        private readonly Dictionary<int, MonitorState>? _monitors;

        private bool _disposed;
        private bool _testCharacterSheetUpdating;
        private int _consecutiveNotModifiedCount;
        private bool _startupComplete;

        #endregion


        #region Production Constructor

        /// <summary>
        /// Production constructor — creates real ESI monitors and callbacks.
        /// Called from CCPCharacter when ESI key info is updated.
        /// </summary>
        internal CharacterQueryOrchestrator(CCPCharacter ccpCharacter)
        {
            m_ccpCharacter = ccpCharacter ?? throw new ArgumentNullException(nameof(ccpCharacter));
            _isProductionMode = true;
            _characterId = ccpCharacter.CharacterID;
            _characterName = ccpCharacter.Name ?? string.Empty;

            InitializeStartupDelay(ccpCharacter);

            m_characterQueryMonitors = CreateMonitors(ccpCharacter);
            m_characterQueryMonitors.ForEach(monitor => ccpCharacter.QueryMonitors.Add(monitor));

            m_basicFeaturesMonitors = InitializeBasicFeaturesMonitors(ccpCharacter);

            if (EveMonClient.SmartQueryScheduler != null)
            {
                m_schedulerAdapter = new ScheduledQueryableAdapter(
                    ccpCharacter.CharacterID, () => ProcessTick());
                EveMonClient.SmartQueryScheduler.Register(m_schedulerAdapter);
            }
        }

        #endregion


        #region Test Constructor

        /// <summary>
        /// Test/scheduling constructor — uses abstract MonitorState for scheduling tests.
        /// Preserved for backward compatibility with existing tests.
        /// </summary>
        public CharacterQueryOrchestrator(
            IQueryScheduler scheduler,
            IEsiClient esiClient,
            IEventAggregator eventAggregator,
            long characterId,
            string characterName)
        {
            _scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
            _esiClient = esiClient ?? throw new ArgumentNullException(nameof(esiClient));
            _eventAggregator = eventAggregator ?? throw new ArgumentNullException(nameof(eventAggregator));
            _characterId = characterId;
            _characterName = characterName ?? string.Empty;
            _isProductionMode = false;
            _monitors = new Dictionary<int, MonitorState>();

            // Auto-create the basic feature monitors
            foreach (int dataType in BasicFeatureDataTypes)
            {
                _monitors[dataType] = new MonitorState
                {
                    DataType = dataType,
                    IsActive = true,
                    NextQueryTime = DateTime.MinValue // Ready to query immediately
                };
            }

            _scheduler.Register(this);
        }

        #endregion


        #region ICharacterQueryManager Properties

        /// <inheritdoc />
        public long CharacterID => _characterId;

        /// <inheritdoc />
        public int ActiveMonitorCount
        {
            get
            {
                if (_isProductionMode)
                    return m_characterQueryMonitors?.Count ?? 0;

                lock (_lock)
                {
                    return _monitors!.Count(kv => kv.Value.IsActive);
                }
            }
        }

        /// <inheritdoc />
        public bool IsCharacterSheetUpdating => _isProductionMode
            ? m_characterSheetUpdating
            : _testCharacterSheetUpdating;

        /// <inheritdoc />
        public bool IsStartupComplete => _isProductionMode
            ? m_startupDelayCompleted
            : _startupComplete;

        /// <inheritdoc />
        public int ConsecutiveNotModifiedCount => _consecutiveNotModifiedCount;

        #endregion


        #region ICharacterDataQuerying Properties

        /// <summary>
        /// Gets the character sheet monitor (production mode only).
        /// </summary>
        internal CharacterQueryMonitor<EsiAPICharacterSheet>? CharacterSheetMonitor { get; private set; }

        /// <inheritdoc />
        public bool HasCharacterSheetError => CharacterSheetMonitor?.HasError ?? false;

        /// <inheritdoc />
        public bool CharacterMarketOrdersQueried => m_charMarketOrdersMonitor != null
            ? !m_charMarketOrdersMonitor.IsUpdating
            : true;

        /// <inheritdoc />
        public bool CharacterContractsQueried => m_charContractsMonitor != null
            ? !m_charContractsMonitor.IsUpdating
            : true;

        /// <inheritdoc />
        public bool CharacterIndustryJobsQueried => m_charIndustryJobsMonitor != null
            ? !m_charIndustryJobsMonitor.IsUpdating
            : true;

        #endregion


        #region ICharacterQueryManager Methods

        /// <inheritdoc />
        public void RequestDataType(int dataType)
        {
            if (_disposed)
                return;

            if (!_isProductionMode)
            {
                lock (_lock)
                {
                    if (_monitors!.ContainsKey(dataType))
                        return;

                    _monitors[dataType] = new MonitorState
                    {
                        DataType = dataType,
                        IsActive = true,
                        NextQueryTime = DateTime.MinValue
                    };
                }
            }
        }

        /// <inheritdoc />
        public bool IsQueryComplete(int dataType)
        {
            if (!_isProductionMode)
            {
                lock (_lock)
                {
                    if (_monitors!.TryGetValue(dataType, out var state))
                        return state.HasCompletedOnce;
                    return false;
                }
            }
            return false;
        }

        #endregion


        #region Production Mode — Monitor Creation

        /// <summary>
        /// Calculates the staggered startup delay to prevent all characters querying at once.
        /// </summary>
        private void InitializeStartupDelay(CCPCharacter ccpCharacter)
        {
            if (ccpCharacter.ForceUpdateBasicFeatures)
            {
                // New character added manually - fetch immediately
                m_startupDelayUntil = DateTime.UtcNow;
                m_startupDelayCompleted = true;
                EveMonClient.Trace($"CharacterQueryOrchestrator - {ccpCharacter.Name} is new, skipping startup delay");
            }
            else
            {
                int characterIndex = System.Threading.Interlocked.Increment(ref s_characterStartupIndex);
                int baseDelayMs = characterIndex * StartupDelayPerCharacterMs;
                int jitterMs = s_random.Next(StartupRandomJitterMs);
                m_startupDelayUntil = DateTime.UtcNow.AddMilliseconds(baseDelayMs + jitterMs);
                EveMonClient.Trace($"CharacterQueryOrchestrator - {ccpCharacter.Name} startup delayed until {m_startupDelayUntil:HH:mm:ss.fff} (index {characterIndex})");
            }
        }

        /// <summary>
        /// Creates all ESI query monitors for the character.
        /// Monitors are ordered as they appear in the throbber menu.
        /// </summary>
        private List<IQueryMonitorEx> CreateMonitors(CCPCharacter ccpCharacter)
        {
            var notifiers = EveMonClient.Notifications;
            var monitors = new List<IQueryMonitorEx>();

            // Character sheet
            CharacterSheetMonitor = new CharacterQueryMonitor<EsiAPICharacterSheet>(
                ccpCharacter, ESIAPICharacterMethods.CharacterSheet, OnCharacterSheetUpdated,
                notifiers.NotifyCharacterSheetError, suppressSelfTicking: true);
            monitors.Add(CharacterSheetMonitor);
            // Location
            monitors.Add(new CharacterQueryMonitor<EsiAPILocation>(
                ccpCharacter, ESIAPICharacterMethods.Location, OnCharacterLocationUpdated,
                notifiers.NotifyCharacterLocationError, suppressSelfTicking: true));
            // Clones
            monitors.Add(new CharacterQueryMonitor<EsiAPIClones>(
                ccpCharacter, ESIAPICharacterMethods.Clones, OnCharacterClonesUpdated,
                notifiers.NotifyCharacterClonesError, suppressSelfTicking: true));
            // Implants
            monitors.Add(new CharacterQueryMonitor<List<int>>(
                ccpCharacter, ESIAPICharacterMethods.Implants, OnCharacterImplantsUpdated,
                OnCharacterImplantsFailed, true, suppressSelfTicking: true));
            // Ship
            monitors.Add(new CharacterQueryMonitor<EsiAPIShip>(
                ccpCharacter, ESIAPICharacterMethods.Ship, OnCharacterShipUpdated,
                notifiers.NotifyCharacterShipError, suppressSelfTicking: true));
            // Skills
            m_charSkillsMonitor = new CharacterQueryMonitor<EsiAPISkills>(
                ccpCharacter, ESIAPICharacterMethods.Skills, OnCharacterSkillsUpdated,
                notifiers.NotifyCharacterSkillsError, suppressSelfTicking: true);
            monitors.Add(m_charSkillsMonitor);
            // Skill queue
            m_charSkillQueueMonitor = new CharacterQueryMonitor<EsiAPISkillQueue>(
                ccpCharacter, ESIAPICharacterMethods.SkillQueue, OnSkillQueueUpdated,
                notifiers.NotifySkillQueueError, suppressSelfTicking: true);
            monitors.Add(m_charSkillQueueMonitor);
            // Employment history
            monitors.Add(new CharacterQueryMonitor<EsiAPIEmploymentHistory>(
                ccpCharacter, ESIAPICharacterMethods.EmploymentHistory,
                OnCharacterEmploymentUpdated, notifiers.NotifyCharacterEmploymentError,
                suppressSelfTicking: true));
            // Standings
            monitors.Add(new PagedQueryMonitor<EsiAPIStandings,
                EsiStandingsListItem>(new CharacterQueryMonitor<EsiAPIStandings>(
                ccpCharacter, ESIAPICharacterMethods.Standings, OnStandingsUpdated,
                notifiers.NotifyCharacterStandingsError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Contacts
            monitors.Add(new PagedQueryMonitor<EsiAPIContactsList,
                EsiContactListItem>(new CharacterQueryMonitor<EsiAPIContactsList>(ccpCharacter,
                ESIAPICharacterMethods.ContactList, OnContactsUpdated,
                notifiers.NotifyCharacterContactsError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Factional warfare
            monitors.Add(new CharacterQueryMonitor<EsiAPIFactionalWarfareStats>(
                ccpCharacter, ESIAPICharacterMethods.FactionalWarfareStats,
                OnFactionalWarfareStatsUpdated, notifiers.
                NotifyCharacterFactionalWarfareStatsError, suppressSelfTicking: true) { QueryOnStartup = true });
            // Medals
            monitors.Add(new PagedQueryMonitor<EsiAPIMedals,
                EsiMedalsListItem>(new CharacterQueryMonitor<EsiAPIMedals>(ccpCharacter,
                ESIAPICharacterMethods.Medals, OnMedalsUpdated,
                notifiers.NotifyCharacterMedalsError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Kill log
            monitors.Add(new PagedQueryMonitor<EsiAPIKillLog,
                EsiKillLogListItem>(new CharacterQueryMonitor<EsiAPIKillLog>(ccpCharacter,
                ESIAPICharacterMethods.KillLog, OnKillLogUpdated,
                notifiers.NotifyCharacterKillLogError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Assets
            monitors.Add(new PagedQueryMonitor<EsiAPIAssetList,
                EsiAssetListItem>(new CharacterQueryMonitor<EsiAPIAssetList>(ccpCharacter,
                ESIAPICharacterMethods.AssetList, OnAssetsUpdated,
                notifiers.NotifyCharacterAssetsError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Market orders
            m_charMarketOrdersMonitor = new CharacterQueryMonitor<EsiAPIMarketOrders>(
                ccpCharacter, ESIAPICharacterMethods.MarketOrders, OnMarketOrdersUpdated,
                notifiers.NotifyCharacterMarketOrdersError, suppressSelfTicking: true) { QueryOnStartup = true };
            monitors.Add(m_charMarketOrdersMonitor);
            // Contracts
            m_charContractsMonitor = new PagedQueryMonitor<EsiAPIContracts,
                EsiContractListItem>(new CharacterQueryMonitor<EsiAPIContracts>(ccpCharacter,
                ESIAPICharacterMethods.Contracts, OnContractsUpdated,
                notifiers.NotifyCharacterContractsError, suppressSelfTicking: true) { QueryOnStartup = true });
            monitors.Add(m_charContractsMonitor);
            // Wallet journal
            monitors.Add(new PagedQueryMonitor<EsiAPIWalletJournal,
                EsiWalletJournalListItem>(new CharacterQueryMonitor<EsiAPIWalletJournal>(
                ccpCharacter, ESIAPICharacterMethods.WalletJournal, OnWalletJournalUpdated,
                notifiers.NotifyCharacterWalletJournalError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Wallet balance
            monitors.Add(new CharacterQueryMonitor<string>(
                ccpCharacter, ESIAPICharacterMethods.AccountBalance, OnWalletBalanceUpdated,
                notifiers.NotifyCharacterBalanceError, suppressSelfTicking: true));
            // Wallet transactions
            monitors.Add(new PagedQueryMonitor<EsiAPIWalletTransactions,
                EsiWalletTransactionsListItem>(new CharacterQueryMonitor<
                EsiAPIWalletTransactions>(ccpCharacter, ESIAPICharacterMethods.
                WalletTransactions, OnWalletTransactionsUpdated, notifiers.
                NotifyCharacterWalletTransactionsError, suppressSelfTicking: true) { QueryOnStartup = true }));
            // Industry
            m_charIndustryJobsMonitor = new CharacterQueryMonitor<EsiAPIIndustryJobs>(
                ccpCharacter, ESIAPICharacterMethods.IndustryJobs, OnIndustryJobsUpdated,
                notifiers.NotifyCharacterIndustryJobsError, suppressSelfTicking: true) { QueryOnStartup = true };
            monitors.Add(m_charIndustryJobsMonitor);
            // Research points
            monitors.Add(new CharacterQueryMonitor<EsiAPIResearchPoints>(
                ccpCharacter, ESIAPICharacterMethods.ResearchPoints, OnResearchPointsUpdated,
                notifiers.NotifyCharacterResearchPointsError, suppressSelfTicking: true) { QueryOnStartup = true });
            // Mail
            monitors.Add(new CharacterQueryMonitor<EsiAPIMailMessages>(
                ccpCharacter, ESIAPICharacterMethods.MailMessages, OnEVEMailMessagesUpdated,
                notifiers.NotifyEVEMailMessagesError, suppressSelfTicking: true) { QueryOnStartup = true });
            // Mailing lists
            monitors.Add(new CharacterQueryMonitor<EsiAPIMailingLists>(
                ccpCharacter, ESIAPICharacterMethods.MailingLists, OnEveMailingListsUpdated,
                    notifiers.NotifyMailingListsError, suppressSelfTicking: true));
            // Notifications
            monitors.Add(new CharacterQueryMonitor<EsiAPINotifications>(
                ccpCharacter, ESIAPICharacterMethods.Notifications, OnEVENotificationsUpdated,
                notifiers.NotifyEVENotificationsError, suppressSelfTicking: true) { QueryOnStartup = true });
            // Calendar
            monitors.Add(new CharacterQueryMonitor<EsiAPICalendarEvents>(
                ccpCharacter, ESIAPICharacterMethods.UpcomingCalendarEvents,
                OnUpcomingCalendarEventsUpdated, notifiers.
                NotifyCharacterUpcomingCalendarEventsError, suppressSelfTicking: true) { QueryOnStartup = true });
            // PI
            monitors.Add(new CharacterQueryMonitor<EsiAPIPlanetaryColoniesList>(
                ccpCharacter, ESIAPICharacterMethods.PlanetaryColonies,
                OnPlanetaryColoniesUpdated, notifiers.
                NotifyCharacterPlanetaryColoniesError, suppressSelfTicking: true) { QueryOnStartup = true });
            // LP
            monitors.Add(new CharacterQueryMonitor<EsiAPILoyality>(
                ccpCharacter, ESIAPICharacterMethods.LoyaltyPoints,
                OnLoyaltyPointsUpdated, notifiers.
                NotifyCharacterLoyaltyPointsError, suppressSelfTicking: true) { QueryOnStartup = true });

            return monitors;
        }

        /// <summary>
        /// Filters monitor list to basic features and optionally force-updates them for new characters.
        /// </summary>
        private List<IQueryMonitorEx> InitializeBasicFeaturesMonitors(CCPCharacter ccpCharacter)
        {
            var basicMonitors = new List<IQueryMonitorEx>(m_characterQueryMonitors!.Count);
            long basicFeatures = (long)CCPAPIMethodsEnum.BasicCharacterFeatures;
            foreach (var monitor in m_characterQueryMonitors)
            {
                long method = (long)(ESIAPICharacterMethods)monitor.Method;
                if (method == (method & basicFeatures))
                {
                    basicMonitors.Add(monitor);
                    // If force update is selected, update basic features only
                    if (ccpCharacter.ForceUpdateBasicFeatures)
                    {
                        monitor.Enabled = true;  // Enable immediately for new characters
                        monitor.ForceUpdate(true);
                    }
                }
            }
            return basicMonitors;
        }

        #endregion


        #region ProcessTick

        /// <summary>
        /// Processes a single tick. In production mode, drives all real monitors.
        /// In test mode, drives abstract MonitorState scheduling.
        /// </summary>
        public void ProcessTick()
        {
            if (_disposed)
                return;

            if (_isProductionMode)
                ProcessTickProduction();
            else
                ProcessTickTest();
        }

        /// <summary>
        /// Production mode tick: drives real monitors via UpdateTick().
        /// </summary>
        private void ProcessTickProduction()
        {
            // Check if startup delay has passed (staggered startup to prevent API burst)
            if (!m_startupDelayCompleted)
            {
                if (DateTime.UtcNow < m_startupDelayUntil)
                {
                    // Still in startup delay - keep monitors disabled
                    return;
                }

                // Startup delay completed - allow monitors to run
                m_startupDelayCompleted = true;
                EveMonClient.Trace($"CharacterQueryOrchestrator - {m_ccpCharacter!.Name} startup delay completed, monitors enabled");
            }

            // If character is monitored enable the basic feature monitoring
            foreach (var monitor in m_basicFeaturesMonitors!)
                monitor.Enabled = m_ccpCharacter!.Monitored;
            if (m_characterSheetUpdating)
                FinishCharacterSheetUpdated();

            // Drive all monitors' update checks from this single tick handler
            // instead of each monitor subscribing to FiveSecondTick individually.
            foreach (var monitor in m_characterQueryMonitors!)
                monitor.UpdateTick();
        }

        /// <summary>
        /// Test mode tick: drives abstract MonitorState scheduling.
        /// Preserved for backward compatibility with existing tests.
        /// </summary>
        private void ProcessTickTest()
        {
            var now = DateTime.UtcNow;
            bool anyProcessed = false;
            bool allNotModified = true;

            // Flag that a character sheet update cycle is starting (only during initial startup)
            if (!_startupComplete && !_testCharacterSheetUpdating)
                _testCharacterSheetUpdating = true;

            KeyValuePair<int, MonitorState>[] snapshot;
            lock (_lock)
            {
                snapshot = _monitors!.Where(kv => kv.Value.IsActive).ToArray();
            }

            // Track which monitors complete during THIS tick so that same-tick
            // completions are not treated as satisfied prerequisites.
            var completedThisTick = new HashSet<int>();

            foreach (var kv in snapshot)
            {
                var state = kv.Value;
                int dataType = kv.Key;

                // Skip if not yet due
                if (now < state.NextQueryTime)
                    continue;

                // Check prerequisites
                if (Prerequisites.TryGetValue(dataType, out int[] prereqs))
                {
                    bool prerequisitesMet;
                    lock (_lock)
                    {
                        prerequisitesMet = prereqs.All(p =>
                            _monitors!.TryGetValue(p, out var prereqState) &&
                            prereqState.HasCompletedOnce &&
                            !completedThisTick.Contains(p));
                    }

                    if (!prerequisitesMet)
                        continue;
                }

                // For Skills (1), check that SkillQueue (2) has a cached result
                if (dataType == DataType_Skills)
                {
                    MonitorState queueState;
                    lock (_lock)
                    {
                        _monitors!.TryGetValue(DataType_SkillQueue, out queueState);
                    }

                    if (queueState == null || queueState.CachedResult == null)
                        continue;
                }

                // Execute the query
                state.HasCompletedOnce = true;
                state.NextQueryTime = now.AddMinutes(5);
                anyProcessed = true;
                completedThisTick.Add(dataType);

                // Cache the SkillQueue result for Skills import on subsequent ticks
                if (dataType == DataType_SkillQueue)
                    state.CachedResult = new object();

                if (state.ConsecutiveNotModified > 0)
                {
                    // Track not-modified at the orchestrator level
                }
                else
                {
                    allNotModified = false;
                }
            }

            if (anyProcessed)
            {
                _consecutiveNotModifiedCount = allNotModified
                    ? _consecutiveNotModifiedCount + 1
                    : 0;
            }

            // Check if all basic monitors have completed (CharacterSheet completion tracking)
            CheckCharacterSheetCompletionTest();
        }

        #endregion


        #region Production Mode — CharacterSheet Completion

        /// <summary>
        /// Check if any character sheet related query monitors are still running, and trigger
        /// events if they are all completed.
        /// </summary>
        private void FinishCharacterSheetUpdated()
        {
            // Check if all CharacterSheet related query monitors have completed
            if (!m_characterQueryMonitors!.Any(monitor => (ESIAPICharacterMethods.
                CharacterSheet.Equals(monitor.Method) || monitor.Method.HasParent(
                ESIAPICharacterMethods.CharacterSheet)) && monitor.Status == QueryStatus.
                Updating))
            {
                m_characterSheetUpdating = false;
                var target = m_ccpCharacter;
                // Character may have been deleted since we queried
                if (target != null)
                {
                    // Finally all done!
                    EveMonClient.Notifications.InvalidateCharacterAPIError(target);
                    EveMonClient.OnCharacterUpdated(target);
                    EveMonClient.OnCharacterInfoUpdated(target);
                    EveMonClient.OnCharacterImplantSetCollectionChanged(target);
                    // Save character information locally
                    var doc = Util.SerializeToXmlDocument(target.Export());
                    LocalXmlCache.SaveAsync(target.Name, doc).ConfigureAwait(false);
                }
            }
        }

        #endregion


        #region Test Mode — CharacterSheet Completion

        /// <summary>
        /// Checks if all basic feature monitors have completed at least once (test mode).
        /// </summary>
        private void CheckCharacterSheetCompletionTest()
        {
            if (!_testCharacterSheetUpdating)
                return;

            bool allBasicComplete;
            lock (_lock)
            {
                allBasicComplete = BasicFeatureDataTypes.All(dt =>
                    _monitors!.TryGetValue(dt, out var state) && state.HasCompletedOnce);
            }

            if (allBasicComplete)
            {
                _testCharacterSheetUpdating = false;
                _startupComplete = true;

                _eventAggregator!.Publish(new CharacterUpdatedEvent(_characterId, _characterName));
            }
        }

        #endregion


        #region Production Mode — Callbacks: Core

        /// <summary>
        /// Processes the queried character's character sheet information.
        /// </summary>
        private void OnCharacterSheetUpdated(EsiAPICharacterSheet result)
        {
            // Flag that we are waiting for character sheet operations to finish
            if (!m_characterSheetUpdating)
                m_characterSheetUpdating = true;

            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                EveMonClient.Trace($"CharacterSheet updated - {target.Name} no longer cached");
                target.Import(result);
            }
        }

        /// <summary>
        /// Processes the queried character's location.
        /// </summary>
        private void OnCharacterLocationUpdated(EsiAPILocation result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                target.Import(result);
        }

        /// <summary>
        /// Processes the queried character's clones.
        /// </summary>
        private void OnCharacterClonesUpdated(EsiAPIClones result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                target.Import(result);
        }

        /// <summary>
        /// Processes the queried character's ship.
        /// </summary>
        private void OnCharacterShipUpdated(EsiAPIShip result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                target.Import(result);
        }

        /// <summary>
        /// Processes the queried character's employment history.
        /// </summary>
        private void OnCharacterEmploymentUpdated(EsiAPIEmploymentHistory result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                target.Import(result);
        }

        #endregion


        #region Production Mode — Callbacks: Skills + SkillQueue

        /// <summary>
        /// Processes the queried character's skills.
        /// </summary>
        private void OnCharacterSkillsUpdated(EsiAPISkills result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null && m_lastQueue != null)
                target.Import(result, m_lastQueue);
        }

        /// <summary>
        /// Processes the queried character's skill queue information.
        /// </summary>
        private void OnSkillQueueUpdated(EsiAPISkillQueue result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                m_lastQueue = result;
                target.SkillQueue.Import(result.CreateSkillQueue());
                // Check if the character has less than the threshold queue length
                if (target.IsTraining && target.SkillQueue.LessThanWarningThreshold)
                    EveMonClient.Notifications.NotifySkillQueueThreshold(target,
                        Settings.UI.MainWindow.SkillQueueWarningThresholdDays);
                else
                    EveMonClient.Notifications.InvalidateSkillQueueThreshold(target);
            }
            else
                m_lastQueue = null;
        }

        #endregion


        #region Production Mode — Callbacks: Implants → Attributes Chain

        /// <summary>
        /// Processes the queried character's implants.
        /// </summary>
        private void OnCharacterImplantsUpdated(List<int> result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                if (result != null)
                    target.Import(result);
                QueryAttributesAsync(target);
            }
        }

        /// <summary>
        /// Notifies the user if character implants could not be queried, but continues to
        /// query the attributes even if this occurs.
        /// </summary>
        private void OnCharacterImplantsFailed(CCPCharacter character, EsiResult<List<int>>
            result)
        {
            EveMonClient.Notifications.NotifyCharacterImplantsError(character, result);
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                QueryAttributesAsync(target);
        }

        /// <summary>
        /// Queries the character's attributes. Called on success or failure of implant
        /// import as attributes must be done second.
        /// </summary>
        private void QueryAttributesAsync(CCPCharacter target)
        {
            // Fire and forget - the async method handles the result internally
            _ = QueryAttributesInternalAsync(target);
        }

        /// <summary>
        /// Internal async implementation for querying character attributes.
        /// </summary>
        private async Task QueryAttributesInternalAsync(CCPCharacter target)
        {
            // This is only invoked where the character has already been checked against null
            ESIKey esiKey = target.Identity.FindAPIKeyWithAccess(ESIAPICharacterMethods.
                Attributes);
            if (esiKey != null && !EsiErrors.IsErrorCountExceeded)
            {
                var result = await EveMonClient.APIProviders.CurrentProvider.QueryEsiAsync<EsiAPIAttributes>(
                    ESIAPICharacterMethods.Attributes,
                    new ESIParams(m_attrResponse, esiKey.AccessToken)
                    {
                        ParamOne = target.CharacterID
                    }).ConfigureAwait(false);

                // Marshal back to UI thread for processing
                Dispatcher.Invoke(() => OnCharacterAttributesUpdated(result));
            }
        }

        /// <summary>
        /// Processes the queried character's attributes.
        /// </summary>
        private void OnCharacterAttributesUpdated(EsiResult<EsiAPIAttributes> result)
        {
            var target = m_ccpCharacter;
            m_attrResponse = result.Response;
            // Character may have been deleted since we queried
            if (target != null && target.Monitored)
            {
                if (target.ShouldNotifyError(result, ESIAPICharacterMethods.Attributes))
                    EveMonClient.Notifications.NotifyCharacterAttributesError(target, result);
                if (!result.HasError && result.HasData && result.Result != null)
                    target.Import(result.Result);
            }
        }

        #endregion


        #region Production Mode — Callbacks: Market Orders + History Chain

        /// <summary>
        /// Queries the character's historical market orders.
        /// </summary>
        private void OnMarketOrdersUpdated(EsiAPIMarketOrders result)
        {
            // Fire and forget - the async method handles the result internally
            _ = OnMarketOrdersUpdatedInternalAsync(result);
        }

        /// <summary>
        /// Internal async implementation for fetching market order history.
        /// </summary>
        private async Task OnMarketOrdersUpdatedInternalAsync(EsiAPIMarketOrders regularOrders)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                var esiKey = target.Identity.FindAPIKeyWithAccess(ESIAPICharacterMethods.
                    MarketOrders);
                if (esiKey != null && !EsiErrors.IsErrorCountExceeded)
                {
                    var historyResult = await EveMonClient.APIProviders.CurrentProvider.QueryEsiAsync<EsiAPIMarketOrders>(
                        ESIAPICharacterMethods.MarketOrdersHistory,
                        new ESIParams(m_orderHistoryResponse, esiKey.AccessToken)
                        {
                            ParamOne = target.CharacterID
                        }).ConfigureAwait(false);

                    // Marshal back to UI thread for processing
                    Dispatcher.Invoke(() => OnMarketOrdersCompleted(historyResult, regularOrders));
                }
                else
                {
                    Dispatcher.Invoke(() => OnMarketOrdersCompleted(null, regularOrders));
                }
            }
        }

        /// <summary>
        /// Processes the queried character's market orders. Called from the history fetch on
        /// success or failure, but merges the original orders too.
        /// </summary>
        private void OnMarketOrdersCompleted(EsiResult<EsiAPIMarketOrders>? historyResult,
            EsiAPIMarketOrders regularOrders)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null && regularOrders != null)
            {
                var endedOrders = new LinkedList<MarketOrder>();
                var allOrders = new EsiAPIMarketOrders();
                if (historyResult != null)
                    m_orderHistoryResponse = historyResult.Response;
                // Ignore the If-Modified-Since and cache timer on order history to ensure
                // that old orders are not wiped out
                if (m_orderHistoryResponse != null)
                {
                    m_orderHistoryResponse.Expires = null;
                    m_orderHistoryResponse.ETag = null!;
                }
                // Add normal orders first
                allOrders.AddRange(regularOrders);
                // Add result second
                if (historyResult != null && !historyResult.HasError && historyResult.Result != null)
                    allOrders.AddRange(historyResult.Result);
                allOrders.SetAllIssuedBy(target.CharacterID);
                target.CharacterMarketOrders.Import(allOrders, IssuedFor.Character,
                    endedOrders);
                EveMonClient.OnCharacterMarketOrdersUpdated(target, endedOrders);
                allOrders.Clear();
                // Notify if either one failed
                if (historyResult != null && historyResult.HasError)
                    EveMonClient.Notifications.NotifyCharacterMarketOrdersError(target,
                        historyResult);
            }
        }

        #endregion


        #region Production Mode — Callbacks: Collections

        /// <summary>
        /// Processes the queried character's standings information.
        /// </summary>
        private void OnStandingsUpdated(EsiAPIStandings result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.Standings.Import(result);
                EveMonClient.OnCharacterStandingsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's factional warfare statistic information.
        /// </summary>
        private void OnFactionalWarfareStatsUpdated(EsiAPIFactionalWarfareStats result)
        {
            var target = m_ccpCharacter;
            int factionID = result.FactionID;
            // Character may have been deleted since we queried
            if (target != null)
            {
                if (factionID != 0)
                {
                    target.IsFactionalWarfareNotEnlisted = false;
                    target.FactionalWarfareStats = new FactionalWarfareStats(result);
                }
                else
                    target.IsFactionalWarfareNotEnlisted = true;
                EveMonClient.OnCharacterFactionalWarfareStatsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's assets information.
        /// </summary>
        private void OnAssetsUpdated(EsiAPIAssetList result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
                TaskHelper.RunCPUBoundTaskAsync(() => target.Assets.Import(result)).
                    ContinueWith(_ =>
                    {
                        EveMonClient.OnCharacterAssetsUpdated(target);
                    }, EveMonClient.CurrentSynchronizationContext);
        }

        /// <summary>
        /// Processes the queried character's contracts.
        /// </summary>
        private void OnContractsUpdated(EsiAPIContracts result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                foreach (var contract in result)
                    contract.APIMethod = ESIAPICharacterMethods.Contracts;
                var endedContracts = new List<Contract>();
                target.CharacterContracts.Import(result, endedContracts);
                EveMonClient.OnCharacterContractsUpdated(target, endedContracts);
            }
        }

        /// <summary>
        /// Processes the queried character's wallet balance.
        /// </summary>
        private void OnWalletBalanceUpdated(string result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.Import(result);
                target.NotifyInsufficientBalance();
            }
        }

        /// <summary>
        /// Processes the queried character's wallet journal information.
        /// </summary>
        private void OnWalletJournalUpdated(EsiAPIWalletJournal result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.WalletJournal.Import(result.ToXMLItem().WalletJournal);
                EveMonClient.OnCharacterWalletJournalUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's wallet transactions information.
        /// </summary>
        private void OnWalletTransactionsUpdated(EsiAPIWalletTransactions result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.WalletTransactions.Import(result.ToXMLItem().WalletTransactions);
                EveMonClient.OnCharacterWalletTransactionsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's personal industry jobs.
        /// </summary>
        private void OnIndustryJobsUpdated(EsiAPIIndustryJobs result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.CharacterIndustryJobs.Import(result, IssuedFor.Character);
                EveMonClient.OnCharacterIndustryJobsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's research points.
        /// </summary>
        private void OnResearchPointsUpdated(EsiAPIResearchPoints result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.ResearchPoints.Import(result);
                EveMonClient.OnCharacterResearchPointsUpdated(target);
            }
        }

        #endregion


        #region Production Mode — Callbacks: Communications

        /// <summary>
        /// Processes the queried character's EVE mail messages.
        /// </summary>
        private void OnEVEMailMessagesUpdated(EsiAPIMailMessages result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.EVEMailMessages.Import(result.ToXMLItem().Messages);
                int newMessages = target.EVEMailMessages.NewMessages;
                if (newMessages != 0)
                    EveMonClient.Notifications.NotifyNewEVEMailMessages(target, newMessages);
            }
        }

        /// <summary>
        /// Processes the queried character's EVE mailing lists.
        /// </summary>
        private void OnEveMailingListsUpdated(EsiAPIMailingLists result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.EVEMailingLists.Import(result);
            }
        }

        /// <summary>
        /// Processes the queried character's EVE notifications.
        /// </summary>
        private void OnEVENotificationsUpdated(EsiAPINotifications result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.EVENotifications.Import(result);
                int newNotify = target.EVENotifications.NewNotifications;
                if (newNotify != 0)
                    EveMonClient.Notifications.NotifyNewEVENotifications(target, newNotify);
            }
        }

        #endregion


        #region Production Mode — Callbacks: Social

        /// <summary>
        /// Processes the queried character's contact list.
        /// </summary>
        private void OnContactsUpdated(EsiAPIContactsList result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.Contacts.Import(result);
                EveMonClient.OnCharacterContactsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's medals.
        /// </summary>
        private void OnMedalsUpdated(EsiAPIMedals result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.CharacterMedals.Import(result, true);
                EveMonClient.OnCharacterMedalsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's kill log.
        /// </summary>
        private void OnKillLogUpdated(EsiAPIKillLog result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.KillLog.Import(result);
                EveMonClient.OnCharacterKillLogUpdated(m_ccpCharacter);
            }
        }

        #endregion


        #region Production Mode — Callbacks: Other

        /// <summary>
        /// Processes the queried character's upcoming calendar events.
        /// </summary>
        private void OnUpcomingCalendarEventsUpdated(EsiAPICalendarEvents result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.UpcomingCalendarEvents.Import(result);
                EveMonClient.OnCharacterUpcomingCalendarEventsUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's planetary colonies.
        /// </summary>
        private void OnPlanetaryColoniesUpdated(EsiAPIPlanetaryColoniesList result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                // Invalidate previous notifications
                EveMonClient.Notifications.InvalidateCharacterPlanetaryPinCompleted(target);

                target.PlanetaryColonies.Import(result);
                EveMonClient.OnCharacterPlanetaryColoniesUpdated(target);
            }
        }

        /// <summary>
        /// Processes the queried character's loyalty point information.
        /// </summary>
        private void OnLoyaltyPointsUpdated(EsiAPILoyality result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target != null)
            {
                target.LoyaltyPoints.Import(result);
                EveMonClient.OnCharacterLoyaltyPointsUpdated(target);
            }
        }

        #endregion


        #region Dispose

        /// <summary>
        /// Cleans up resources and unregisters from scheduler.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;

            if (_isProductionMode)
            {
                if (m_schedulerAdapter != null)
                {
                    EveMonClient.SmartQueryScheduler?.Unregister(m_schedulerAdapter);
                    m_schedulerAdapter = null;
                }

                // Unsubscribe events in monitors
                if (m_characterQueryMonitors != null)
                {
                    foreach (var monitor in m_characterQueryMonitors)
                        monitor.Dispose();
                }
            }
            else
            {
                _scheduler!.Unregister(this);

                lock (_lock)
                {
                    _monitors!.Clear();
                }
            }
        }

        #endregion


        #region MonitorState (Test Mode)

        /// <summary>
        /// Tracks the state of an individual query monitor within the orchestrator (test mode only).
        /// </summary>
        private class MonitorState
        {
            public int DataType { get; set; }
            public bool IsActive { get; set; }
            public bool HasCompletedOnce { get; set; }
            public DateTime NextQueryTime { get; set; }
            public int ConsecutiveNotModified { get; set; }
            public object CachedResult { get; set; }
        }

        #endregion
    }
}
