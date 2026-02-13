using System;
using Microsoft.Extensions.DependencyInjection;
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
        private static IServiceProvider s_serviceProvider;

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

            // Register core interfaces as singletons (they wrap static classes)
            services.AddSingleton<IDispatcher>(sp => AppServices.Dispatcher);
            services.AddSingleton<ISettingsProvider>(sp => AppServices.Settings);
            services.AddSingleton<IEsiClient>(sp => AppServices.EsiClient);
            services.AddSingleton<IEventAggregator>(sp => AppServices.EventAggregator);
            services.AddSingleton<ICharacterRepository>(sp => AppServices.CharacterRepository);

            s_serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Disposes the service provider and releases resources.
        /// </summary>
        internal static void Dispose()
        {
            if (s_serviceProvider is IDisposable disposable)
                disposable.Dispose();

            s_serviceProvider = null;
        }
    }
}
