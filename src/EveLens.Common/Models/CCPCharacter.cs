// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Collections;
using EveLens.Common.Constants;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Interfaces;
using EveLens.Common.Models.Collections;
using EveLens.Common.Models.Extended;
using EveLens.Common.QueryMonitor;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Services;
using EveLens.Common.Serialization.Settings;
using Microsoft.Extensions.Logging;
using EveLens.Common.Extensions;
using EveLens.Core;
using CommonEvents = EveLens.Common.Events;

namespace EveLens.Common.Models
{
    /// <summary>
    /// Represents a character from CCP, with additional capacities for training and such.
    /// </summary>
    public sealed class CCPCharacter : Character
    {
        #region Fields

        private readonly List<MarketOrder> m_endedOrdersForCharacter;
        private readonly List<MarketOrder> m_endedOrdersForCorporation;
        private readonly List<Contract> m_endedContractsForCharacter;
        private readonly List<Contract> m_endedContractsForCorporation;
        private readonly List<IndustryJob> m_jobsCompletedForCharacter;

        private ICharacterDataQuerying? m_characterDataQuerying;
        private ICorporationDataQuerying? m_corporationDataQuerying;

        /// <summary>
        /// Gets the query orchestrator for this character, used by the tier subscriber
        /// to toggle Tier 1 (Detail) monitors on/off based on active tab.
        /// </summary>
        internal CharacterQueryOrchestrator? QueryOrchestrator =>
            m_characterDataQuerying as CharacterQueryOrchestrator;

        /// <summary>
        /// Gets whether this character has received its first ESI data update this session.
        /// Returns false during startup while waiting for the initial API fetch.
        /// Used by UI controls to show loading indicators instead of stale/empty data.
        /// </summary>
        public bool HasCompletedFirstUpdate =>
            QueryMonitors.Any() && QueryMonitors.Any(m => m.LastUpdate > DateTime.MinValue
                && m.LastUpdate.Year > 2000);
        private List<SerializableAPIUpdate>? m_lastAPIUpdates;

        private readonly ICharacterServices m_services;
        internal ICharacterServices Services => m_services;

        private Enum m_errorNotifiedMethod = CCPAPIMethodsEnum.None;
        private readonly Dictionary<int, int> m_consecutiveFailures = new();
        private bool m_isFwEnlisted;

        private string m_allianceName;
        private string m_corporationName;

        // Lazy-initialized collection backing fields
        private Lazy<StandingCollection> _standings;
        private Lazy<AssetCollection> _assets;
        private Lazy<MarketOrderCollection> _characterMarketOrders;
        private Lazy<MarketOrderCollection> _corporationMarketOrders;
        private Lazy<ContractCollection> _characterContracts;
        private Lazy<ContractCollection> _corporationContracts;
        private Lazy<IndustryJobCollection> _characterIndustryJobs;
        private Lazy<IndustryJobCollection> _corporationIndustryJobs;
        private Lazy<WalletJournalCollection> _walletJournal;
        private Lazy<WalletTransactionsCollection> _walletTransactions;
        private Lazy<ResearchPointCollection> _researchPoints;
        private Lazy<EveMailMessageCollection> _eveMailMessages;
        private Lazy<EveMailingListCollection> _eveMailingLists;
        private Lazy<EveNotificationCollection> _eveNotifications;
        private Lazy<ContactCollection> _contacts;
        private Lazy<MedalCollection> _characterMedals;
        private Lazy<MedalCollection> _corporationMedals;
        private Lazy<UpcomingCalendarEventCollection> _upcomingCalendarEvents;
        private Lazy<KillLogCollection> _killLog;
        private Lazy<PlanetaryColonyCollection> _planetaryColonies;
        private Lazy<LoyaltyCollection> _loyaltyPoints;

        #endregion


        #region Constructors

        /// <summary>
        /// Base constructor.
        /// </summary>
        /// <param name="identity"></param>
        /// <param name="guid"></param>
        private CCPCharacter(CharacterIdentity identity, Guid guid, ICharacterServices services = null)
            : base(identity, guid)
        {
            m_services = services ?? EveLensClientCharacterServices.Instance;
            QueryMonitors = new QueryMonitorCollection();
            SkillQueue = new SkillQueue(this);

            // Lazy-initialized collections: created on first access to reduce startup allocations.
            // For 30+ characters, deferring these saves significant constructor cost.
            _standings = new Lazy<StandingCollection>(() => new StandingCollection(this));
            _assets = new Lazy<AssetCollection>(() => new AssetCollection(this));
            _characterMarketOrders = new Lazy<MarketOrderCollection>(() => new MarketOrderCollection(this));
            _corporationMarketOrders = new Lazy<MarketOrderCollection>(() => new MarketOrderCollection(this));
            _characterContracts = new Lazy<ContractCollection>(() => new ContractCollection(this));
            _corporationContracts = new Lazy<ContractCollection>(() => new ContractCollection(this));
            _characterIndustryJobs = new Lazy<IndustryJobCollection>(() => new IndustryJobCollection(this));
            _corporationIndustryJobs = new Lazy<IndustryJobCollection>(() => new IndustryJobCollection(this));
            _walletJournal = new Lazy<WalletJournalCollection>(() => new WalletJournalCollection(this));
            _walletTransactions = new Lazy<WalletTransactionsCollection>(() => new WalletTransactionsCollection(this));
            _researchPoints = new Lazy<ResearchPointCollection>(() => new ResearchPointCollection(this));
            _eveMailMessages = new Lazy<EveMailMessageCollection>(() => new EveMailMessageCollection(this));
            _eveMailingLists = new Lazy<EveMailingListCollection>(() => new EveMailingListCollection(this));
            _eveNotifications = new Lazy<EveNotificationCollection>(() => new EveNotificationCollection(this));
            _contacts = new Lazy<ContactCollection>(() => new ContactCollection(this));
            _characterMedals = new Lazy<MedalCollection>(() => new MedalCollection(this));
            _corporationMedals = new Lazy<MedalCollection>(() => new MedalCollection(this));
            _upcomingCalendarEvents = new Lazy<UpcomingCalendarEventCollection>(() => new UpcomingCalendarEventCollection(this));
            _killLog = new Lazy<KillLogCollection>(() => new KillLogCollection(this));
            _planetaryColonies = new Lazy<PlanetaryColonyCollection>(() => new PlanetaryColonyCollection(this));
            _loyaltyPoints = new Lazy<LoyaltyCollection>(() => new LoyaltyCollection(this));

            m_endedOrdersForCharacter = new List<MarketOrder>();
            m_endedOrdersForCorporation = new List<MarketOrder>();

            m_endedContractsForCharacter = new List<Contract>();
            m_endedContractsForCorporation = new List<Contract>();

            m_jobsCompletedForCharacter = new List<IndustryJob>();
            m_allianceName = ServiceLocator.NameResolver?.GetName(AllianceID) ?? string.Empty;
            m_corporationName = ServiceLocator.NameResolver?.GetName(CorporationID) ?? string.Empty;

            // Safe to call now that SkillQueue and all collections are initialized
            // (moved out of base Character constructor to avoid virtual call before init)
            UpdateAccountStatus();

            // Character-specific events are dispatched directly by EveLensClient.OnXxx() methods
            // instead of each CCPCharacter subscribing and filtering. This eliminates 10N handlers
            // for N characters. Only global (non-character) events need subscriptions.
            m_services.ESIKeyInfoUpdated += EveLensClient_ESIKeyInfoUpdated;
            m_services.EveIDToNameUpdated += EveLensClient_EveIDToNameUpdated;
            m_services.FiveSecondTick += EveLensClient_TimerTick;
        }

        /// <summary>
        /// Deserialization constructor.
        /// </summary>
        /// <param name="identity">The identity for this character</param>
        /// <param name="serial">A deserialization object for characters</param>
        internal CCPCharacter(CharacterIdentity identity, SerializableCCPCharacter serial)
            : this(identity, serial.Guid)
        {
            Import(serial);
            m_lastAPIUpdates = serial.LastUpdates.ToList();
            ForceUpdateBasicFeatures = true;  // Force immediate ESI refresh on startup
        }

        /// <summary>
        /// Constructor for a new CCP character on a new identity.
        /// </summary>
        /// <param name="identity"></param>
        internal CCPCharacter(CharacterIdentity identity)
            : this(identity, Guid.NewGuid())
        {
            ForceUpdateBasicFeatures = true;
            m_lastAPIUpdates = new List<SerializableAPIUpdate>();
        }

        /// <summary>
        /// Deserialization constructor with custom services (used by CharacterFactory).
        /// </summary>
        internal CCPCharacter(CharacterIdentity identity, SerializableCCPCharacter serial, ICharacterServices services)
            : this(identity, serial.Guid, services)
        {
            Import(serial);
            m_lastAPIUpdates = serial.LastUpdates.ToList();
            ForceUpdateBasicFeatures = true;
        }

        /// <summary>
        /// Constructor for a new CCP character with custom services (used by CharacterFactory).
        /// </summary>
        internal CCPCharacter(CharacterIdentity identity, ICharacterServices services)
            : this(identity, Guid.NewGuid(), services)
        {
            ForceUpdateBasicFeatures = true;
            m_lastAPIUpdates = new List<SerializableAPIUpdate>();
        }

        #endregion


        #region Public Properties
        
        /// <summary>
        /// Gets an adorned name, with (file), (url) or (cached) labels.
        /// </summary>
        public override string AdornedName => !Identity.ESIKeys.Any() || Identity.ESIKeys.All(
            apiKey => !apiKey.Monitored) || (m_characterDataQuerying != null &&
            m_characterDataQuerying.HasCharacterSheetError) ? $"{Name} (cached)" : Name;

        /// <summary>
        /// Gets the skill queue for this character.
        /// </summary>
        public SkillQueue SkillQueue { get; }

        /// <summary>
        /// Gets the standings for this character.
        /// </summary>
        public StandingCollection Standings => _standings.Value;

        /// <summary>
        /// Gets the assets for this character.
        /// </summary>
        public AssetCollection Assets => _assets.Value;

        /// <summary>
        /// Gets the factional warfare stats for this character.
        /// </summary>
        public FactionalWarfareStats? FactionalWarfareStats { get; internal set; }

        /// <summary>
        /// Gets the wallet journal for this character.
        /// </summary>
        public WalletJournalCollection WalletJournal => _walletJournal.Value;

        /// <summary>
        /// Gets the wallet transactions for this character.
        /// </summary>
        public WalletTransactionsCollection WalletTransactions => _walletTransactions.Value;

        /// <summary>
        /// Gets the collection of market orders.
        /// </summary>
        public IEnumerable<MarketOrder> MarketOrders => CharacterMarketOrders.Concat(
            CorporationMarketOrders.Where(order => order.OwnerID == CharacterID));

        /// <summary>
        /// Gets the character market orders.
        /// </summary>
        public MarketOrderCollection CharacterMarketOrders => _characterMarketOrders.Value;

        /// <summary>
        /// Gets the corporation market orders.
        /// </summary>
        public MarketOrderCollection CorporationMarketOrders => _corporationMarketOrders.Value;

        /// <summary>
        /// Gets the collection of contracts.
        /// </summary>
        /// <remarks>Excludes contracts that appear in both collections</remarks>
        public IEnumerable<Contract> Contracts => CharacterContracts.Where(charContract =>
            CorporationContracts.All(corpContract => corpContract.ID != charContract.ID)).
            Concat(CorporationContracts.Where(contract => contract.IssuerID == CharacterID));

        /// <summary>
        /// Gets the character contracts.
        /// </summary>
        public ContractCollection CharacterContracts => _characterContracts.Value;

        /// <summary>
        /// Gets the corporation contracts.
        /// </summary>
        public ContractCollection CorporationContracts => _corporationContracts.Value;
        
        /// <summary>
        /// Gets the collection of industry jobs.
        /// </summary>
        public IEnumerable<IndustryJob> IndustryJobs => CharacterIndustryJobs.Concat(
            CorporationIndustryJobs.Where(job => job.InstallerID == CharacterID));

        /// <summary>
        /// Gets the character industry jobs.
        /// </summary>
        public IndustryJobCollection CharacterIndustryJobs => _characterIndustryJobs.Value;

        /// <summary>
        /// Gets the corporation industry jobs.
        /// </summary>
        public IndustryJobCollection CorporationIndustryJobs => _corporationIndustryJobs.Value;

        /// <summary>
        /// Gets the collection of research points.
        /// </summary>
        public ResearchPointCollection ResearchPoints => _researchPoints.Value;

        /// <summary>
        /// Gets the collection of EVE mail messages.
        /// </summary>
        public EveMailMessageCollection EVEMailMessages => _eveMailMessages.Value;

        /// <summary>
        /// Gets the collection of EVE mail messages.
        /// </summary>
        public EveMailingListCollection EVEMailingLists => _eveMailingLists.Value;

        /// <summary>
        /// Gets the collection of EVE notifications.
        /// </summary>
        public EveNotificationCollection EVENotifications => _eveNotifications.Value;

        /// <summary>
        /// Gets the collection of contacts.
        /// </summary>
        public ContactCollection Contacts => _contacts.Value;

        /// <summary>
        /// Gets the collection of character medals.
        /// </summary>
        public MedalCollection CharacterMedals => _characterMedals.Value;

        /// <summary>
        /// Gets the collection of corporation medals.
        /// </summary>
        public MedalCollection CorporationMedals => _corporationMedals.Value;

        /// <summary>
        /// Gets the collection of upcoming calendar events.
        /// </summary>
        public UpcomingCalendarEventCollection UpcomingCalendarEvents => _upcomingCalendarEvents.Value;

        /// <summary>
        /// Gets the collection of kill logs.
        /// </summary>
        public KillLogCollection KillLog => _killLog.Value;

        /// <summary>
        /// Gets the collection of planetary colonies.
        /// </summary>
        public PlanetaryColonyCollection PlanetaryColonies => _planetaryColonies.Value;

        /// <summary>
        /// Gets the collection of loyalty points.
        /// </summary>
        public LoyaltyCollection LoyaltyPoints => _loyaltyPoints.Value;

        /// <summary>
        /// Gets the query monitors enumeration.
        /// </summary>
        public QueryMonitorCollection QueryMonitors { get; }

        /// <summary>
        /// Gets true when the character is currently actively training, false otherwise.
        /// </summary>
        public override bool IsTraining => SkillQueue.IsTraining;

        /// <summary>
        /// Gets the skill currently in training, even when it is paused.
        /// </summary>
        public override QueuedSkill? CurrentlyTrainingSkill => SkillQueue?.CurrentlyTraining;

        /// <summary>
        /// Gets a value indicating whether the character has insufficient balance to complete its buy orders.
        /// </summary>
        /// <value>
        /// 	<c>true</c> if the character has sufficient balance; otherwise, <c>false</c>.
        /// </value>
        public bool HasSufficientBalance
        {
            get
            {
                var activeBuyOrders = MarketOrders.OfType<BuyOrder>().Where(order =>
                    (order.State == OrderState.Active || order.State == OrderState.Modified) &&
                    order.IssuedFor == IssuedFor.Character).ToList();

                decimal additionalToCover = activeBuyOrders.Sum(x => x.TotalPrice) -
                    activeBuyOrders.Sum(order => order.Escrow);

                return Balance >= additionalToCover;
            }

        }

        /// <summary>
        /// Gets a value indicating whether the character is enlisted in factional warfare.
        /// </summary>
        /// <value>
        ///   <c>true</c> if character is not enlisted in factional warfare; otherwise, <c>false</c>.
        /// </value>
        public bool IsFactionalWarfareNotEnlisted
        {
            get { return FactionID == 0 || m_isFwEnlisted; }
            internal set { m_isFwEnlisted = value; }
        }

        /// <summary>
        /// Gets true when a new character is created.
        /// </summary>
        public bool ForceUpdateBasicFeatures { get; }

        #endregion


        #region Cache Restore

        /// <summary>
        /// Restores live ESI data from the local disk cache.
        /// Called during startup after Import(serial) but before the ESI scheduler starts,
        /// so character tabs are populated instantly.
        /// </summary>
        internal async System.Threading.Tasks.Task RestoreFromCacheAsync()
        {
            var cache = AppServices.CharacterDataCache;
            long id = CharacterID;

            var assets = await cache.LoadAsync<Serialization.Esi.EsiAPIAssetList>(id, "assets");
            if (assets != null) Assets.Import(assets);

            var contacts = await cache.LoadAsync<Serialization.Esi.EsiAPIContactsList>(id, "contacts");
            if (contacts != null) Contacts.Import(contacts);

            var standings = await cache.LoadAsync<Serialization.Esi.EsiAPIStandings>(id, "standings");
            if (standings != null) Standings.Import(standings);

            var mailHeaders = await cache.LoadAsync<Serialization.Esi.EsiAPIMailMessages>(id, "mail_headers");
            if (mailHeaders != null) EVEMailMessages.Import(mailHeaders.ToXMLItem().Messages);

            var mailingLists = await cache.LoadAsync<Serialization.Esi.EsiAPIMailingLists>(id, "mailing_lists");
            if (mailingLists != null) EVEMailingLists.Import(mailingLists);

            var notifications = await cache.LoadAsync<Serialization.Esi.EsiAPINotifications>(id, "notifications");
            if (notifications != null) EVENotifications.Import(notifications);

            var walletJournal = await cache.LoadAsync<Serialization.Esi.EsiAPIWalletJournal>(id, "wallet_journal");
            if (walletJournal != null) WalletJournal.Import(walletJournal.ToXMLItem().WalletJournal);

            var walletTxns = await cache.LoadAsync<Serialization.Esi.EsiAPIWalletTransactions>(id, "wallet_transactions");
            if (walletTxns != null) WalletTransactions.Import(walletTxns.ToXMLItem().WalletTransactions);

            var killLog = await cache.LoadAsync<Serialization.Esi.EsiAPIKillLog>(id, "kill_log");
            if (killLog != null) KillLog.Import(killLog);

            var planetary = await cache.LoadAsync<Serialization.Esi.EsiAPIPlanetaryColoniesList>(id, "planetary");
            if (planetary != null) PlanetaryColonies.Import(planetary);

            var research = await cache.LoadAsync<Serialization.Esi.EsiAPIResearchPoints>(id, "research");
            if (research != null) ResearchPoints.Import(research);

            var loyalty = await cache.LoadAsync<Serialization.Esi.EsiAPILoyality>(id, "loyalty");
            if (loyalty != null) LoyaltyPoints.Import(loyalty);

            var calendar = await cache.LoadAsync<Serialization.Esi.EsiAPICalendarEvents>(id, "calendar");
            if (calendar != null) UpcomingCalendarEvents.Import(calendar);

            var medals = await cache.LoadAsync<Serialization.Esi.EsiAPIMedals>(id, "medals");
            if (medals != null) CharacterMedals.Import(medals, true);

            var contracts = await cache.LoadAsync<Serialization.Esi.EsiAPIContracts>(id, "contracts");
            if (contracts != null)
            {
                foreach (var contract in contracts)
                    contract.APIMethod = Enumerations.CCPAPI.ESIAPICharacterMethods.Contracts;
                var endedContracts = new System.Collections.Generic.List<Contract>();
                CharacterContracts.Import(contracts, endedContracts);
            }

            var marketOrders = await cache.LoadAsync<Serialization.Esi.EsiAPIMarketOrders>(id, "market_orders");
            if (marketOrders != null)
            {
                marketOrders.SetAllIssuedBy(id);
                var endedOrders = new System.Collections.Generic.LinkedList<MarketOrder>();
                CharacterMarketOrders.Import(marketOrders, Enumerations.IssuedFor.Character, endedOrders);
            }

            var industryJobs = await cache.LoadAsync<Serialization.Esi.EsiAPIIndustryJobs>(id, "industry_jobs");
            if (industryJobs != null)
            {
                try
                {
                    CharacterIndustryJobs.Import(industryJobs, Enumerations.IssuedFor.Character);
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace(
                        $"RestoreFromCacheAsync: {Name} — industry jobs import failed: {ex.Message}",
                        printMethod: false);
                }
            }

            var fwStats = await cache.LoadAsync<Serialization.Esi.EsiAPIFactionalWarfareStats>(id, "factional_warfare");
            if (fwStats != null)
            {
                if (fwStats.FactionID != 0)
                {
                    IsFactionalWarfareNotEnlisted = false;
                    FactionalWarfareStats = new FactionalWarfareStats(fwStats);
                }
                else
                    IsFactionalWarfareNotEnlisted = true;
            }

            AppServices.TraceService?.Trace($"RestoreFromCacheAsync: {Name} — cache restored");
        }

        #endregion


        #region Importing & Exporting

        /// <summary>
        /// Create a serializable character sheet for this character.
        /// </summary>
        /// <returns></returns>
        public override SerializableSettingsCharacter Export()
        {
            SerializableCCPCharacter serial = new SerializableCCPCharacter();
            Export(serial);

            // Skill queue
            serial.SkillQueue.AddRange(SkillQueue.Export());

            // Market orders
            serial.MarketOrders.AddRange(MarketOrdersExport());

            // Contracts
            serial.Contracts.AddRange(ContractsExport());
            
            // Industry jobs
            serial.IndustryJobs.AddRange(IndustryJobsExport());

            // Eve mail messages IDs
            serial.EveMailMessagesIDs = EVEMailMessages.Export();

            // Eve notifications IDs
            serial.EveNotificationsIDs = EVENotifications.Export();

            // Last API updates
            if (QueryMonitors.Any())
            {
                m_lastAPIUpdates = QueryMonitors.Select(
                    monitor => new SerializableAPIUpdate
                                   {
                                       Method = monitor.Method.ToString(),
                                       Time = monitor.LastUpdate
                                   }).ToList();
            }

            serial.LastUpdates.AddRange(m_lastAPIUpdates);

            return serial;
        }

        /// <summary>
        /// Exports the market orders.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SerializableOrderBase> MarketOrdersExport()
        {
            // Until we can determine what data the character's API keys can query,
            // we have to keep the data unprocessed. Once we know, we filter them

            IEnumerable<SerializableOrderBase> corporationMarketOrdersExport =
                m_services.AnyESIKeyUnprocessed() || m_corporationDataQuerying != null
                    ? CorporationMarketOrders.ExportOnlyIssuedByCharacter()
                    : new List<SerializableOrderBase>();

            IEnumerable<SerializableOrderBase> characterMarketOrdersExport =
                m_services.AnyESIKeyUnprocessed() || m_characterDataQuerying != null
                    ? CharacterMarketOrders.Export()
                    : new List<SerializableOrderBase>();

            return characterMarketOrdersExport.Concat(corporationMarketOrdersExport);
        }

        /// <summary>
        /// Exports the contracts.
        /// </summary>
        /// <returns></returns>
        /// <remarks>Excludes contracts that appear in both collections.</remarks>
        private IEnumerable<SerializableContract> ContractsExport()
        {
            // Until we can determine what data the character's API keys can query,
            // we have to keep the data unprocessed. Once we know, we filter them

            IEnumerable<SerializableContract> corporationContractsExport =
                m_services.AnyESIKeyUnprocessed() || m_corporationDataQuerying != null
                    ? CorporationContracts.ExportOnlyIssuedByCharacter()
                    : new List<SerializableContract>();

            IEnumerable<SerializableContract> characterContractsExport =
                m_services.AnyESIKeyUnprocessed() || m_characterDataQuerying != null
                    ? CharacterContracts.Export().Where(charContract => corporationContractsExport.All(
                        corpContract => corpContract.ContractID != charContract.ContractID))
                    : new List<SerializableContract>();

            return characterContractsExport.Concat(corporationContractsExport);
        }

        /// <summary>
        /// Exports the industry jobs.
        /// </summary>
        /// <returns></returns>
        private IEnumerable<SerializableJob> IndustryJobsExport()
        {
            // Until we can determine what data the character's API keys can query,
            // we have to keep the data unprocessed. Once we know, we filter them

            IEnumerable<SerializableJob> corporationIndustryJobsExport =
                m_services.AnyESIKeyUnprocessed() || m_corporationDataQuerying != null
                    ? CorporationIndustryJobs.ExportOnlyIssuedByCharacter()
                    : new List<SerializableJob>();

            IEnumerable<SerializableJob> characterIndustryJobsExport =
                m_services.AnyESIKeyUnprocessed() || m_characterDataQuerying != null
                    ? CharacterIndustryJobs.Export()
                    : new List<SerializableJob>();

            return characterIndustryJobsExport.Concat(corporationIndustryJobsExport);
        }

        /// <summary>
        /// Imports data from a serialization object.
        /// </summary>
        /// <param name="serial"></param>
        private void Import(SerializableCCPCharacter serial)
        {
            Import((SerializableSettingsCharacter)serial);

            // Skill queue
            SkillQueue.Import(serial.SkillQueue);

            // Market orders
            MarketOrdersImport(serial.MarketOrders);

            // Contracts
            ContractsImport(serial.Contracts);
            
            // Industry jobs
            IndustryJobsImport(serial.IndustryJobs);

            // EVE mail messages IDs
            EVEMailMessages.Import(serial.EveMailMessagesIDs);

            // EVE notifications IDs
            EVENotifications.Import(serial.EveNotificationsIDs);

            // Kill Logs
            KillLog.ImportFromCacheFile();

            // Fire the global event
            m_services.OnCharacterUpdated(this);
        }

        /// <summary>
        /// Imports the market orders.
        /// </summary>
        /// <param name="marketOrders">The market orders.</param>
        private void MarketOrdersImport(IList<SerializableOrderBase> marketOrders)
        {
            CharacterMarketOrders.Import(marketOrders.Where(order => order.IssuedFor == IssuedFor.Character));
            CorporationMarketOrders.Import(marketOrders.Where(order => order.IssuedFor == IssuedFor.Corporation));
        }

        /// <summary>
        /// Imports the contracts.
        /// </summary>
        /// <param name="contracts">The contracts.</param>
        private void ContractsImport(IList<SerializableContract> contracts)
        {
            CharacterContracts.Import(contracts.Where(contract => contract.IssuedFor == IssuedFor.Character));
            CorporationContracts.Import(contracts.Where(contract => contract.IssuedFor == IssuedFor.Corporation));
        }

        /// <summary>
        /// Imports the industry jobs.
        /// </summary>
        /// <param name="industryJobs">The industry jobs.</param>
        private void IndustryJobsImport(IList<SerializableJob> industryJobs)
        {
            CharacterIndustryJobs.Import(industryJobs.Where(job => job.IssuedFor == IssuedFor.Character));
            CorporationIndustryJobs.Import(industryJobs.Where(job => job.IssuedFor == IssuedFor.Corporation));
        }

        #endregion


        #region Inherited Events

        /// <summary>
        /// Called when the object gets disposed.
        /// </summary>
        internal override void Dispose()
        {
            // Unsubscribe remaining global events (character-specific events no longer subscribed)
            m_services.ESIKeyInfoUpdated -= EveLensClient_ESIKeyInfoUpdated;
            m_services.EveIDToNameUpdated -= EveLensClient_EveIDToNameUpdated;
            m_services.FiveSecondTick -= EveLensClient_TimerTick;

            // Unsubscribe events
            SkillQueue.Dispose();
            if (_characterIndustryJobs.IsValueCreated)
                CharacterIndustryJobs.Dispose();
            if (_corporationIndustryJobs.IsValueCreated)
                CorporationIndustryJobs.Dispose();
            if (_planetaryColonies.IsValueCreated)
                PlanetaryColonies.Dispose();

            // Unsubscribe events
            if (m_characterDataQuerying != null)
            {
                m_characterDataQuerying.Dispose();
                m_characterDataQuerying = null;
            }

            if (m_corporationDataQuerying != null)
            {
                m_corporationDataQuerying.Dispose();
                m_corporationDataQuerying = null;
            }
        }

        #endregion


        #region Helper Methods

        /// <summary>
        /// Checks whether we should notify an error.
        /// Transient errors (5xx, timeouts, rate limits) are suppressed until they persist
        /// for 3 consecutive poll cycles, preventing activity log noise from ESI hiccups.
        /// Auth failures (401/403) and not-found (404) are shown immediately since they
        /// require user action.
        /// </summary>
        internal bool ShouldNotifyError(IAPIResult result, Enum method)
        {
            int methodKey = method.GetHashCode();

            if (result.HasError)
            {
                // Auth failures and not-found always surface immediately — user must act
                if (result.ErrorCode == 401 || result.ErrorCode == 403 || result.ErrorCode == 404)
                {
                    m_consecutiveFailures.Remove(methodKey);
                    if (!m_errorNotifiedMethod.Equals(CCPAPIMethodsEnum.None))
                        return false;
                    m_errorNotifiedMethod = method;
                    return true;
                }

                // Transient errors: only notify after 3 consecutive failures
                int count = m_consecutiveFailures.GetValueOrDefault(methodKey, 0) + 1;
                m_consecutiveFailures[methodKey] = count;

                if (count < 3)
                    return false;

                if (!m_errorNotifiedMethod.Equals(CCPAPIMethodsEnum.None))
                    return false;
                m_errorNotifiedMethod = method;
                return true;
            }

            // Success: reset consecutive failure counter for this method
            m_consecutiveFailures.Remove(methodKey);

            // Removes the previous error notification
            if (!m_errorNotifiedMethod.Equals(method))
                return false;
            m_services.Notifications?.InvalidateCharacterAPIError(this);
            m_errorNotifiedMethod = CCPAPIMethodsEnum.None;
            return false;
        }

        /// <summary>
        /// Notifies for market orders related events.
        /// </summary>
        private void NotifyForMarketOrdersRelatedEvents()
        {
            // Notify for ended orders
            NotifyEndedOrders();

            // Notify for insufficient balance
            NotifyInsufficientBalance();

            // Reset helper lists
            m_endedOrdersForCharacter.Clear();
            m_endedOrdersForCorporation.Clear();

            // Fires the event regarding market orders update
            m_services.OnMarketOrdersUpdated(this);
        }

        /// <summary>
        /// Notifies for ended orders.
        /// </summary>
        private void NotifyEndedOrders()
        {
            // Notify ended orders issued by the character
            if (m_endedOrdersForCharacter.Any())
                m_services.Notifications?.NotifyCharacterMarketOrdersEnded(this, m_endedOrdersForCharacter);

            // Uncomment upon implementing an exclusive corporation monitor
            // Notify ended orders issued for the corporation
            //if (m_endedOrdersForCorporation.Any())
            //    m_services.Notifications?.NotifyCorporationMarketOrdersEnded(Corporation, m_endedOrdersForCorporation);
        }

        /// <summary>
        /// Notifies for contracts related events.
        /// </summary>
        private void NotifyForContractsRelatedEvents()
        {
            // Notify for ended contracts
            NotifyEndedContracts();

            // Notify for assigned contracts
            NotifyAssignedContracts();

            // Reset helper lists
            // Note: Special condition logic is applied due to the fact that CCP
            // includes corporation related contracts in character API feed
            if (m_characterDataQuerying != null && m_corporationDataQuerying != null &&
                m_corporationDataQuerying.CorporationContractsQueried)
            {
                m_endedContractsForCharacter.Clear();
            }

            if (m_corporationDataQuerying != null && m_characterDataQuerying != null &&
                m_characterDataQuerying.CharacterContractsQueried)
            {
                m_endedContractsForCorporation.Clear();
            }

            // Fires the event regarding contracts update
            m_services.OnContractsUpdated(this);
        }

        /// <summary>
        /// Notifies for ended contracts.
        /// </summary>
        private void NotifyEndedContracts()
        {
            // Notify ended contracts issued by the character
            if (m_endedContractsForCharacter.Any(x => !x.NotificationSend))
            {
                m_services.Notifications?.NotifyCharacterContractsEnded(this, m_endedContractsForCharacter);
                m_endedContractsForCharacter.ForEach(x => x.NotificationSend = true);
            }

            // Uncomment upon implementing an exclusive corporation monitor
            // Notify ended contracts issued for the corporation
            //if (m_endedContractsForCorporation.All(x => x.NotificationSend))
            //    return;

            //m_services.Notifications?.NotifyCorporationContractsEnded(Corporation, m_endedContractsForCorporation);
            //m_endedContractsForCorporation.ForEach(x => x.NotificationSend = true);
        }

        /// <summary>
        /// Notifies for assigned contracts.
        /// </summary>
        private void NotifyAssignedContracts()
        {
            if (Contracts.Any(contract => contract.State == ContractState.Assigned))
            {
                int assignedContracts = Contracts.Count(contracts => contracts.State == ContractState.Assigned);
                m_services.Notifications?.NotifyCharacterContractsAssigned(this, assignedContracts);
                return;
            }

            m_services.Notifications?.InvalidateCharacterContractsAssigned(this);
        }

        /// <summary>
        /// Notifies for insufficient balance.
        /// </summary>
        internal void NotifyInsufficientBalance()
        {
            // Check the character has sufficient balance
            // for its buying orders and send a notification if not
            if (!HasSufficientBalance)
            {
                m_services.Notifications?.NotifyInsufficientBalance(this);
                return;
            }

            m_services.Notifications?.InvalidateInsufficientBalance(this);
        }

        /// <summary>
        /// Notifies for industry jobs related events.
        /// </summary>
        private void NotifyForIndustryJobsRelatedEvents()
        {
            // Fires the event regarding industry jobs update
            m_services.OnIndustryJobsUpdated(this);
        }

        /// <summary>
        /// Clears in-memory collections and on-disk cache files for scopes that have been
        /// revoked during re-authentication. Resets lazy fields to fresh empty instances
        /// so the UI no longer displays stale data.
        /// </summary>
        /// <param name="revokedScopes">The ESI scopes that were removed.</param>
        internal void ClearRevokedScopeData(IEnumerable<string> revokedScopes)
        {
            var cache = AppServices.CharacterDataCache;
            long id = CharacterID;

            foreach (string scope in revokedScopes)
            {
                // Delete cache files for this scope
                foreach (string cacheKey in EsiScopeMapping.GetCacheKeysForScope(scope))
                {
                    _ = cache.ClearEndpointAsync(id, cacheKey);
                }

                // Reset in-memory lazy collections to empty instances
                switch (scope)
                {
                    case "esi-wallet.read_character_wallet.v1":
                        _walletJournal = new Lazy<WalletJournalCollection>(() => new WalletJournalCollection(this));
                        _walletTransactions = new Lazy<WalletTransactionsCollection>(() => new WalletTransactionsCollection(this));
                        break;
                    case "esi-assets.read_assets.v1":
                        _assets = new Lazy<AssetCollection>(() => new AssetCollection(this));
                        break;
                    case "esi-markets.read_character_orders.v1":
                        _characterMarketOrders = new Lazy<MarketOrderCollection>(() => new MarketOrderCollection(this));
                        break;
                    case "esi-contracts.read_character_contracts.v1":
                        _characterContracts = new Lazy<ContractCollection>(() => new ContractCollection(this));
                        break;
                    case "esi-industry.read_character_jobs.v1":
                        _characterIndustryJobs = new Lazy<IndustryJobCollection>(() => new IndustryJobCollection(this));
                        break;
                    case "esi-mail.read_mail.v1":
                        _eveMailMessages = new Lazy<EveMailMessageCollection>(() => new EveMailMessageCollection(this));
                        _eveMailingLists = new Lazy<EveMailingListCollection>(() => new EveMailingListCollection(this));
                        break;
                    case "esi-characters.read_notifications.v1":
                        _eveNotifications = new Lazy<EveNotificationCollection>(() => new EveNotificationCollection(this));
                        break;
                    case "esi-characters.read_contacts.v1":
                        _contacts = new Lazy<ContactCollection>(() => new ContactCollection(this));
                        break;
                    case "esi-characters.read_standings.v1":
                        _standings = new Lazy<StandingCollection>(() => new StandingCollection(this));
                        break;
                    case "esi-characters.read_medals.v1":
                        _characterMedals = new Lazy<MedalCollection>(() => new MedalCollection(this));
                        break;
                    case "esi-characters.read_fw_stats.v1":
                        FactionalWarfareStats = null;
                        IsFactionalWarfareNotEnlisted = true;
                        break;
                    case "esi-characters.read_agents_research.v1":
                        _researchPoints = new Lazy<ResearchPointCollection>(() => new ResearchPointCollection(this));
                        break;
                    case "esi-killmails.read_killmails.v1":
                        _killLog = new Lazy<KillLogCollection>(() => new KillLogCollection(this));
                        break;
                    case "esi-calendar.read_calendar_events.v1":
                        _upcomingCalendarEvents = new Lazy<UpcomingCalendarEventCollection>(() => new UpcomingCalendarEventCollection(this));
                        break;
                    case "esi-planets.manage_planets.v1":
                        _planetaryColonies = new Lazy<PlanetaryColonyCollection>(() => new PlanetaryColonyCollection(this));
                        break;
                    case "esi-characters.read_loyalty.v1":
                        _loyaltyPoints = new Lazy<LoyaltyCollection>(() => new LoyaltyCollection(this));
                        break;
                }
            }

            // Notify UI so tabs refresh with cleared data
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(this));
        }

        /// <summary>
        /// Resets the last API updates.
        /// </summary>
        /// <param name="lastUpdates">The last updates.</param>
        private void ResetLastAPIUpdates(IEnumerable<SerializableAPIUpdate> lastUpdates)
        {
            foreach (SerializableAPIUpdate lastUpdate in lastUpdates)
            {
                Enum? method = ESIMethods.Methods.FirstOrDefault(apiMethod => apiMethod.ToString() == lastUpdate.Method);
                if (method == null)
                    continue;

                IQueryMonitorEx? monitor = QueryMonitors[method] as IQueryMonitorEx;
                monitor?.Reset(lastUpdate.Time);
            }
        }

        #endregion


        #region Global Events

        /// <summary>
        /// Handles the TimerTick event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_TimerTick(object? sender, EventArgs e)
        {
            // Force update a monitor if the last update exceed the current datetime
            foreach (var monitor in QueryMonitors.Where(monitor => !monitor.IsUpdating &&
                monitor.LastUpdate > DateTime.UtcNow))
            {
                (monitor as IQueryMonitorEx)?.ForceUpdate(true);
            }
        }

        /// <summary>
        /// Handles the ESIKeyInfoUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_ESIKeyInfoUpdated(object? sender, EventArgs e)
        {
            if (m_services.AnyESIKeyUnprocessed())
                return;

            if (!Identity.ESIKeys.Any())
                return;

            if (m_characterDataQuerying == null && Identity.ESIKeys.Any())
            {
                m_characterDataQuerying = new Services.CharacterQueryOrchestrator(this,
                    AppServices.LoggerFactory?.CreateLogger<Services.CharacterQueryOrchestrator>());
                ResetLastAPIUpdates(m_lastAPIUpdates.Where(lastUpdate => Enum.IsDefined(
                    typeof(ESIAPICharacterMethods), lastUpdate.Method)));
            }

            if (m_corporationDataQuerying == null && Identity.ESIKeys.Any())
            {
                m_corporationDataQuerying = new Services.CorporationQueryOrchestrator(this);
                ResetLastAPIUpdates(m_lastAPIUpdates.Where(lastUpdate => Enum.IsDefined(
                    typeof(ESIAPICorporationMethods), lastUpdate.Method)));
            }
        }

        /// <summary>
        /// Handles the CharacterAssetsUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.CharacterChangedEventArgs"/> instance containing the event data.</param>
        /// <remarks>Triggering a settings exportation to update the character owned skillbooks found in Assets</remarks>
        /// <summary>
        /// Called directly by EveLensClient when this character's assets are updated.
        /// </summary>
        internal void OnAssetsUpdated()
        {
            Export();
        }

        /// <summary>
        /// Handles the CharacterMarketOrdersUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.MarketOrdersEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's market orders are updated.
        /// </summary>
        internal void OnCharacterMarketOrdersUpdated(IEnumerable<MarketOrder> endedOrders)
        {
            m_endedOrdersForCharacter.AddRange(endedOrders);

            if (m_corporationDataQuerying?.CorporationMarketOrdersQueried ?? true)
                NotifyForMarketOrdersRelatedEvents();
        }

        /// <summary>
        /// Handles the CorporationMarketOrdersUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.MarketOrdersEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's corporation market orders are updated.
        /// </summary>
        internal void OnCorporationMarketOrdersUpdated(IEnumerable<MarketOrder> endedOrders)
        {
            m_endedOrdersForCorporation.AddRange(endedOrders);
            m_endedOrdersForCharacter.AddRange(endedOrders.Where(order => order.OwnerID == CharacterID));

            if (m_characterDataQuerying?.CharacterMarketOrdersQueried ?? true)
                NotifyForMarketOrdersRelatedEvents();
        }

        /// <summary>
        /// Handles the CharacterContractsUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.ContractsEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's contracts are updated.
        /// </summary>
        internal void OnCharacterContractsUpdated(IEnumerable<Contract> endedContracts)
        {
            m_endedContractsForCharacter.AddRange(endedContracts.Where(
                charEndedContract => m_endedContractsForCorporation.All(
                corpEndedContract => corpEndedContract.ID != charEndedContract.ID)));

            if (m_corporationDataQuerying?.CorporationContractsQueried ?? true)
                NotifyForContractsRelatedEvents();
        }

        /// <summary>
        /// Handles the CorporationContractsUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.ContractsEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's corporation contracts are updated.
        /// </summary>
        internal void OnCorporationContractsUpdated(IEnumerable<Contract> endedContracts)
        {
            m_endedContractsForCorporation.AddRange(endedContracts);
            m_endedContractsForCharacter.AddRange(endedContracts.Where(contract =>
                contract.IssuerID == CharacterID).Where(corpEndedContract =>
                m_endedContractsForCharacter.All(charEndedContract => charEndedContract.
                ID != corpEndedContract.ID)));

            if (m_characterDataQuerying?.CharacterContractsQueried ?? true)
                NotifyForContractsRelatedEvents();
        }

        /// <summary>
        /// Handles the CharacterIndustryJobsUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.CharacterChangedEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's industry jobs are updated.
        /// </summary>
        internal void OnCharacterIndustryJobsUpdated()
        {
            if (m_corporationDataQuerying?.CorporationIndustryJobsQueried ?? true)
                NotifyForIndustryJobsRelatedEvents();
        }

        /// <summary>
        /// Handles the CorporationIndustryJobsUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.CharacterChangedEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's corporation industry jobs are updated.
        /// </summary>
        internal void OnCorporationIndustryJobsUpdated()
        {
            if (m_characterDataQuerying?.CharacterIndustryJobsQueried ?? true)
                NotifyForIndustryJobsRelatedEvents();
        }

        /// <summary>
        /// Handles the CharacterIndustryJobsCompleted event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.IndustryJobsEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's industry jobs complete.
        /// </summary>
        internal void OnCharacterIndustryJobsCompleted(IEnumerable<IndustryJob> completedJobs)
        {
            m_jobsCompletedForCharacter.AddRange(completedJobs);

            // If character has completed corporation issued jobs, wait until those are gathered too
            if (!CorporationIndustryJobs.Any(job => job.ActiveJobState ==
                    ActiveJobState.Ready && !job.NotificationSend))
            {
                m_services.Notifications?.NotifyCharacterIndustryJobCompletion(this,
                    m_jobsCompletedForCharacter);

                // Now that we have send the notification clear the list
                m_jobsCompletedForCharacter.Clear();
            }
        }

        /// <summary>
        /// Handles the CorporationIndustryJobsCompleted event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EveLens.Common.CustomEventArgs.IndustryJobsEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's corporation industry jobs complete.
        /// </summary>
        internal void OnCorporationIndustryJobsCompleted(IEnumerable<IndustryJob> completedJobs)
        {
            // Uncomment upon implementing an exclusive corporation monitor
            // m_services.Notifications?.NotifyCorporationIndustryJobCompletion(Corporation, completedJobs);
        }

        /// <summary>
        /// Handles the CharacterPlanetaryPinsCompleted event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="PlanetaryPinsEventArgs"/> instance containing the event data.</param>
        /// <summary>
        /// Called directly by EveLensClient when this character's planetary pins complete.
        /// </summary>
        internal void OnPlanetaryPinsCompleted(IEnumerable<PlanetaryPin> completedPins)
        {
            m_services.Notifications?.NotifyCharacterPlanetaryPinCompleted(this, completedPins);
        }

        /// <summary>
        /// Handles the EveIDToNameUpdated event of the EveLensClient control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
        private void EveLensClient_EveIDToNameUpdated(object? sender, EventArgs e)
        {
            bool updated = false;
            string cname = CorporationName, aname = AllianceName, fname = FactionName;

            // If the corp, alliance, or faction was unknown, update it
            if (cname.IsEmptyOrUnknown())
            {
                CorporationName = ServiceLocator.NameResolver.GetName(CorporationID);
                if (CorporationName != cname)
                    updated = true;
            }
            if (aname.IsEmptyOrUnknown())
            {
                AllianceName = ServiceLocator.NameResolver.GetName(AllianceID);
                if (AllianceName != aname)
                    updated = true;
            }
            if (fname.IsEmptyOrUnknown())
            {
                FactionName = ServiceLocator.NameResolver.GetName(FactionID);
                if (FactionName != fname)
                    updated = true;
            }

            // Only fire update if the new names changed
            if (updated)
                m_services.OnCharacterInfoUpdated(this);
        }

        #endregion
    }
}
