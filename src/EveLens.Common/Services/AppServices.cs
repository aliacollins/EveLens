// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Collections.Global;
using EveLens.Common.Enumerations;
using EveLens.Common.Interfaces;
using EveLens.Common.Logging;
using EveLens.Common.Models;
using EveLens.Common.Scheduling;
using EveLens.Core.Interfaces;
using EveLens.Infrastructure.Scheduling.Health;
using Microsoft.Extensions.Logging;

// Aliases for new platform-agnostic service interfaces
using IDialogService = EveLens.Core.Interfaces.IDialogService;
using IClipboardService = EveLens.Core.Interfaces.IClipboardService;
using IApplicationLifecycle = EveLens.Core.Interfaces.IApplicationLifecycle;
using IScreenInfo = EveLens.Core.Interfaces.IScreenInfo;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Central access point for all service interfaces (Strangler Fig facade).
    /// New code should depend on interfaces via this class rather than on static classes directly.
    /// </summary>
    /// <remarks>
    /// This is a transitional pattern. When DI is introduced (Tier 3), these services
    /// will be registered in the container and this static accessor will be deprecated.
    /// </remarks>
    public static class AppServices
    {
        private static Lazy<IDispatcher> s_dispatcher = new(() => new DispatcherService());
        private static Lazy<ISettingsProvider> s_settings = new(() => new SettingsProviderService());
        private static Lazy<IEsiClient> s_esiClient = new(() => new EsiClientService());
        private static Lazy<IEventAggregator> s_eventAggregator = new(() =>
            new EventAggregator(LoggerFactory.CreateLogger<EventAggregator>()));
        private static Lazy<ICharacterRepository> s_characterRepository = new(() => new CharacterRepositoryService());
        private static Lazy<ISettingsDataStore> s_dataStore = new(() => EveLensClientDataStore.Instance);
        private static Lazy<CharacterFactory> s_characterFactory = new(() => new CharacterFactory(
            CharacterRepository, EventAggregator, EveLensClientCharacterServices.Instance));
        private static Lazy<ITraceService> s_traceService = new(() => new TraceService());
        private static Lazy<IApplicationPaths> s_applicationPaths = new(() => new ApplicationPathsAdapter());
        private static Lazy<INameResolver> s_nameResolver = new(() => new NameResolverAdapter());
        private static Lazy<IStationResolver> s_stationResolver = new(() => new StationResolverAdapter());
        private static Lazy<IFlagResolver> s_flagResolver = new(() => new FlagResolverAdapter());
        private static Lazy<Core.Interfaces.IImageService> s_imageService = new(() => new ImageServiceAdapter());
        private static Lazy<INotificationTypeResolver> s_notificationTypeResolver = new(() => new NotificationTypeResolverAdapter());
        private static Lazy<IResourceProvider> s_resourceProvider = new(() => new ResourceProviderAdapter());
        private static Lazy<GlobalNotificationCollection> s_notifications = new(() => EveLensClient.Notifications);
        private static Lazy<GlobalCharacterCollection> s_characters = new(() => EveLensClient.Characters);
        private static Lazy<GlobalAPIKeyCollection> s_esiKeys = new(() => EveLensClient.ESIKeys);
        private static Lazy<GlobalAPIProviderCollection> s_apiProviders = new(() => EveLensClient.APIProviders);
        private static Lazy<GlobalCharacterIdentityCollection> s_characterIdentities = new(() => EveLensClient.CharacterIdentities);
        private static Lazy<GlobalMonitoredCharacterCollection> s_monitoredCharacters = new(() => EveLensClient.MonitoredCharacters);
        private static Lazy<EveServer> s_eveServer = new(() => EveLensClient.EVEServer);
        private static Lazy<ILoggerFactory> s_loggerFactory = new(() => CreateLoggerFactory());
        private static Lazy<ActiveCharacterTierSubscriber> s_tierSubscriber = new(() => new ActiveCharacterTierSubscriber());
        private static Lazy<IEsiScheduler> s_esiScheduler = new(() => new EsiScheduler(
            Dispatcher, EventAggregator, EsiClient,
            LoggerFactory?.CreateLogger<EsiScheduler>()));
        private static Lazy<IDialogService> s_dialogService = new(() => new NullDialogService());
        private static Lazy<IClipboardService> s_clipboardService = new(() => new NullClipboardService());
        private static Lazy<IApplicationLifecycle> s_applicationLifecycle = new(() => new NullApplicationLifecycle());
        private static Lazy<IScreenInfo> s_screenInfo = new(() => new NullScreenInfo());
        private static Lazy<ActivityLogService> s_activityLog = new(() =>
            new ActivityLogService(ApplicationPaths.DataDirectory));
        private static Lazy<ICharacterDataCache> s_characterDataCache = new(() => new CharacterDataCacheService());
        private static Lazy<EndpointHealthTracker> s_healthTracker = new(() =>
        {
            var tracker = new EndpointHealthTracker(EventAggregator, Dispatcher);
            if (s_esiScheduler.IsValueCreated && s_esiScheduler.Value is EsiScheduler scheduler)
                scheduler.SetHealthTracker(tracker);
            return tracker;
        });
        private static Lazy<HealthNotificationSubscriber> s_healthNotifySub = new(() =>
            new HealthNotificationSubscriber(EventAggregator));
        private static PrivacyCategories s_privacyMask;

        /// <summary>
        /// Gets the notification collection.
        /// </summary>
        public static GlobalNotificationCollection Notifications => s_notifications.Value;

        /// <summary>
        /// Gets the character collection.
        /// </summary>
        public static GlobalCharacterCollection Characters => s_characters.Value;

        /// <summary>
        /// Gets the ESI key collection.
        /// </summary>
        public static GlobalAPIKeyCollection ESIKeys => s_esiKeys.Value;

        /// <summary>
        /// Gets the API provider collection.
        /// </summary>
        public static GlobalAPIProviderCollection APIProviders => s_apiProviders.Value;

        /// <summary>
        /// Gets the character identity collection.
        /// </summary>
        public static GlobalCharacterIdentityCollection CharacterIdentities => s_characterIdentities.Value;

        /// <summary>
        /// Gets the monitored character collection.
        /// </summary>
        public static GlobalMonitoredCharacterCollection MonitoredCharacters => s_monitoredCharacters.Value;

        /// <summary>
        /// Gets the EVE server status.
        /// </summary>
        public static EveServer EVEServer => s_eveServer.Value;

        /// <summary>
        /// Gets the tier subscriber that bridges tab selection to query tier activation.
        /// Accessing this property initializes the subscriber (lazy).
        /// </summary>
        internal static ActiveCharacterTierSubscriber TierSubscriber => s_tierSubscriber.Value;

        /// <summary>
        /// Gets the ESI scheduler for priority-based character data fetching.
        /// Replaces SmartQueryScheduler with cache-expiry-driven scheduling.
        /// </summary>
        public static IEsiScheduler EsiScheduler => s_esiScheduler.Value;

        /// <summary>
        /// Gets the ESI endpoint health tracker. Tracks per-(character, endpoint) health
        /// using a state machine with rolling time windows. Replaces event-based error notifications
        /// for scheduler-driven endpoints.
        /// </summary>
        public static EndpointHealthTracker HealthTracker => s_healthTracker.Value;

        /// <summary>
        /// Gets the logger factory for creating structured loggers (MEL).
        /// Provides TCP JSON-lines streaming and System.Diagnostics.Trace bridging.
        /// </summary>
        public static ILoggerFactory LoggerFactory => s_loggerFactory.Value;

        /// <summary>
        /// Gets the dialog service for platform-agnostic message boxes and file dialogs.
        /// </summary>
        public static IDialogService DialogService => s_dialogService.Value;

        /// <summary>
        /// Gets the clipboard service for platform-agnostic clipboard access.
        /// </summary>
        public static IClipboardService ClipboardService => s_clipboardService.Value;

        /// <summary>
        /// Gets the application lifecycle service for exit/restart.
        /// </summary>
        public static IApplicationLifecycle ApplicationLifecycle => s_applicationLifecycle.Value;

        /// <summary>
        /// Gets the screen information service for display geometry.
        /// </summary>
        public static IScreenInfo ScreenInfo => s_screenInfo.Value;

        /// <summary>
        /// Gets the activity log service for persisting notification history.
        /// </summary>
        public static ActivityLogService ActivityLog => s_activityLog.Value;

        /// <summary>
        /// Gets the character data cache for persisting live ESI data to disk.
        /// </summary>
        public static ICharacterDataCache CharacterDataCache => s_characterDataCache.Value;

        /// <summary>
        /// Gets the current privacy mask (which categories are hidden).
        /// </summary>
        public static PrivacyCategories PrivacyMask => s_privacyMask;

        /// <summary>
        /// Gets whether any privacy category is active (for icon state, backward compat).
        /// </summary>
        public static bool PrivacyModeEnabled => s_privacyMask != PrivacyCategories.None;

        /// <summary>
        /// Returns true if the given category is currently hidden.
        /// </summary>
        public static bool IsPrivate(PrivacyCategories category) => (s_privacyMask & category) != 0;

        /// <summary>
        /// XORs a single category on/off and publishes <see cref="Events.PrivacyModeChangedEvent"/>.
        /// </summary>
        public static void TogglePrivacyCategory(PrivacyCategories category)
        {
            s_privacyMask ^= category;
            EventAggregator?.Publish(new Events.PrivacyModeChangedEvent(PrivacyModeEnabled));
        }

        /// <summary>
        /// All-or-none toggle: if any category is set, clears all; otherwise sets all.
        /// </summary>
        public static void TogglePrivacyMode()
        {
            s_privacyMask = s_privacyMask != PrivacyCategories.None
                ? PrivacyCategories.None
                : PrivacyCategories.All;
            EventAggregator?.Publish(new Events.PrivacyModeChangedEvent(PrivacyModeEnabled));
        }

        /// <summary>
        /// Gets whether the application is closed.
        /// </summary>
        public static bool Closed => EveLensClient.Closed;

        /// <summary>
        /// Gets whether this is a debug build.
        /// </summary>
        public static bool IsDebugBuild => EveLensClient.IsDebugBuild;

        /// <summary>
        /// Gets whether the static data has been loaded.
        /// </summary>
        public static bool IsDataLoaded => EveLensClient.IsDataLoaded;

        /// <summary>
        /// Sets whether the static data has been loaded.
        /// </summary>
        public static void SetDataLoaded(bool value) => EveLensClient.IsDataLoaded = value;

        /// <summary>
        /// Gets whether this is a pre-release version.
        /// </summary>
        public static bool IsPreReleaseVersion => EveLensClient.IsPreReleaseVersion;

        /// <summary>
        /// Gets whether this is an alpha version.
        /// </summary>
        public static bool IsAlphaVersion => EveLensClient.IsAlphaVersion;

        /// <summary>
        /// Gets whether this is a beta version.
        /// </summary>
        public static bool IsBetaVersion => EveLensClient.IsBetaVersion;

        /// <summary>
        /// Gets the file version information for the application.
        /// </summary>
        public static System.Diagnostics.FileVersionInfo FileVersionInfo => EveLensClient.FileVersionInfo;

        /// <summary>
        /// Gets the product name with version string.
        /// </summary>
        public static string ProductNameWithVersion => EveLensClient.ProductNameWithVersion;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string VersionString => EveLensClient.VersionString;

        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        public static string SettingsFileName => EveLensClient.SettingsFileName;

        /// <summary>
        /// Gets the global datafile collection used during data loading.
        /// </summary>
        public static GlobalDatafileCollection Datafiles => EveLensClient.Datafiles;

        /// <summary>
        /// Gets a value indicating whether cache folder in EVE default location exist.
        /// </summary>
        public static bool EveAppDataFoldersExistInDefaultLocation => EveLensClient.EveAppDataFoldersExistInDefaultLocation;

        /// <summary>
        /// Clears all cached data (settings, XML cache, images).
        /// </summary>
        public static void ClearCache() => EveLensClient.ClearCache();

        /// <summary>
        /// Gets the dispatcher service for UI thread marshaling.
        /// </summary>
        public static IDispatcher Dispatcher => s_dispatcher.Value;

        /// <summary>
        /// Gets the settings provider service.
        /// </summary>
        public static ISettingsProvider Settings => s_settings.Value;

        /// <summary>
        /// Gets the ESI client service for API rate limiting.
        /// </summary>
        public static IEsiClient EsiClient => s_esiClient.Value;

        /// <summary>
        /// Gets the event aggregator for publish/subscribe messaging.
        /// </summary>
        public static IEventAggregator EventAggregator => s_eventAggregator.Value;

        /// <summary>
        /// Gets the character repository service.
        /// </summary>
        public static ICharacterRepository CharacterRepository => s_characterRepository.Value;

        /// <summary>
        /// Gets the data store service for settings import/export.
        /// </summary>
        public static ISettingsDataStore DataStore => s_dataStore.Value;

        /// <summary>
        /// Gets the character factory for creating CCPCharacter instances.
        /// </summary>
        internal static CharacterFactory CharacterFactory => s_characterFactory.Value;

        /// <summary>
        /// Gets the trace service for diagnostic logging.
        /// </summary>
        public static ITraceService TraceService => s_traceService.Value;

        /// <summary>
        /// Gets the application paths service.
        /// </summary>
        public static IApplicationPaths ApplicationPaths => s_applicationPaths.Value;

        /// <summary>
        /// Gets the name resolver for EVE entity ID to name lookups.
        /// </summary>
        public static INameResolver NameResolver => s_nameResolver.Value;

        /// <summary>
        /// Gets the station resolver for station/structure lookups.
        /// </summary>
        public static IStationResolver StationResolver => s_stationResolver.Value;

        /// <summary>
        /// Gets the flag resolver for inventory flag lookups.
        /// </summary>
        public static IFlagResolver FlagResolver => s_flagResolver.Value;

        /// <summary>
        /// Gets the image service for image downloading and caching.
        /// </summary>
        public static Core.Interfaces.IImageService ImageService => s_imageService.Value;

        /// <summary>
        /// Gets the notification type resolver.
        /// </summary>
        public static INotificationTypeResolver NotificationTypeResolver => s_notificationTypeResolver.Value;

        /// <summary>
        /// Gets the resource provider for embedded resources (XSLT, static data).
        /// </summary>
        public static IResourceProvider ResourceProvider => s_resourceProvider.Value;

        /// <summary>
        /// Replaces a service implementation (for testing or DI transition).
        /// </summary>
        internal static void SetDispatcher(IDispatcher dispatcher) => s_dispatcher = new Lazy<IDispatcher>(() => dispatcher);
        internal static void SetSettings(ISettingsProvider settings) => s_settings = new Lazy<ISettingsProvider>(() => settings);
        internal static void SetEsiClient(IEsiClient esiClient) => s_esiClient = new Lazy<IEsiClient>(() => esiClient);
        internal static void SetEventAggregator(IEventAggregator aggregator) => s_eventAggregator = new Lazy<IEventAggregator>(() => aggregator);
        internal static void SetCharacterRepository(ICharacterRepository repo) => s_characterRepository = new Lazy<ICharacterRepository>(() => repo);
        internal static void SetDataStore(ISettingsDataStore store) => s_dataStore = new Lazy<ISettingsDataStore>(() => store);
        internal static void SetCharacterFactory(CharacterFactory factory) => s_characterFactory = new Lazy<CharacterFactory>(() => factory);
        internal static void SetTraceService(ITraceService svc) => s_traceService = new Lazy<ITraceService>(() => svc);
        internal static void SetApplicationPaths(IApplicationPaths paths) => s_applicationPaths = new Lazy<IApplicationPaths>(() => paths);
        internal static void SetNameResolver(INameResolver resolver) => s_nameResolver = new Lazy<INameResolver>(() => resolver);
        internal static void SetStationResolver(IStationResolver resolver) => s_stationResolver = new Lazy<IStationResolver>(() => resolver);
        internal static void SetFlagResolver(IFlagResolver resolver) => s_flagResolver = new Lazy<IFlagResolver>(() => resolver);
        internal static void SetImageService(Core.Interfaces.IImageService svc) => s_imageService = new Lazy<Core.Interfaces.IImageService>(() => svc);
        internal static void SetNotificationTypeResolver(INotificationTypeResolver resolver) => s_notificationTypeResolver = new Lazy<INotificationTypeResolver>(() => resolver);
        internal static void SetResourceProvider(IResourceProvider provider) => s_resourceProvider = new Lazy<IResourceProvider>(() => provider);
        internal static void SetNotifications(GlobalNotificationCollection notifications) => s_notifications = new Lazy<GlobalNotificationCollection>(() => notifications);
        internal static void SetCharacters(GlobalCharacterCollection chars) => s_characters = new Lazy<GlobalCharacterCollection>(() => chars);
        internal static void SetESIKeys(GlobalAPIKeyCollection keys) => s_esiKeys = new Lazy<GlobalAPIKeyCollection>(() => keys);
        internal static void SetAPIProviders(GlobalAPIProviderCollection providers) => s_apiProviders = new Lazy<GlobalAPIProviderCollection>(() => providers);
        internal static void SetCharacterIdentities(GlobalCharacterIdentityCollection ids) => s_characterIdentities = new Lazy<GlobalCharacterIdentityCollection>(() => ids);
        internal static void SetMonitoredCharacters(GlobalMonitoredCharacterCollection chars) => s_monitoredCharacters = new Lazy<GlobalMonitoredCharacterCollection>(() => chars);
        internal static void SetLoggerFactory(ILoggerFactory factory) => s_loggerFactory = new Lazy<ILoggerFactory>(() => factory);
        internal static void SetEVEServer(EveServer server) => s_eveServer = new Lazy<EveServer>(() => server);
        internal static void SetEsiScheduler(IEsiScheduler scheduler) => s_esiScheduler = new Lazy<IEsiScheduler>(() => scheduler);
        internal static void SetDialogService(IDialogService svc) => s_dialogService = new Lazy<IDialogService>(() => svc);
        internal static void SetClipboardService(IClipboardService svc) => s_clipboardService = new Lazy<IClipboardService>(() => svc);
        internal static void SetApplicationLifecycle(IApplicationLifecycle svc) => s_applicationLifecycle = new Lazy<IApplicationLifecycle>(() => svc);
        internal static void SetScreenInfo(IScreenInfo svc) => s_screenInfo = new Lazy<IScreenInfo>(() => svc);
        internal static void SetActivityLog(ActivityLogService svc) => s_activityLog = new Lazy<ActivityLogService>(() => svc);
        internal static void SetCharacterDataCache(ICharacterDataCache svc) => s_characterDataCache = new Lazy<ICharacterDataCache>(() => svc);
        internal static void SetHealthTracker(EndpointHealthTracker tracker) => s_healthTracker = new Lazy<EndpointHealthTracker>(() => tracker);

        /// <summary>
        /// Bootstraps the application: initializes filesystem paths, trace logging,
        /// EveLensClient, DI services, and syncs to ServiceLocator.
        /// Call this from Program.cs instead of touching EveLensClient directly.
        /// </summary>
        public static void Bootstrap()
        {
            // Phase 1: Filesystem paths and trace logging (must happen first)
            EveLensClient.CheckIsDebug();
            EveLensClient.CheckIsSnapshot();
            EveLensClient.InitializeFileSystemPaths();
            TraceService.StartLogging(EveLensClient.TraceFileNameFullPath);

            // Phase 2: Snapshot paths so ApplicationPathsAdapter no longer delegates to EveLensClient
            var paths = (ApplicationPathsAdapter)ApplicationPaths;
            paths.SnapshotFromEveLensClient();

            TraceService?.Trace("AppServices.Bootstrap - paths snapshotted", printMethod: false);

            // Phase 3: Initialize EveLensClient (creates collections, timers)
            EveLensClient.Initialize();
            TraceService?.Trace("AppServices.Bootstrap - EveLensClient initialized", printMethod: false);

            // Phase 4: Sync to ServiceLocator for Models/Infrastructure access
            SyncToServiceLocator();
            TraceService?.Trace("AppServices.Bootstrap - ServiceLocator synced", printMethod: false);

            // Phase 5: Initialize the tier subscriber to bridge tab selection to query tier activation
            _ = TierSubscriber;
            TraceService?.Trace("AppServices.Bootstrap - TierSubscriber initialized", printMethod: false);

            // Phase 6: Initialize health tracking — state machine replaces event-based error notifications
            _ = HealthTracker;
            _ = s_healthNotifySub.Value;
            if (EsiScheduler is EsiScheduler esiSched)
                esiSched.SetHealthTracker(HealthTracker);
            TraceService?.Trace("AppServices.Bootstrap - HealthTracker initialized", printMethod: false);
        }

        /// <summary>
        /// Shuts down the application: saves settings, stops timers, disposes resources.
        /// Call this from Program.cs instead of touching EveLensClient directly.
        /// </summary>
        public static void Shutdown()
        {
            // Dispose the ESI scheduler (stops dispatch loop), then persist state
            if (s_esiScheduler.IsValueCreated)
            {
                s_esiScheduler.Value.Dispose();
                s_esiScheduler.Value.PersistState();
            }

            // Dispose the tier subscriber
            if (s_tierSubscriber.IsValueCreated)
                s_tierSubscriber.Value.Dispose();

            // Shutdown settings (dispose SmartSettingsManager and timer subscriptions)
            Common.Settings.Shutdown();

            // Stop the one-second timer and dispose resources
            EveLensClient.Shutdown();

            // Dispose the logger factory (stops TCP listener before trace logging ends)
            if (s_loggerFactory.IsValueCreated && s_loggerFactory.Value is IDisposable disposableFactory)
                disposableFactory.Dispose();

            // Stop trace logging
            TraceService.StopLogging();
        }

        /// <summary>
        /// Starts the UI message loop on the given thread.
        /// Wraps EveLensClient.Run() for the dispatcher timer.
        /// </summary>
        public static void Run(System.Threading.Thread thread)
        {
            EveLensClient.Run(thread);
        }

        /// <summary>
        /// Syncs all service instances to the Core ServiceLocator,
        /// enabling code in EveLens.Models/Infrastructure to access services
        /// without referencing EveLens.Common.
        /// </summary>
        public static void SyncToServiceLocator()
        {
            Core.ServiceLocator.TraceService = TraceService;
            Core.ServiceLocator.ApplicationPaths = ApplicationPaths;
            Core.ServiceLocator.NameResolver = NameResolver;
            Core.ServiceLocator.StationResolver = StationResolver;
            Core.ServiceLocator.FlagResolver = FlagResolver;
            Core.ServiceLocator.ImageService = ImageService;
            Core.ServiceLocator.NotificationTypeResolver = NotificationTypeResolver;
            Core.ServiceLocator.EventAggregator = EventAggregator;
            Core.ServiceLocator.Dispatcher = Dispatcher;
            Core.ServiceLocator.CharacterRepository = CharacterRepository;
            Core.ServiceLocator.ResourceProvider = ResourceProvider;
        }

        /// <summary>
        /// Resets all services to their defaults (for testing).
        /// </summary>
        internal static void Reset()
        {
            s_dispatcher = new Lazy<IDispatcher>(() => new DispatcherService());
            s_settings = new Lazy<ISettingsProvider>(() => new SettingsProviderService());
            s_esiClient = new Lazy<IEsiClient>(() => new EsiClientService());
            s_loggerFactory = new Lazy<ILoggerFactory>(() => CreateLoggerFactory());
            s_eventAggregator = new Lazy<IEventAggregator>(() =>
                new EventAggregator(LoggerFactory.CreateLogger<EventAggregator>()));
            s_characterRepository = new Lazy<ICharacterRepository>(() => new CharacterRepositoryService());
            s_dataStore = new Lazy<ISettingsDataStore>(() => EveLensClientDataStore.Instance);
            s_characterFactory = new Lazy<CharacterFactory>(() => new CharacterFactory(
                CharacterRepository, EventAggregator, EveLensClientCharacterServices.Instance));
            s_traceService = new Lazy<ITraceService>(() => new TraceService());
            s_applicationPaths = new Lazy<IApplicationPaths>(() => new ApplicationPathsAdapter());
            s_nameResolver = new Lazy<INameResolver>(() => new NameResolverAdapter());
            s_stationResolver = new Lazy<IStationResolver>(() => new StationResolverAdapter());
            s_flagResolver = new Lazy<IFlagResolver>(() => new FlagResolverAdapter());
            s_imageService = new Lazy<Core.Interfaces.IImageService>(() => new ImageServiceAdapter());
            s_notificationTypeResolver = new Lazy<INotificationTypeResolver>(() => new NotificationTypeResolverAdapter());
            s_resourceProvider = new Lazy<IResourceProvider>(() => new ResourceProviderAdapter());
            s_notifications = new Lazy<GlobalNotificationCollection>(() => EveLensClient.Notifications);
            s_characters = new Lazy<GlobalCharacterCollection>(() => EveLensClient.Characters);
            s_esiKeys = new Lazy<GlobalAPIKeyCollection>(() => EveLensClient.ESIKeys);
            s_apiProviders = new Lazy<GlobalAPIProviderCollection>(() => EveLensClient.APIProviders);
            s_characterIdentities = new Lazy<GlobalCharacterIdentityCollection>(() => EveLensClient.CharacterIdentities);
            s_monitoredCharacters = new Lazy<GlobalMonitoredCharacterCollection>(() => EveLensClient.MonitoredCharacters);
            s_eveServer = new Lazy<EveServer>(() => EveLensClient.EVEServer);
            s_tierSubscriber = new Lazy<ActiveCharacterTierSubscriber>(() => new ActiveCharacterTierSubscriber());
            s_esiScheduler = new Lazy<IEsiScheduler>(() => new EsiScheduler(
                Dispatcher, EventAggregator, EsiClient,
                LoggerFactory?.CreateLogger<EsiScheduler>()));
            s_dialogService = new Lazy<IDialogService>(() => new NullDialogService());
            s_clipboardService = new Lazy<IClipboardService>(() => new NullClipboardService());
            s_applicationLifecycle = new Lazy<IApplicationLifecycle>(() => new NullApplicationLifecycle());
            s_screenInfo = new Lazy<IScreenInfo>(() => new NullScreenInfo());
            s_activityLog = new Lazy<ActivityLogService>(() =>
                new ActivityLogService(ApplicationPaths.DataDirectory));
            s_characterDataCache = new Lazy<ICharacterDataCache>(() => new CharacterDataCacheService());
            s_healthTracker = new Lazy<EndpointHealthTracker>(() =>
                new EndpointHealthTracker(EventAggregator, Dispatcher));
            s_healthNotifySub = new Lazy<HealthNotificationSubscriber>(() =>
                new HealthNotificationSubscriber(EventAggregator));
            s_privacyMask = PrivacyCategories.None;
        }

        /// <summary>
        /// Creates the default <see cref="ILoggerFactory"/> with TCP JSON-lines and Trace providers.
        /// </summary>
        /// <summary>
        /// The TCP diagnostic stream provider. In debug builds, starts stopped
        /// and is toggled via the Debug menu. Null in release builds.
        /// </summary>
        public static TcpJsonLoggerProvider? DiagnosticStream { get; private set; }

        private static ILoggerFactory CreateLoggerFactory()
        {
            var factory = Microsoft.Extensions.Logging.LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddProvider(new TraceLoggerProvider());
#if DEBUG
                // Register provider but don't auto-start — Debug menu controls it
                DiagnosticStream = new TcpJsonLoggerProvider(autoStart: false);
                builder.AddProvider(DiagnosticStream);
#endif
            });
            return factory;
        }
    }
}
