// EVEMon NexT — Character Intelligence for EVE Online
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
using EVEMon.Avalonia.Services;
using EVEMon.Avalonia.Views;
using EVEMon.Common;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Helpers;
using EVEMon.Common.Service;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia
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
            // EveMonClient/Settings are initialized.
            string theme = ReadThemePreference();
            var paletteUri = new Uri($"avares://EVEMon NexT/Themes/Palettes/{theme}.axaml");
            Styles.Add(new StyleInclude(paletteUri) { Source = paletteUri });

            var themeUri = new Uri("avares://EVEMon NexT/Themes/EVEMonTheme.axaml");
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

            // If tray icon can keep the app running (minimize-to-tray), use explicit shutdown
            // so hiding the main window doesn't terminate the process
            var closeBehaviour = Settings.UI.MainWindowCloseBehaviour;
            var trayMode = Settings.UI.SystemTrayIcon;
            desktop.ShutdownMode = (closeBehaviour == CloseBehaviour.MinimizeToTray &&
                                    trayMode != SystemTrayBehaviour.Disabled)
                ? ShutdownMode.OnExplicitShutdown
                : ShutdownMode.OnMainWindowClose;

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

            // Phase 11: Set up system tray icon
            SetupTrayIcon(desktop);
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - tray icon configured", printMethod: false);

            // Phase 12: Register shutdown handler
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
                    _trayIcon?.Dispose();
                    _trayIcon = null;
                    _settingsSaveSubscriber?.Dispose();
                    AppServices.Shutdown();
                }
            };
        }

        private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
        {
            try
            {
                var trayBehaviour = Settings.UI.SystemTrayIcon;

                // Build context menu
                _trayMenu = new NativeMenu();

                var showItem = new NativeMenuItem("Show EVEMon");
                showItem.Click += (_, _) => ShowMainWindow(desktop);
                _trayMenu.Add(showItem);

                _trayMenu.Add(new NativeMenuItemSeparator());

                var exitItem = new NativeMenuItem("Exit");
                exitItem.Click += (_, _) =>
                {
                    IsExiting = true;
                    _trayIcon?.Dispose();
                    _trayIcon = null;
                    desktop.Shutdown();
                };
                _trayMenu.Add(exitItem);

                // Create tray icon
                _trayIcon = new TrayIcon
                {
                    ToolTipText = AppServices.ProductNameWithVersion,
                    Menu = _trayMenu,
                    Icon = LoadTrayIcon(),
                    IsVisible = trayBehaviour == SystemTrayBehaviour.AlwaysVisible
                };

                _trayIcon.Clicked += (_, _) => ShowMainWindow(desktop);

                // Handle window close → minimize-to-tray
                if (desktop.MainWindow != null)
                {
                    desktop.MainWindow.Closing += (_, e) =>
                    {
                        // When exiting from tray or explicit shutdown, let the close proceed
                        if (IsExiting)
                            return;

                        var closeBehaviour = Settings.UI.MainWindowCloseBehaviour;
                        var trayMode = Settings.UI.SystemTrayIcon;

                        if (closeBehaviour == CloseBehaviour.MinimizeToTray &&
                            trayMode != SystemTrayBehaviour.Disabled)
                        {
                            e.Cancel = true;
                            desktop.MainWindow.Hide();
                            if (_trayIcon != null)
                                _trayIcon.IsVisible = true;
                        }
                        else if (closeBehaviour == CloseBehaviour.MinimizeToTaskbar)
                        {
                            e.Cancel = true;
                            desktop.MainWindow.WindowState = WindowState.Minimized;
                        }
                        // CloseBehaviour.Exit → let it close normally
                    };

                    // Show/hide tray on minimize (for ShowWhenMinimized mode)
                    desktop.MainWindow.PropertyChanged += (_, e) =>
                    {
                        if (e.Property != Window.WindowStateProperty || _trayIcon == null) return;
                        var state = (WindowState)(e.NewValue ?? WindowState.Normal);
                        var trayMode = Settings.UI.SystemTrayIcon;

                        if (trayMode == SystemTrayBehaviour.ShowWhenMinimized)
                        {
                            _trayIcon.IsVisible = state == WindowState.Minimized;
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error setting up tray icon: {ex}");
                AppServices.TraceService?.Trace($"Tray icon setup failed: {ex.Message}", printMethod: false);
            }
        }

        private static WindowIcon? LoadTrayIcon()
        {
            try
            {
                var uri = new Uri("avares://EVEMon NexT/Properties/EVEMon.ico");
                return new WindowIcon(global::Avalonia.Platform.AssetLoader.Open(uri));
            }
            catch
            {
                return null;
            }
        }

        private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            if (desktop.MainWindow == null) return;
            desktop.MainWindow.Show();
            desktop.MainWindow.WindowState = WindowState.Normal;
            desktop.MainWindow.Activate();
        }

        private static string ReadThemePreference()
        {
            try
            {
                string path = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "EVEMon", "theme.txt");
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
