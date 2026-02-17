using EVEMon.Core.Interfaces;

namespace EVEMon.Core
{
    /// <summary>
    /// Lightweight, Core-level service locator that bridges the gap between the
    /// <c>EVEMon.Core</c> assembly and service implementations in <c>EVEMon.Common</c>.
    /// Provides static access to services for code that cannot use constructor injection
    /// (static data classes, model base classes, infrastructure utilities).
    /// </summary>
    /// <remarks>
    /// <b>When to use:</b> Only for static data loading classes (<c>StaticSkills</c>,
    /// <c>StaticBlueprints</c>, etc.) that load BEFORE the DI container is available,
    /// and for model/infrastructure code that cannot reference <c>EVEMon.Common</c>.
    /// All other code should use <c>AppServices</c> (in <c>EVEMon.Common</c>) or
    /// constructor injection.
    ///
    /// <b>Initialization:</b> Properties are populated by
    /// <c>AppServices.SyncToServiceLocator()</c> during <c>AppServices.Bootstrap()</c>.
    /// Before sync, all properties are null. Code using this locator should null-check
    /// or use <c>?.</c> when called during early bootstrap.
    ///
    /// <b>Property setters are public</b> to allow test setup. In production, they are
    /// only written to by <c>AppServices.SyncToServiceLocator()</c>.
    ///
    /// <b>Thread safety:</b> Properties are set once during single-threaded bootstrap and
    /// then read-only for the rest of the application lifetime. No synchronization needed.
    ///
    /// <b>Relationship to AppServices:</b>
    /// <list type="bullet">
    ///   <item><c>AppServices</c> (in <c>EVEMon.Common</c>) owns and creates all service instances.</item>
    ///   <item><c>ServiceLocator</c> (in <c>EVEMon.Core</c>) holds references to the same instances,
    ///     accessible from assemblies that cannot reference <c>EVEMon.Common</c>.</item>
    ///   <item><c>AppServices.SyncToServiceLocator()</c> copies references from AppServices to here.</item>
    /// </list>
    /// </remarks>
    public static class ServiceLocator
    {
        /// <summary>
        /// Gets or sets the trace/diagnostic logging service.
        /// Powered by standalone <c>TraceService</c> in <c>EVEMon.Common</c>.
        /// </summary>
        public static ITraceService TraceService { get; set; }

        /// <summary>
        /// Gets or sets the application directory/file path provider.
        /// Snapshotted from <c>EveMonClient</c> static paths at startup.
        /// </summary>
        public static IApplicationPaths ApplicationPaths { get; set; }

        /// <summary>
        /// Gets or sets the EVE entity ID to name resolver.
        /// Delegates to <c>EveIDToName</c> and <c>EveRefType</c> statics.
        /// </summary>
        public static INameResolver NameResolver { get; set; }

        /// <summary>
        /// Gets or sets the station/structure ID resolver.
        /// Delegates to <c>EveIDToStation</c> static methods.
        /// </summary>
        public static IStationResolver StationResolver { get; set; }

        /// <summary>
        /// Gets or sets the inventory flag ID resolver.
        /// Delegates to <c>EveFlag</c> static methods.
        /// </summary>
        public static IFlagResolver FlagResolver { get; set; }

        /// <summary>
        /// Gets or sets the image downloading/caching service.
        /// Delegates to <c>ImageService</c> static methods.
        /// </summary>
        public static IImageService ImageService { get; set; }

        /// <summary>
        /// Gets or sets the notification type code resolver.
        /// Delegates to <c>EveNotificationType</c> static methods.
        /// </summary>
        public static INotificationTypeResolver NotificationTypeResolver { get; set; }

        /// <summary>
        /// Gets or sets the event aggregator for publish/subscribe messaging.
        /// Same instance as <c>AppServices.EventAggregator</c>.
        /// </summary>
        public static IEventAggregator EventAggregator { get; set; }

        /// <summary>
        /// Gets or sets the UI thread dispatcher for marshaling and scheduling.
        /// Same instance as <c>AppServices.Dispatcher</c>.
        /// </summary>
        public static IDispatcher Dispatcher { get; set; }

        /// <summary>
        /// Gets or sets the character collection repository (read + write).
        /// Delegates to <c>EveMonClient.Characters</c> and <c>MonitoredCharacters</c>.
        /// </summary>
        public static ICharacterRepository CharacterRepository { get; set; }

        /// <summary>
        /// Gets or sets the embedded resource provider (XSLT, static CSV data).
        /// Delegates to <c>Properties.Resources</c> in <c>EVEMon.Common</c>.
        /// </summary>
        public static IResourceProvider ResourceProvider { get; set; }
    }
}
