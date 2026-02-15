using System;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
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
        private static Lazy<IEventAggregator> s_eventAggregator = new(() => new EventAggregator());
        private static Lazy<ICharacterRepository> s_characterRepository = new(() => new CharacterRepositoryService());
        private static Lazy<ISettingsDataStore> s_dataStore = new(() => EveMonClientDataStore.Instance);
        private static Lazy<CharacterFactory> s_characterFactory = new(() => new CharacterFactory(
            CharacterRepository, EventAggregator, EveMonClientCharacterServices.Instance));
        private static Lazy<ITraceService> s_traceService = new(() => new TraceServiceAdapter());
        private static Lazy<IApplicationPaths> s_applicationPaths = new(() => new ApplicationPathsAdapter());
        private static Lazy<INameResolver> s_nameResolver = new(() => new NameResolverAdapter());
        private static Lazy<IStationResolver> s_stationResolver = new(() => new StationResolverAdapter());
        private static Lazy<IFlagResolver> s_flagResolver = new(() => new FlagResolverAdapter());
        private static Lazy<Core.Interfaces.IImageService> s_imageService = new(() => new ImageServiceAdapter());
        private static Lazy<INotificationTypeResolver> s_notificationTypeResolver = new(() => new NotificationTypeResolverAdapter());
        private static Lazy<IResourceProvider> s_resourceProvider = new(() => new ResourceProviderAdapter());
        private static Lazy<GlobalNotificationCollection> s_notifications = new(() => EveMonClient.Notifications);
        private static Lazy<GlobalCharacterCollection> s_characters = new(() => EveMonClient.Characters);
        private static Lazy<GlobalAPIKeyCollection> s_esiKeys = new(() => EveMonClient.ESIKeys);
        private static Lazy<GlobalAPIProviderCollection> s_apiProviders = new(() => EveMonClient.APIProviders);
        private static Lazy<GlobalCharacterIdentityCollection> s_characterIdentities = new(() => EveMonClient.CharacterIdentities);
        private static Lazy<GlobalMonitoredCharacterCollection> s_monitoredCharacters = new(() => EveMonClient.MonitoredCharacters);
        private static Lazy<EveServer> s_eveServer = new(() => EveMonClient.EVEServer);

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
        /// Gets whether the application is closed.
        /// </summary>
        public static bool Closed => EveMonClient.Closed;

        /// <summary>
        /// Gets whether this is a debug build.
        /// </summary>
        public static bool IsDebugBuild => EveMonClient.IsDebugBuild;

        /// <summary>
        /// Gets whether the static data has been loaded.
        /// </summary>
        public static bool IsDataLoaded => EveMonClient.IsDataLoaded;

        /// <summary>
        /// Sets whether the static data has been loaded.
        /// </summary>
        public static void SetDataLoaded(bool value) => EveMonClient.IsDataLoaded = value;

        /// <summary>
        /// Gets whether this is a pre-release version.
        /// </summary>
        public static bool IsPreReleaseVersion => EveMonClient.IsPreReleaseVersion;

        /// <summary>
        /// Gets whether this is an alpha version.
        /// </summary>
        public static bool IsAlphaVersion => EveMonClient.IsAlphaVersion;

        /// <summary>
        /// Gets whether this is a beta version.
        /// </summary>
        public static bool IsBetaVersion => EveMonClient.IsBetaVersion;

        /// <summary>
        /// Gets the file version information for the application.
        /// </summary>
        public static System.Diagnostics.FileVersionInfo FileVersionInfo => EveMonClient.FileVersionInfo;

        /// <summary>
        /// Gets the product name with version string.
        /// </summary>
        public static string ProductNameWithVersion => EveMonClient.ProductNameWithVersion;

        /// <summary>
        /// Gets the version string.
        /// </summary>
        public static string VersionString => EveMonClient.VersionString;

        /// <summary>
        /// Gets the settings file name.
        /// </summary>
        public static string SettingsFileName => EveMonClient.SettingsFileName;

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
        internal static void SetEVEServer(EveServer server) => s_eveServer = new Lazy<EveServer>(() => server);

        /// <summary>
        /// Syncs all service instances to the Core ServiceLocator,
        /// enabling code in EVEMon.Models/Infrastructure to access services
        /// without referencing EVEMon.Common.
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
            s_eventAggregator = new Lazy<IEventAggregator>(() => new EventAggregator());
            s_characterRepository = new Lazy<ICharacterRepository>(() => new CharacterRepositoryService());
            s_dataStore = new Lazy<ISettingsDataStore>(() => EveMonClientDataStore.Instance);
            s_characterFactory = new Lazy<CharacterFactory>(() => new CharacterFactory(
                CharacterRepository, EventAggregator, EveMonClientCharacterServices.Instance));
            s_traceService = new Lazy<ITraceService>(() => new TraceServiceAdapter());
            s_applicationPaths = new Lazy<IApplicationPaths>(() => new ApplicationPathsAdapter());
            s_nameResolver = new Lazy<INameResolver>(() => new NameResolverAdapter());
            s_stationResolver = new Lazy<IStationResolver>(() => new StationResolverAdapter());
            s_flagResolver = new Lazy<IFlagResolver>(() => new FlagResolverAdapter());
            s_imageService = new Lazy<Core.Interfaces.IImageService>(() => new ImageServiceAdapter());
            s_notificationTypeResolver = new Lazy<INotificationTypeResolver>(() => new NotificationTypeResolverAdapter());
            s_resourceProvider = new Lazy<IResourceProvider>(() => new ResourceProviderAdapter());
            s_notifications = new Lazy<GlobalNotificationCollection>(() => EveMonClient.Notifications);
            s_characters = new Lazy<GlobalCharacterCollection>(() => EveMonClient.Characters);
            s_esiKeys = new Lazy<GlobalAPIKeyCollection>(() => EveMonClient.ESIKeys);
            s_apiProviders = new Lazy<GlobalAPIProviderCollection>(() => EveMonClient.APIProviders);
            s_characterIdentities = new Lazy<GlobalCharacterIdentityCollection>(() => EveMonClient.CharacterIdentities);
            s_monitoredCharacters = new Lazy<GlobalMonitoredCharacterCollection>(() => EveMonClient.MonitoredCharacters);
            s_eveServer = new Lazy<EveServer>(() => EveMonClient.EVEServer);
        }
    }
}
