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
        /// Replaces a service implementation (for testing or DI transition).
        /// </summary>
        internal static void SetDispatcher(IDispatcher dispatcher) => s_dispatcher = dispatcher;
        internal static void SetSettings(ISettingsProvider settings) => s_settings = settings;
        internal static void SetEsiClient(IEsiClient esiClient) => s_esiClient = esiClient;
        internal static void SetEventAggregator(IEventAggregator aggregator) => s_eventAggregator = aggregator;
        internal static void SetCharacterRepository(ICharacterRepository repo) => s_characterRepository = repo;

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
        }
    }
}
