using EVEMon.Core.Interfaces;

namespace EVEMon.Core
{
    /// <summary>
    /// Lightweight Core-level service access for pre-DI static data loading only
    /// (StaticSkills, StaticBlueprints, etc.). Static data classes load BEFORE the DI
    /// container, so they use this ServiceLocator. Everything else should use constructor
    /// injection or AppServices.
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
        public static IResourceProvider ResourceProvider { get; set; }
    }
}
