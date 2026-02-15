using EVEMon.Common.Interfaces;
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
        private static IDispatcher s_dispatcher;
        private static ISettingsProvider s_settings;
        private static IEsiClient s_esiClient;
        private static IEventAggregator s_eventAggregator;
        private static ICharacterRepository s_characterRepository;
        private static ISettingsDataStore s_dataStore;
        private static CharacterFactory s_characterFactory;
        private static ITraceService s_traceService;
        private static IApplicationPaths s_applicationPaths;
        private static INameResolver s_nameResolver;
        private static IStationResolver s_stationResolver;
        private static IFlagResolver s_flagResolver;
        private static Core.Interfaces.IImageService s_imageService;
        private static INotificationTypeResolver s_notificationTypeResolver;

        /// <summary>
        /// Gets the dispatcher service for UI thread marshaling.
        /// </summary>
        public static IDispatcher Dispatcher
            => s_dispatcher ?? (s_dispatcher = new DispatcherService());

        /// <summary>
        /// Gets the settings provider service.
        /// </summary>
        public static ISettingsProvider Settings
            => s_settings ?? (s_settings = new SettingsProviderService());

        /// <summary>
        /// Gets the ESI client service for API rate limiting.
        /// </summary>
        public static IEsiClient EsiClient
            => s_esiClient ?? (s_esiClient = new EsiClientService());

        /// <summary>
        /// Gets the event aggregator for publish/subscribe messaging.
        /// </summary>
        public static IEventAggregator EventAggregator
            => s_eventAggregator ?? (s_eventAggregator = new EventAggregator());

        /// <summary>
        /// Gets the character repository service.
        /// </summary>
        public static ICharacterRepository CharacterRepository
            => s_characterRepository ?? (s_characterRepository = new CharacterRepositoryService());

        /// <summary>
        /// Gets the data store service for settings import/export.
        /// </summary>
        public static ISettingsDataStore DataStore
            => s_dataStore ?? (s_dataStore = EveMonClientDataStore.Instance);

        /// <summary>
        /// Gets the character factory for creating CCPCharacter instances.
        /// </summary>
        internal static CharacterFactory CharacterFactory
            => s_characterFactory ??= new CharacterFactory(
                CharacterRepository, EventAggregator, EveMonClientCharacterServices.Instance);

        /// <summary>
        /// Gets the trace service for diagnostic logging.
        /// </summary>
        public static ITraceService TraceService
            => s_traceService ?? (s_traceService = new TraceServiceAdapter());

        /// <summary>
        /// Gets the application paths service.
        /// </summary>
        public static IApplicationPaths ApplicationPaths
            => s_applicationPaths ?? (s_applicationPaths = new ApplicationPathsAdapter());

        /// <summary>
        /// Gets the name resolver for EVE entity ID to name lookups.
        /// </summary>
        public static INameResolver NameResolver
            => s_nameResolver ?? (s_nameResolver = new NameResolverAdapter());

        /// <summary>
        /// Gets the station resolver for station/structure lookups.
        /// </summary>
        public static IStationResolver StationResolver
            => s_stationResolver ?? (s_stationResolver = new StationResolverAdapter());

        /// <summary>
        /// Gets the flag resolver for inventory flag lookups.
        /// </summary>
        public static IFlagResolver FlagResolver
            => s_flagResolver ?? (s_flagResolver = new FlagResolverAdapter());

        /// <summary>
        /// Gets the image service for image downloading and caching.
        /// </summary>
        public static Core.Interfaces.IImageService ImageService
            => s_imageService ?? (s_imageService = new ImageServiceAdapter());

        /// <summary>
        /// Gets the notification type resolver.
        /// </summary>
        public static INotificationTypeResolver NotificationTypeResolver
            => s_notificationTypeResolver ?? (s_notificationTypeResolver = new NotificationTypeResolverAdapter());

        /// <summary>
        /// Replaces a service implementation (for testing or DI transition).
        /// </summary>
        internal static void SetDispatcher(IDispatcher dispatcher) => s_dispatcher = dispatcher;
        internal static void SetSettings(ISettingsProvider settings) => s_settings = settings;
        internal static void SetEsiClient(IEsiClient esiClient) => s_esiClient = esiClient;
        internal static void SetEventAggregator(IEventAggregator aggregator) => s_eventAggregator = aggregator;
        internal static void SetCharacterRepository(ICharacterRepository repo) => s_characterRepository = repo;
        internal static void SetDataStore(ISettingsDataStore store) => s_dataStore = store;
        internal static void SetCharacterFactory(CharacterFactory factory) => s_characterFactory = factory;
        internal static void SetTraceService(ITraceService svc) => s_traceService = svc;
        internal static void SetApplicationPaths(IApplicationPaths paths) => s_applicationPaths = paths;
        internal static void SetNameResolver(INameResolver resolver) => s_nameResolver = resolver;
        internal static void SetStationResolver(IStationResolver resolver) => s_stationResolver = resolver;
        internal static void SetFlagResolver(IFlagResolver resolver) => s_flagResolver = resolver;
        internal static void SetImageService(Core.Interfaces.IImageService svc) => s_imageService = svc;
        internal static void SetNotificationTypeResolver(INotificationTypeResolver resolver) => s_notificationTypeResolver = resolver;

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
        }

        /// <summary>
        /// Resets all services to their defaults (for testing).
        /// </summary>
        internal static void Reset()
        {
            s_dispatcher = null;
            s_settings = null;
            s_esiClient = null;
            s_eventAggregator = null;
            s_characterRepository = null;
            s_dataStore = null;
            s_characterFactory = null;
            s_traceService = null;
            s_applicationPaths = null;
            s_nameResolver = null;
            s_stationResolver = null;
            s_flagResolver = null;
            s_imageService = null;
            s_notificationTypeResolver = null;
        }
    }
}
