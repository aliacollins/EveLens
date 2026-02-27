// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using EveLens.Avalonia.Services;
using EveLens.Avalonia.Views;
using EveLens.Common;
using EveLens.Common.Collections.Global;
using EveLens.Common.Helpers;
using EveLens.Common.Service;
using EveLens.Common.Services;

namespace EveLens.Avalonia
{
    public partial class App : Application
    {
        private SettingsSaveSubscriber? _settingsSaveSubscriber;
        private TrayIcon? _trayIcon;
        private NativeMenu? _trayMenu;
        /// <summary>
        /// Set to true before calling Shutdown() so the Closing handler
        /// skips minimize-to-tray logic and lets the window close.
        /// </summary>
        internal static bool IsExiting { get; set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            // Load palette (colors/brushes) FIRST, then control styles that reference them.
            // Theme preference is stored in a plain text file so it can be read before
            // EveLensClient/Settings are initialized.
            string theme = ReadThemePreference();
            var paletteUri = new Uri($"avares://EveLens/Themes/Palettes/{theme}.axaml");
            Styles.Add(new StyleInclude(paletteUri) { Source = paletteUri });

            var themeUri = new Uri("avares://EveLens/Themes/EveLensTheme.axaml");
            Styles.Add(new StyleInclude(themeUri) { Source = themeUri });
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
            // Phase 1: Bootstrap (paths, trace, EveLensClient, ServiceLocator)
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

            // Show splash screen before the slow phases begin.
            // Prevent app shutdown when the splash closes (it's the only window at that point).
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            var splash = new SplashWindow();
            splash.Show();

            // Phase 5: Load settings (sync-over-async OK in bootstrap per Law #7 exception)
            splash.UpdateStatus("Loading settings...");
            Dispatcher.UIThread.RunJobs();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Settings.Initialize begin", printMethod: false);
            Settings.Initialize();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Settings.Initialize done", printMethod: false);

            // Phase 6: Load static datafiles
            splash.UpdateStatus("Loading game data...");
            Dispatcher.UIThread.RunJobs();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - Loading game data", printMethod: false);
            Task.Run(() => GlobalDatafileCollection.LoadAsync()).Wait();

            // Phase 7: Load ID-to-name caches
            splash.UpdateStatus("Loading name caches...");
            Dispatcher.UIThread.RunJobs();
            Task.Run(() => TaskHelper.RunIOBoundTaskAsync(() =>
            {
                EveIDToName.InitializeFromFile();
                EveIDToStation.InitializeFromFile();
            })).Wait();

            // Phase 8: Import character data
            splash.UpdateStatus("Loading characters...");
            Dispatcher.UIThread.RunJobs();
            Task.Run(() => Settings.ImportDataAsync()).Wait();
            AppServices.SetDataLoaded(true);
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - data loaded", printMethod: false);

            // Close splash and restore normal shutdown behavior before creating the main window
            splash.Close();

            // Always use explicit shutdown — the Closing handler decides whether
            // to hide (tray) or trigger desktop.Shutdown() (exit). This avoids a
            // race condition where ShutdownMode is still OnExplicitShutdown when
            // the user changes MinimizeToTray and closes before the Post() executes.
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            // Phase 9: Start the tick timer (replaces WinForms Dispatcher.Run timer)
            var timer = new global::Avalonia.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            timer.Tick += (_, _) => EveLensClient.UpdateOnOneSecondTick();
            timer.Start();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - tick timer started", printMethod: false);

            // Phase 10: Create and show main window
            desktop.MainWindow = new MainWindow();
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - MainWindow created", printMethod: false);

            // Phase 11: Set up system tray icon
            SetupTrayIcon(desktop);
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - tray icon configured", printMethod: false);

            // Phase 12: Register shutdown handler
            desktop.ShutdownRequested += (_, _) =>
            {
                try
                {
                    // We're on the UI thread here. SmartSettingsManager.PerformSaveAsync
                    // uses _dispatcher.Post(Export) which needs the UI thread to be free.
                    // Calling .Wait() would deadlock on Linux/macOS.
                    //
                    // Instead: Export synchronously (we're on the UI thread), then write
                    // to disk synchronously. This guarantees the save completes before
                    // the process exits.
                    Settings.SaveSynchronousForShutdown();
                    EveIDToName.SaveImmediateAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Shutdown save failed: {ex.Message}");
                }
                finally
                {
                    DestroyTrayIcon();
                    _settingsSaveSubscriber?.Dispose();
                    AppServices.Shutdown();
                }
            };
        }

        private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                // Handle window close → minimize-to-tray or explicit exit
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.Closing += (_, e) =>
                    {
                        if (IsExiting)
                            return;

                        if (Settings.UI.MinimizeToTray)
                        {
                            e.Cancel = true;
                            desktop.MainWindow.Hide();
                        }
                        else
                        {
                            // Save settings NOW, before the window closes.
                            // On Linux/X11 the process can exit before Post() runs,
                            // so ShutdownRequested never fires and settings are lost.
                            IsExiting = true;
                            try
                            {
                                Settings.SaveSynchronousForShutdown();
                                EveIDToName.SaveImmediateAsync().GetAwaiter().GetResult();
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Pre-close save failed: {ex.Message}");
                            }
                            Dispatcher.UIThread.Post(() => desktop.Shutdown());
                        }
                    };
                }

                // Only create the tray icon if the setting is on
                if (Settings.UI.MinimizeToTray)
                    CreateTrayIcon(desktop);

                // When settings change, create or destroy tray icon dynamically
                AppServices.EventAggregator?.Subscribe<Common.Events.SettingsChangedEvent>(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            bool enabled = Settings.UI.MinimizeToTray;

                            if (enabled && _trayIcon == null)
                                CreateTrayIcon(desktop);
                            else if (!enabled && _trayIcon != null)
                                DestroyTrayIcon();
                        }
                        catch (Exception ex)
                        {
                            AppServices.TraceService?.Trace(
                                $"Tray settings update failed: {ex.Message}", printMethod: false);
                        }
                    });
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up tray icon: {ex}");
                AppServices.TraceService?.Trace($"Tray icon setup failed: {ex.Message}", printMethod: false);
            }
        }

        private void CreateTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            _trayMenu = new NativeMenu();

            var showItem = new NativeMenuItem("Show EveLens");
            showItem.Click += (_, _) => ShowMainWindow(desktop);
            _trayMenu.Add(showItem);

            _trayMenu.Add(new NativeMenuItemSeparator());

            var exitItem = new NativeMenuItem("Exit");
            exitItem.Click += (_, _) =>
            {
                IsExiting = true;
                DestroyTrayIcon();
                desktop.Shutdown();
            };
            _trayMenu.Add(exitItem);

            _trayIcon = new TrayIcon
            {
                ToolTipText = AppServices.ProductNameWithVersion,
                Menu = _trayMenu,
                Icon = LoadTrayIcon(),
                IsVisible = true
            };

            _trayIcon.Clicked += (_, _) => ShowMainWindow(desktop);
        }

        private void DestroyTrayIcon()
        {
            _trayIcon?.Dispose();
            _trayIcon = null;
            _trayMenu = null;
        }

        private static WindowIcon? LoadTrayIcon()
        {
            try
            {
                var uri = new Uri("avares://EveLens/Properties/EveLens.ico");
                return new WindowIcon(global::Avalonia.Platform.AssetLoader.Open(uri));
            }
            catch
            {
                return null;
            }
        }

        private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var window = desktop.MainWindow;
                    if (window == null)
                        return;

                    window.Show();
                    window.WindowState = WindowState.Normal;
                    window.Activate();
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace(
                        $"ShowMainWindow failed: {ex.Message}", printMethod: false);
                }
            });
        }

        private static string ReadThemePreference()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EveLens", "theme.txt");
                if (File.Exists(path))
                {
                    string name = File.ReadAllText(path).Trim();
                    if (!string.IsNullOrEmpty(name))
                        return name;
                }
            }
            catch
            {
                // Fall through to default
            }

            return "DarkSpace";
        }
    }
}
