using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;

namespace EVEMon
{
    /// <summary>
    /// DI composition root for EVEMon.
    /// Registers all service interfaces and their Strangler Fig implementations.
    /// </summary>
    /// <remarks>
    /// This is called early in startup, before MainWindow is created.
    /// Services wrap the existing static classes (EveMonClient, Settings, Dispatcher)
    /// so they work identically to the original code while enabling testability.
    /// </remarks>
    internal static class ServiceRegistration
    {
        private static IServiceProvider s_serviceProvider = null!;

        /// <summary>
        /// Gets the application's service provider.
        /// </summary>
        internal static IServiceProvider ServiceProvider => s_serviceProvider;

        /// <summary>
        /// Configures and builds the DI container, then wires services into AppServices.
        /// </summary>
        internal static void Configure()
        {
            var services = new ServiceCollection();

            // Core services (existing)
            services.AddSingleton<IDispatcher>(sp => AppServices.Dispatcher);
            services.AddSingleton<ISettingsProvider>(sp => AppServices.Settings);
            services.AddSingleton<IEsiClient>(sp => AppServices.EsiClient);
            services.AddSingleton<IEventAggregator>(sp => AppServices.EventAggregator);
            services.AddSingleton<ICharacterRepository>(sp => AppServices.CharacterRepository);

            // Infrastructure services
            services.AddSingleton<ITraceService>(sp => AppServices.TraceService);
            services.AddSingleton<IApplicationPaths>(sp => AppServices.ApplicationPaths);
            services.AddSingleton<INameResolver>(sp => AppServices.NameResolver);
            services.AddSingleton<IStationResolver>(sp => AppServices.StationResolver);
            services.AddSingleton<IFlagResolver>(sp => AppServices.FlagResolver);
            services.AddSingleton<Core.Interfaces.IImageService>(sp => AppServices.ImageService);
            services.AddSingleton<INotificationTypeResolver>(sp => AppServices.NotificationTypeResolver);
            services.AddSingleton<IResourceProvider>(sp => AppServices.ResourceProvider);
            services.AddSingleton<ISettingsDataStore>(sp => AppServices.DataStore);

            // Global collection services
            services.AddSingleton(sp => AppServices.Notifications);
            services.AddSingleton(sp => AppServices.Characters);
            services.AddSingleton(sp => AppServices.ESIKeys);
            services.AddSingleton(sp => AppServices.MonitoredCharacters);
            services.AddSingleton(sp => AppServices.EVEServer);

            services.AddSingleton<IEsiScheduler>(sp => AppServices.EsiScheduler);
            services.AddSingleton<ILoggerFactory>(sp => AppServices.LoggerFactory);

            s_serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Disposes the service provider and releases resources.
        /// </summary>
        internal static void Dispose()
        {
            if (s_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            s_serviceProvider = null!;
        }
    }
}
