using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using EVEMon.Avalonia.Services;
using EVEMon.Avalonia.Views;
using EVEMon.Common;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Helpers;
using EVEMon.Common.Service;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia
{
    public partial class App : Application
    {
        private SettingsSaveSubscriber? _settingsSaveSubscriber;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                try
                {
                    Bootstrap(desktop);
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Bootstrap failed: {ex.Message}");
                    throw;
                }
            }

            base.OnFrameworkInitializationCompleted();
        }

        private void Bootstrap(IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Phase 1: Bootstrap (paths, trace, EveMonClient, ServiceLocator)
            // This mirrors the WinForms Program.cs startup sequence exactly.
            AppServices.Bootstrap();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - begin", printMethod: false);

            // Phase 2: Register Avalonia platform adapters BEFORE anything uses them
            // This replaces the WinForms defaults with Avalonia implementations.
            AppServices.SetDispatcher(new AvaloniaDispatcher());
            AppServices.SetDialogService(new AvaloniaDialogService());
            AppServices.SetClipboardService(new AvaloniaClipboardService());
            AppServices.SetApplicationLifecycle(new AvaloniaApplicationLifecycle(desktop));
            AppServices.SetScreenInfo(new AvaloniaScreenInfo());
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - platform adapters registered", printMethod: false);

            // Phase 3: Re-sync to ServiceLocator so Models/Infrastructure see Avalonia dispatcher
            AppServices.SyncToServiceLocator();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - ServiceLocator re-synced", printMethod: false);

            // Phase 4: Wire up settings-save subscriber
            _settingsSaveSubscriber = new SettingsSaveSubscriber(AppServices.EventAggregator);

            // Phase 5: Load settings (sync-over-async OK in bootstrap per Law #7 exception)
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Settings.Initialize begin", printMethod: false);
            Settings.Initialize();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Settings.Initialize done", printMethod: false);

            // Phase 6: Load static datafiles
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Loading game data", printMethod: false);
            Task.Run(() => GlobalDatafileCollection.LoadAsync()).Wait();

            // Phase 7: Load ID-to-name caches
            Task.Run(() => TaskHelper.RunIOBoundTaskAsync(() =>
            {
                EveIDToName.InitializeFromFile();
                EveIDToStation.InitializeFromFile();
            })).Wait();

            // Phase 8: Import character data
            Task.Run(() => Settings.ImportDataAsync()).Wait();
            AppServices.SetDataLoaded(true);
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - data loaded", printMethod: false);

            // Phase 9: Start the tick timer (replaces WinForms Dispatcher.Run timer)
            var timer = new global::Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (_, _) => EveMonClient.UpdateOnOneSecondTick();
            timer.Start();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - tick timer started", printMethod: false);

            // Phase 10: Create and show main window
            desktop.MainWindow = new MainWindow();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - MainWindow created", printMethod: false);

            // Phase 11: Register shutdown handler
            desktop.ShutdownRequested += (_, _) =>
            {
                try
                {
                    Task.Run(() => Task.WhenAll(
                        Settings.SaveImmediateAsync(),
                        EveIDToName.SaveImmediateAsync())).Wait();
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Shutdown save failed: {ex.Message}");
                }
                finally
                {
                    _settingsSaveSubscriber?.Dispose();
                    AppServices.Shutdown();
                }
            };
        }
    }
}
