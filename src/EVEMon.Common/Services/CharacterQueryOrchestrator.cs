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
using Microsoft.Extensions.Logging;
using CommonEvents = EVEMon.Common.Events;

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


        #region Tier Classification

        /// <summary>
        /// Tier 0 (Monitor): Endpoints needed for overview display and alerting.
        /// Polled for ALL monitored characters, every scheduler tick.
        /// </summary>
        private static readonly HashSet<ESIAPICharacterMethods> Tier0Methods = new()
        {
            ESIAPICharacterMethods.CharacterSheet,
            ESIAPICharacterMethods.Skills,
            ESIAPICharacterMethods.SkillQueue,
            ESIAPICharacterMethods.AccountBalance,
            ESIAPICharacterMethods.Location,
            ESIAPICharacterMethods.Clones,
            ESIAPICharacterMethods.Implants,
            ESIAPICharacterMethods.Ship,
            ESIAPICharacterMethods.MarketOrders,       // order expiry alerts
            ESIAPICharacterMethods.Contracts,          // contract alerts
            ESIAPICharacterMethods.IndustryJobs,       // job completion alerts
            ESIAPICharacterMethods.MailMessages,       // new mail count
            ESIAPICharacterMethods.Notifications,      // sovereignty/war alerts
            ESIAPICharacterMethods.PlanetaryColonies,  // extraction complete
        };

        /// <summary>
        /// Tier 1 (Detail): Endpoints only polled when character tab is open.
        /// </summary>
        private static readonly HashSet<ESIAPICharacterMethods> Tier1Methods = new()
        {
            ESIAPICharacterMethods.AssetList,
            ESIAPICharacterMethods.WalletJournal,
            ESIAPICharacterMethods.WalletTransactions,
            ESIAPICharacterMethods.KillLog,
            ESIAPICharacterMethods.EmploymentHistory,
            ESIAPICharacterMethods.ContactList,
            ESIAPICharacterMethods.MailingLists,
        };

        // Tier 2 (Archive): Everything else — Standings, FW stats, Medals,
        // ResearchPoints, Calendar, LoyaltyPoints. Enabled always but at
        // ESI-dictated slow cache intervals (hours).

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
        private List<IQueryMonitorEx>? m_tier0Monitors;
        private List<IQueryMonitorEx>? m_tier1Monitors;
        private List<IQueryMonitorEx>? m_tier2Monitors;
        private volatile bool m_isActiveCharacter;
        private bool m_characterSheetUpdating;

        private readonly ILogger? m_logger;

        // Responses from the attribute results since we handle it manually
        private ResponseParams? m_attrResponse;
        // Result from the character skill queue to handle a pathological case where skill
        // queues were not-modified but need to be re-imported due to a skills list change
        private EsiAPISkillQueue? m_lastQueue;
        // Stashed skills result for order-independent skills+queue coordination.
        // When skills arrive before the queue, they are stored here and imported
        // once the queue arrives (or immediately if queue is already cached).
        private EsiAPISkills? m_lastSkills;
        // Responses from the market order history results since we handle it manually
        private ResponseParams? m_orderHistoryResponse;

        // Staggered startup fields - prevents all characters from querying at once
        // (retained for startup-complete tracking; actual scheduling moved to EsiScheduler)
        private bool m_startupDelayCompleted = false;

        // Subscription to fetch completion events for updating monitor status (UI throbber)
        private IDisposable? m_fetchCompletedSub;

        #endregion


        #region Test Mode Fields

        private readonly IEsiScheduler? _esiScheduler;
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
        internal CharacterQueryOrchestrator(CCPCharacter ccpCharacter, ILogger<CharacterQueryOrchestrator>? logger = null)
        {
            m_ccpCharacter = ccpCharacter ?? throw new ArgumentNullException(nameof(ccpCharacter));
            m_logger = logger;
            _isProductionMode = true;
            _characterId = ccpCharacter.CharacterID;
            _characterName = ccpCharacter.Name ?? string.Empty;

            m_characterQueryMonitors = CreateMonitors(ccpCharacter);
            m_characterQueryMonitors.ForEach(monitor => ccpCharacter.QueryMonitors.Add(monitor));

            m_basicFeaturesMonitors = InitializeBasicFeaturesMonitors(ccpCharacter);
            ClassifyMonitorsByTier();

            // Register with EsiScheduler for priority-based fetching
            RegisterWithEsiScheduler(ccpCharacter);

            // Subscribe to fetch events to update monitor status for UI throbber
            m_fetchCompletedSub = AppServices.EventAggregator?.Subscribe<Core.Events.MonitorFetchCompletedEvent>(
                OnFetchCompleted);

            // Mark startup as completed — staggered startup is now handled by ColdStartPlanner
            m_startupDelayCompleted = true;
        }

        #endregion


        #region Test Constructor

        /// <summary>
        /// Test constructor — uses abstract MonitorState for scheduling tests.
        /// Does not register with EsiScheduler (test mode manages its own tick cycle).
        /// </summary>
        public CharacterQueryOrchestrator(
            IEsiScheduler scheduler,
            IEsiClient esiClient,
            IEventAggregator eventAggregator,
            long characterId,
            string characterName)
        {
            _esiScheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
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
        /// Registers this character's endpoints with the EsiScheduler.
        /// Each endpoint gets a typed async closure that performs the full HTTP fetch cycle.
        /// The scheduler's ColdStartPlanner handles staggered startup timing.
        /// </summary>
        private void RegisterWithEsiScheduler(CCPCharacter ccpCharacter)
        {
            var scheduler = AppServices.EsiScheduler;
            if (scheduler == null)
                return;

            var notifiers = AppServices.Notifications;
            var registrations = new List<Core.Interfaces.EndpointRegistration>();

            // 1. CharacterSheet
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.CharacterSheet,
                ExecuteAsync = CreateFetchFunc<EsiAPICharacterSheet>(
                    ESIAPICharacterMethods.CharacterSheet,
                    OnCharacterSheetUpdated,
                    (c, r) => notifiers.NotifyCharacterSheetError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.CharacterSheet,
            });

            // 2. Location
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Location,
                ExecuteAsync = CreateFetchFunc<EsiAPILocation>(
                    ESIAPICharacterMethods.Location,
                    OnCharacterLocationUpdated,
                    (c, r) => notifiers.NotifyCharacterLocationError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Location,
            });

            // 3. Clones
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Clones,
                ExecuteAsync = CreateFetchFunc<EsiAPIClones>(
                    ESIAPICharacterMethods.Clones,
                    OnCharacterClonesUpdated,
                    (c, r) => notifiers.NotifyCharacterClonesError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Clones,
            });

            // 4. Implants (special: failure handler also chains to attributes)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Implants,
                ExecuteAsync = CreateFetchFunc<List<int>>(
                    ESIAPICharacterMethods.Implants,
                    OnCharacterImplantsUpdated,
                    OnCharacterImplantsFailed),
                RequiredScope = (ulong)ESIAPICharacterMethods.Implants,
            });

            // 5. Ship
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Ship,
                ExecuteAsync = CreateFetchFunc<EsiAPIShip>(
                    ESIAPICharacterMethods.Ship,
                    OnCharacterShipUpdated,
                    (c, r) => notifiers.NotifyCharacterShipError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Ship,
            });

            // 6. Skills
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Skills,
                ExecuteAsync = CreateFetchFunc<EsiAPISkills>(
                    ESIAPICharacterMethods.Skills,
                    OnCharacterSkillsUpdated,
                    (c, r) => notifiers.NotifyCharacterSkillsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Skills,
            });

            // 7. SkillQueue
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.SkillQueue,
                ExecuteAsync = CreateFetchFunc<EsiAPISkillQueue>(
                    ESIAPICharacterMethods.SkillQueue,
                    OnSkillQueueUpdated,
                    (c, r) => notifiers.NotifySkillQueueError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.SkillQueue,
            });

            // 8. EmploymentHistory
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.EmploymentHistory,
                ExecuteAsync = CreateFetchFunc<EsiAPIEmploymentHistory>(
                    ESIAPICharacterMethods.EmploymentHistory,
                    OnCharacterEmploymentUpdated,
                    (c, r) => notifiers.NotifyCharacterEmploymentError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.EmploymentHistory,
            });

            // 9. Standings (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Standings,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIStandings, EsiStandingsListItem>(
                    ESIAPICharacterMethods.Standings,
                    OnStandingsUpdated,
                    (c, r) => notifiers.NotifyCharacterStandingsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Standings,
            });

            // 10. ContactList (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.ContactList,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIContactsList, EsiContactListItem>(
                    ESIAPICharacterMethods.ContactList,
                    OnContactsUpdated,
                    (c, r) => notifiers.NotifyCharacterContactsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.ContactList,
            });

            // 11. FactionalWarfareStats
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.FactionalWarfareStats,
                ExecuteAsync = CreateFetchFunc<EsiAPIFactionalWarfareStats>(
                    ESIAPICharacterMethods.FactionalWarfareStats,
                    OnFactionalWarfareStatsUpdated,
                    (c, r) => notifiers.NotifyCharacterFactionalWarfareStatsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.FactionalWarfareStats,
            });

            // 12. Medals (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Medals,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIMedals, EsiMedalsListItem>(
                    ESIAPICharacterMethods.Medals,
                    OnMedalsUpdated,
                    (c, r) => notifiers.NotifyCharacterMedalsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Medals,
            });

            // 13. KillLog (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.KillLog,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIKillLog, EsiKillLogListItem>(
                    ESIAPICharacterMethods.KillLog,
                    OnKillLogUpdated,
                    (c, r) => notifiers.NotifyCharacterKillLogError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.KillLog,
            });

            // 14. AssetList (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.AssetList,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIAssetList, EsiAssetListItem>(
                    ESIAPICharacterMethods.AssetList,
                    OnAssetsUpdated,
                    (c, r) => notifiers.NotifyCharacterAssetsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.AssetList,
            });

            // 15. MarketOrders
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.MarketOrders,
                ExecuteAsync = CreateFetchFunc<EsiAPIMarketOrders>(
                    ESIAPICharacterMethods.MarketOrders,
                    OnMarketOrdersUpdated,
                    (c, r) => notifiers.NotifyCharacterMarketOrdersError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.MarketOrders,
            });

            // 16. Contracts (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Contracts,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIContracts, EsiContractListItem>(
                    ESIAPICharacterMethods.Contracts,
                    OnContractsUpdated,
                    (c, r) => notifiers.NotifyCharacterContractsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Contracts,
            });

            // 17. WalletJournal (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.WalletJournal,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIWalletJournal, EsiWalletJournalListItem>(
                    ESIAPICharacterMethods.WalletJournal,
                    OnWalletJournalUpdated,
                    (c, r) => notifiers.NotifyCharacterWalletJournalError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.WalletJournal,
            });

            // 18. AccountBalance
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.AccountBalance,
                ExecuteAsync = CreateFetchFunc<string>(
                    ESIAPICharacterMethods.AccountBalance,
                    OnWalletBalanceUpdated,
                    (c, r) => notifiers.NotifyCharacterBalanceError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.AccountBalance,
            });

            // 19. WalletTransactions (PAGED)
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.WalletTransactions,
                ExecuteAsync = CreatePagedFetchFunc<EsiAPIWalletTransactions, EsiWalletTransactionsListItem>(
                    ESIAPICharacterMethods.WalletTransactions,
                    OnWalletTransactionsUpdated,
                    (c, r) => notifiers.NotifyCharacterWalletTransactionsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.WalletTransactions,
            });

            // 20. IndustryJobs
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.IndustryJobs,
                ExecuteAsync = CreateFetchFunc<EsiAPIIndustryJobs>(
                    ESIAPICharacterMethods.IndustryJobs,
                    OnIndustryJobsUpdated,
                    (c, r) => notifiers.NotifyCharacterIndustryJobsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.IndustryJobs,
            });

            // 21. ResearchPoints
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.ResearchPoints,
                ExecuteAsync = CreateFetchFunc<EsiAPIResearchPoints>(
                    ESIAPICharacterMethods.ResearchPoints,
                    OnResearchPointsUpdated,
                    (c, r) => notifiers.NotifyCharacterResearchPointsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.ResearchPoints,
            });

            // 22. MailMessages
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.MailMessages,
                ExecuteAsync = CreateFetchFunc<EsiAPIMailMessages>(
                    ESIAPICharacterMethods.MailMessages,
                    OnEVEMailMessagesUpdated,
                    (c, r) => notifiers.NotifyEVEMailMessagesError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.MailMessages,
            });

            // 23. MailingLists
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.MailingLists,
                ExecuteAsync = CreateFetchFunc<EsiAPIMailingLists>(
                    ESIAPICharacterMethods.MailingLists,
                    OnEveMailingListsUpdated,
                    (c, r) => notifiers.NotifyMailingListsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.MailingLists,
            });

            // 24. Notifications
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.Notifications,
                ExecuteAsync = CreateFetchFunc<EsiAPINotifications>(
                    ESIAPICharacterMethods.Notifications,
                    OnEVENotificationsUpdated,
                    (c, r) => notifiers.NotifyEVENotificationsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.Notifications,
            });

            // 25. UpcomingCalendarEvents
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.UpcomingCalendarEvents,
                ExecuteAsync = CreateFetchFunc<EsiAPICalendarEvents>(
                    ESIAPICharacterMethods.UpcomingCalendarEvents,
                    OnUpcomingCalendarEventsUpdated,
                    (c, r) => notifiers.NotifyCharacterUpcomingCalendarEventsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.UpcomingCalendarEvents,
            });

            // 26. PlanetaryColonies
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.PlanetaryColonies,
                ExecuteAsync = CreateFetchFunc<EsiAPIPlanetaryColoniesList>(
                    ESIAPICharacterMethods.PlanetaryColonies,
                    OnPlanetaryColoniesUpdated,
                    (c, r) => notifiers.NotifyCharacterPlanetaryColoniesError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.PlanetaryColonies,
            });

            // 27. LoyaltyPoints
            registrations.Add(new Core.Interfaces.EndpointRegistration
            {
                Method = (long)ESIAPICharacterMethods.LoyaltyPoints,
                ExecuteAsync = CreateFetchFunc<EsiAPILoyality>(
                    ESIAPICharacterMethods.LoyaltyPoints,
                    OnLoyaltyPointsUpdated,
                    (c, r) => notifiers.NotifyCharacterLoyaltyPointsError(c, r)),
                RequiredScope = (ulong)ESIAPICharacterMethods.LoyaltyPoints,
            });

            scheduler.RegisterCharacter(ccpCharacter.CharacterID, registrations);

            AppServices.TraceService?.Trace(
                $"CharacterQueryOrchestrator - {ccpCharacter.Name} registered with EsiScheduler ({registrations.Count} endpoints)");
        }

        /// <summary>
        /// Finds the query monitor for the given ESI method (for status updates).
        /// </summary>
        private IQueryMonitorEx? FindMonitor(ESIAPICharacterMethods method)
        {
            if (m_characterQueryMonitors == null)
                return null;

            foreach (var monitor in m_characterQueryMonitors)
            {
                if (monitor.Method is ESIAPICharacterMethods m && m == method)
                    return monitor;
            }
            return null;
        }

        /// <summary>
        /// Creates a typed async closure for a single non-paged ESI endpoint.
        /// The closure handles: ESI key lookup, HTTP call, success/error callbacks,
        /// monitor status updates (for UI throbber), and FetchOutcome.
        /// </summary>
        private Func<string?, Task<FetchOutcome>> CreateFetchFunc<T>(
            ESIAPICharacterMethods method,
            Action<T> onSuccess,
            Action<CCPCharacter, EsiResult<T>>? onError = null) where T : class
        {
            // Capture the monitor for this endpoint so we can update its status
            var monitor = FindMonitor(method);

            return async (etag) =>
            {
                var target = m_ccpCharacter;
                if (target == null)
                    return new FetchOutcome { StatusCode = 0 };

                var esiKey = target.Identity.FindAPIKeyWithAccess(method);
                if (esiKey == null || EsiErrors.IsErrorCountExceeded)
                    return new FetchOutcome { StatusCode = 0 };

                // Set monitor to Updating for UI throbber
                Dispatcher.Post(() => monitor?.SetExternalStatus(true));

                var lastResponse = etag != null
                    ? new ResponseParams(0) { ETag = etag }
                    : null;

                var result = await AppServices.APIProviders.CurrentProvider.QueryEsiAsync<T>(
                    method,
                    new ESIParams(lastResponse, esiKey.AccessToken)
                    {
                        ParamOne = target.CharacterID
                    }).ConfigureAwait(false);

                var cachedUntil = result.CachedUntil;

                // Marshal callback to UI thread and update monitor status + timer
                if (!result.HasError && result.HasData && result.Result != null)
                {
                    Dispatcher.Post(() =>
                    {
                        onSuccess(result.Result);
                        monitor?.SetExternalStatus(false, DateTime.UtcNow);
                        if (cachedUntil != default)
                            monitor?.SetCachedUntilOverride(cachedUntil);
                    });
                }
                else
                {
                    Dispatcher.Post(() =>
                    {
                        if (result.HasError && onError != null && target.ShouldNotifyError(result, method))
                            onError(target, result);
                        monitor?.SetExternalStatus(false, DateTime.UtcNow);
                        if (cachedUntil != default)
                            monitor?.SetCachedUntilOverride(cachedUntil);
                    });
                }

                return new FetchOutcome
                {
                    StatusCode = result.ResponseCode,
                    CachedUntil = cachedUntil,
                    ETag = result.Response?.ETag,
                    RateLimitRemaining = result.Response?.RateLimitRemaining,
                    RetryAfterSeconds = result.Response?.RetryAfterSeconds,
                };
            };
        }

        /// <summary>
        /// Creates a typed async closure for a paged ESI collection endpoint.
        /// </summary>
        private Func<string?, Task<FetchOutcome>> CreatePagedFetchFunc<T, U>(
            ESIAPICharacterMethods method,
            Action<T> onSuccess,
            Action<CCPCharacter, EsiResult<T>>? onError = null) where T : List<U> where U : class
        {
            var monitor = FindMonitor(method);

            return async (etag) =>
            {
                var target = m_ccpCharacter;
                if (target == null)
                    return new FetchOutcome { StatusCode = 0 };

                var esiKey = target.Identity.FindAPIKeyWithAccess(method);
                if (esiKey == null || EsiErrors.IsErrorCountExceeded)
                    return new FetchOutcome { StatusCode = 0 };

                // Set monitor to Updating for UI throbber
                Dispatcher.Post(() => monitor?.SetExternalStatus(true));

                var lastResponse = etag != null
                    ? new ResponseParams(0) { ETag = etag }
                    : null;

                var result = await AppServices.APIProviders.CurrentProvider.QueryPagedEsiAsync<T, U>(
                    method,
                    new ESIParams(lastResponse, esiKey.AccessToken)
                    {
                        ParamOne = target.CharacterID
                    }).ConfigureAwait(false);

                var cachedUntil = result.CachedUntil;

                // Marshal callback to UI thread and update monitor status + timer
                if (!result.HasError && result.HasData && result.Result != null)
                {
                    Dispatcher.Post(() =>
                    {
                        onSuccess(result.Result);
                        monitor?.SetExternalStatus(false, DateTime.UtcNow);
                        if (cachedUntil != default)
                            monitor?.SetCachedUntilOverride(cachedUntil);
                    });
                }
                else
                {
                    Dispatcher.Post(() =>
                    {
                        if (result.HasError && onError != null && target.ShouldNotifyError(result, method))
                            onError(target, result);
                        monitor?.SetExternalStatus(false, DateTime.UtcNow);
                        if (cachedUntil != default)
                            monitor?.SetCachedUntilOverride(cachedUntil);
                    });
                }

                return new FetchOutcome
                {
                    StatusCode = result.ResponseCode,
                    CachedUntil = result.CachedUntil,
                    ETag = result.Response?.ETag,
                    RateLimitRemaining = result.Response?.RateLimitRemaining,
                    RetryAfterSeconds = result.Response?.RetryAfterSeconds,
                };
            };
        }

        /// <summary>
        /// Creates all ESI query monitors for the character.
        /// Monitors are ordered as they appear in the throbber menu.
        /// </summary>
        private List<IQueryMonitorEx> CreateMonitors(CCPCharacter ccpCharacter)
        {
            var notifiers = AppServices.Notifications;
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

        /// <summary>
        /// Classifies all monitors into Tier 0 (Monitor), Tier 1 (Detail), or Tier 2 (Archive)
        /// based on their ESI method. Called once after monitor creation.
        /// </summary>
        private void ClassifyMonitorsByTier()
        {
            m_tier0Monitors = new List<IQueryMonitorEx>();
            m_tier1Monitors = new List<IQueryMonitorEx>();
            m_tier2Monitors = new List<IQueryMonitorEx>();

            foreach (var monitor in m_characterQueryMonitors!)
            {
                if (monitor.Method is ESIAPICharacterMethods method)
                {
                    if (Tier0Methods.Contains(method))
                        m_tier0Monitors.Add(monitor);
                    else if (Tier1Methods.Contains(method))
                        m_tier1Monitors.Add(monitor);
                    else
                        m_tier2Monitors.Add(monitor);
                }
            }
        }

        /// <summary>
        /// Sets whether this character's tab is currently active (visible) in the UI.
        /// When active, Tier 1 (Detail) monitors are enabled. When inactive, they are disabled.
        /// Called by <see cref="ActiveCharacterTierSubscriber"/> in response to tab switches.
        /// </summary>
        internal void SetActiveCharacter(bool isActive)
        {
            m_isActiveCharacter = isActive;
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
        /// Production mode tick: drives real monitors via UpdateTick() with three-tier activation.
        /// Tier 0 (Monitor): Always active for all monitored characters (overview + alerts).
        /// Tier 1 (Detail): Only active when this character's tab is open.
        /// Tier 2 (Archive): Always enabled (slow ESI cache intervals handle frequency).
        /// </summary>
        private void ProcessTickProduction()
        {
            bool monitored = m_ccpCharacter!.Monitored;

            // Tier 0: Always active for monitored characters (overview + alerts)
            foreach (var monitor in m_tier0Monitors!)
                monitor.Enabled = monitored;

            // Tier 1: Enabled for all monitored characters (background fetch).
            // EsiScheduler prioritizes the visible character via SetVisibleCharacter().
            // After 5-10 minutes of running, all data is cached — every tab switch is instant.
            foreach (var monitor in m_tier1Monitors!)
                monitor.Enabled = monitored;

            // Tier 2: Always enabled (slow ESI cache intervals handle frequency)
            foreach (var monitor in m_tier2Monitors!)
                monitor.Enabled = monitored;

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


        #region Production Mode — Monitor Status Bridge

        /// <summary>
        /// Updates query monitor status from EsiScheduler fetch completion events.
        /// Bridges the scheduler's direct HTTP path to the legacy monitor status display (throbber).
        /// </summary>
        private void OnFetchCompleted(Core.Events.MonitorFetchCompletedEvent e)
        {
            if (e.CharacterId != _characterId || !_isProductionMode)
                return;

            if (m_characterQueryMonitors == null)
                return;

            foreach (var monitor in m_characterQueryMonitors)
            {
                if (monitor.Method is Enumerations.CCPAPI.ESIAPICharacterMethods method
                    && (long)method == e.EndpointMethod)
                {
                    monitor.Enabled = true;
                    break;
                }
            }
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
                    AppServices.Notifications.InvalidateCharacterAPIError(target);
                    // CharacterUpdated
                    AppServices.TraceService?.Trace($"CharacterUpdated: {target.Name}");
                    AppServices.EventAggregator?.Publish(new CharacterUpdatedEvent(target.CharacterID, target.Name));
                    AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(target));
                    // CharacterInfoUpdated
                    AppServices.TraceService?.Trace($"CharacterInfoUpdated: {target.Name}");
                    AppServices.EventAggregator?.Publish(new CharacterInfoUpdatedEvent(target.CharacterID, target.Name));
                    AppServices.EventAggregator?.Publish(new CommonEvents.CharacterInfoUpdatedEvent(target));
                    // CharacterImplantSetCollectionChanged
                    AppServices.TraceService?.Trace($"CharacterImplantSetCollectionChanged: {target.Name}");
                    AppServices.EventAggregator?.Publish(new CommonEvents.CharacterImplantSetCollectionChangedEvent(target));
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
                AppServices.TraceService?.Trace($"CharacterSheet updated - {target.Name} no longer cached");
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
        /// Stashes the result and attempts import — if the queue hasn't arrived yet,
        /// the import will occur when the queue callback fires.
        /// </summary>
        private void OnCharacterSkillsUpdated(EsiAPISkills result)
        {
            var target = m_ccpCharacter;
            // Character may have been deleted since we queried
            if (target == null)
                return;

            m_lastSkills = result;

            m_logger?.LogDebug(new EventId(6, "SKILL"),
                "skills stashed for {CharName}, attempting import (queue={HasQueue})",
                _characterName, m_lastQueue != null);

            TryImportSkills();
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

                // If skills arrived before queue, import them now
                TryImportSkills();

                // Check if the character has less than the threshold queue length
                if (target.IsTraining && target.SkillQueue.LessThanWarningThreshold)
                    AppServices.Notifications.NotifySkillQueueThreshold(target,
                        Settings.UI.MainWindow.SkillQueueWarningThresholdDays);
                else
                    AppServices.Notifications.InvalidateSkillQueueThreshold(target);
            }
            else
                m_lastQueue = null;
        }

        /// <summary>
        /// Attempts to import skills using the latest skills and queue data.
        /// Called from both <see cref="OnCharacterSkillsUpdated"/> and
        /// <see cref="OnSkillQueueUpdated"/> to handle either-order arrival.
        /// </summary>
        private void TryImportSkills()
        {
            var target = m_ccpCharacter;
            if (target == null || m_lastSkills == null)
                return;

            // Import with queue if available, or without (Character.Import handles null queue)
            target.Import(m_lastSkills, m_lastQueue);

            m_logger?.LogInformation(new EventId(6, "SKILL"),
                "skills imported for {CharName} — {SkillCount} skills, queue={HasQueue}",
                _characterName, m_lastSkills.Skills?.Count ?? 0, m_lastQueue != null);

            // Clear stashed skills after successful import (queue stays for future cycles)
            m_lastSkills = null;
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
            AppServices.Notifications.NotifyCharacterImplantsError(character, result);
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
                var result = await AppServices.APIProviders.CurrentProvider.QueryEsiAsync<EsiAPIAttributes>(
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
                    AppServices.Notifications.NotifyCharacterAttributesError(target, result);
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
                    var historyResult = await AppServices.APIProviders.CurrentProvider.QueryEsiAsync<EsiAPIMarketOrders>(
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
                // CharacterMarketOrdersUpdated
                AppServices.TraceService?.Trace($"CharacterMarketOrdersUpdated: {target.Name}");
                (target as CCPCharacter)?.OnCharacterMarketOrdersUpdated(endedOrders);
                AppServices.EventAggregator?.Publish(new CharacterMarketOrdersUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterMarketOrdersUpdatedEvent(target, endedOrders));
                allOrders.Clear();
                // Notify if either one failed
                if (historyResult != null && historyResult.HasError)
                    AppServices.Notifications.NotifyCharacterMarketOrdersError(target,
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
                // CharacterStandingsUpdated
                AppServices.TraceService?.Trace($"CharacterStandingsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterStandingsUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterStandingsUpdatedEvent(target));
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
                // CharacterFactionalWarfareStatsUpdated
                AppServices.TraceService?.Trace($"CharacterFactionalWarfareStatsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterFactionalWarfareStatsUpdatedEvent(target));
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
                        AppServices.Dispatcher?.Post(() =>
                        {
                            // CharacterAssetsUpdated
                            AppServices.TraceService?.Trace($"CharacterAssetsUpdated: {target.Name}");
                            (target as CCPCharacter)?.OnAssetsUpdated();
                            AppServices.EventAggregator?.Publish(new CharacterAssetsUpdatedEvent(target.CharacterID, target.Name));
                            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterAssetsUpdatedEvent(target));
                        });
                    });
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
                // CharacterContractsUpdated
                AppServices.TraceService?.Trace($"CharacterContractsUpdated: {target.Name}");
                (target as CCPCharacter)?.OnCharacterContractsUpdated(endedContracts);
                AppServices.EventAggregator?.Publish(new CharacterContractsUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContractsEndedEvent(target, endedContracts));
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
                // CharacterWalletJournalUpdated
                AppServices.TraceService?.Trace($"CharacterWalletJournalUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterWalletJournalUpdatedEvent(target));
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
                // CharacterWalletTransactionsUpdated
                AppServices.TraceService?.Trace($"CharacterWalletTransactionsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterWalletTransactionsUpdatedEvent(target));
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
                // CharacterIndustryJobsUpdated
                AppServices.TraceService?.Trace($"CharacterIndustryJobsUpdated: {target.Name}");
                (target as CCPCharacter)?.OnCharacterIndustryJobsUpdated();
                AppServices.EventAggregator?.Publish(new CharacterIndustryJobsUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.IndustryJobsUpdatedEvent(target));
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
                // CharacterResearchPointsUpdated
                AppServices.TraceService?.Trace($"CharacterResearchPointsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterResearchUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterResearchPointsUpdatedEvent(target));
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
                    AppServices.Notifications.NotifyNewEVEMailMessages(target, newMessages);
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
                    AppServices.Notifications.NotifyNewEVENotifications(target, newNotify);
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
                // CharacterContactsUpdated
                AppServices.TraceService?.Trace($"CharacterContactsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterContactsUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterContactsUpdatedEvent(target));
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
                // CharacterMedalsUpdated
                AppServices.TraceService?.Trace($"CharacterMedalsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterMedalsUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterMedalsUpdatedEvent(target));
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
                // CharacterKillLogUpdated
                AppServices.TraceService?.Trace($"CharacterKillLogUpdated: {m_ccpCharacter.Name}");
                AppServices.EventAggregator?.Publish(new CharacterKillLogUpdatedEvent(m_ccpCharacter.CharacterID, m_ccpCharacter.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterKillLogUpdatedEvent(m_ccpCharacter));
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
                // CharacterUpcomingCalendarEventsUpdated
                AppServices.TraceService?.Trace($"CharacterUpcomingCalendarEventsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterCalendarUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpcomingCalendarEventsUpdatedEvent(target));
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
                AppServices.Notifications.InvalidateCharacterPlanetaryPinCompleted(target);

                target.PlanetaryColonies.Import(result);
                // CharacterPlanetaryColoniesUpdated
                AppServices.TraceService?.Trace($"CharacterPlanetaryColoniesUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterPlanetaryUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterPlanetaryColoniesUpdatedEvent(target));
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
                // CharacterLoyaltyPointsUpdated
                AppServices.TraceService?.Trace($"CharacterLoyaltyPointsUpdated: {target.Name}");
                AppServices.EventAggregator?.Publish(new CharacterLoyaltyUpdatedEvent(target.CharacterID, target.Name));
                AppServices.EventAggregator?.Publish(new CommonEvents.CharacterLoyaltyPointsUpdatedEvent(target));
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
                // Unregister from EsiScheduler
                AppServices.EsiScheduler?.UnregisterCharacter(_characterId);

                // Dispose fetch completion subscription
                m_fetchCompletedSub?.Dispose();
                m_fetchCompletedSub = null;

                // Unsubscribe events in monitors
                if (m_characterQueryMonitors != null)
                {
                    foreach (var monitor in m_characterQueryMonitors)
                        monitor.Dispose();
                }
            }
            else
            {
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
