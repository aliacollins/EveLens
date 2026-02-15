using EVEMon.Core.Interfaces;

namespace EVEMon.Core
{
    /// <summary>
    /// Lightweight service locator in the Core assembly.
    /// Provides interface-only access to services from any assembly layer.
    /// Implementations are registered by EVEMon.Common at startup via AppServices.
    /// </summary>
    public static class ServiceLocator
    {
        public static ITraceService TraceService { get; set; }
        public static IApplicationPaths ApplicationPaths { get; set; }
        public static INameResolver NameResolver { get; set; }
        public static IStationResolver StationResolver { get; set; }
        public static IFlagResolver FlagResolver { get; set; }
        public static IImageService ImageService { get; set; }
        public static INotificationTypeResolver NotificationTypeResolver { get; set; }
        public static IEventAggregator EventAggregator { get; set; }
        public static IDispatcher Dispatcher { get; set; }
        public static ICharacterRepository CharacterRepository { get; set; }
    }
}
