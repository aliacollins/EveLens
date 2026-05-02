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
using EveLens.Common.Models;
using EveLens.Common.Service;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using System.Linq;
using System.Text;

namespace EveLens.Avalonia
{
    public partial class App : Application
    {
        private SettingsSaveSubscriber? _settingsSaveSubscriber;
        private TrayIcon? _trayIcon;
        private NativeMenu? _trayMenu;
        private IDisposable? _traySettingsSub;
        private IDisposable? _trayUpdateSub;
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

            // Apply font scale and language from settings (before any views are created)
            FontScaleService.Apply(Settings.UI.FontScalePercent);
            Loc.Language = Settings.UI.Language ?? "en";

            // Phase 6: Load static datafiles
            splash.UpdateStatus(Loc.Get("Splash.LoadingGameData"));
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

            // Phase 8b: Refresh all ESI tokens before the scheduler starts dispatching.
            // On startup, all access tokens are expired. Without this, the scheduler
            // fires hundreds of requests with expired tokens, burning CCP's error budget
            // and triggering the 420 cascade (Issue #34).
            splash.UpdateStatus("Refreshing ESI tokens...");
            Dispatcher.UIThread.RunJobs();
            foreach (var key in EveLensClient.ESIKeys)
                key.ForceUpdate();

            // Give token refreshes a moment to complete before scheduler fires
            Task.Delay(TimeSpan.FromSeconds(3)).Wait();

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

            // Phase 10.5: Linux desktop integration (icon + .desktop file)
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(
                    System.Runtime.InteropServices.OSPlatform.Linux))
            {
                try { RegisterLinuxDesktopEntry(); }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace($"Linux desktop integration failed: {ex.Message}", printMethod: false);
                }
            }

            // Phase 11: Set up system tray icon
            SetupTrayIcon(desktop);
            AppServices.TraceService?.Trace("Avalonia.App.Bootstrap - tray icon configured", printMethod: false);

            // Phase 12: Register shutdown handler
            desktop.ShutdownRequested += (_, _) =>
            {
                try
                {
                    // Save settings with a timeout — Windows OS shutdown gives ~5 seconds.
                    // If save takes too long, abandon it rather than blocking shutdown.
                    var saveTask = System.Threading.Tasks.Task.Run(() =>
                    {
                        try
                        {
                            Settings.SaveSynchronousForShutdown();
                        }
                        catch { /* Non-critical — don't block shutdown */ }
                    });

                    // Wait up to 3 seconds for settings save
                    saveTask.Wait(TimeSpan.FromSeconds(3));

                    // ID-to-name cache: fire and forget, don't block
                    try { EveIDToName.SaveImmediateAsync().Wait(TimeSpan.FromSeconds(1)); }
                    catch { }
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

                        // Save window position/size before anything else —
                        // must happen before Settings.SaveSynchronousForShutdown()
                        if (desktop.MainWindow is MainWindow mw)
                            mw.SaveWindowLocationNow();

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

                            // Close all modeless child windows first to prevent zombie
                            // processes on macOS where owned windows block shutdown.
                            if (desktop.MainWindow is MainWindow mw2)
                                mw2.CloseChildWindows();

                            try
                            {
                                Settings.SaveSynchronousForShutdown();
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
                _traySettingsSub = AppServices.EventAggregator?.Subscribe<Common.Events.SettingsChangedEvent>(_ =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            // Don't touch the tray icon if we're shutting down or restarting
                            // (e.g., theme change triggers SettingsChangedEvent before restart)
                            if (IsExiting)
                                return;

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
                if (desktop.MainWindow is MainWindow mw3)
                    mw3.CloseChildWindows();
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

            _trayIcon.Clicked += (_, _) => ToggleMainWindow(desktop);

            // Update tooltip with training summary every 5 seconds
            _trayUpdateSub = AppServices.EventAggregator?.Subscribe<EveLens.Core.Events.FiveSecondTickEvent>(_ =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        if (_trayIcon != null)
                            _trayIcon.ToolTipText = BuildTrayTooltip();
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Tray tooltip update failed: {ex.Message}");
                    }
                });
            });
        }

        private void DestroyTrayIcon()
        {
            _traySettingsSub?.Dispose();
            _traySettingsSub = null;
            _trayUpdateSub?.Dispose();
            _trayUpdateSub = null;
            _trayIcon?.Dispose();
            _trayIcon = null;
            _trayMenu = null;
        }

        private static string BuildTrayTooltip()
        {
            var characters = AppServices.Characters;
            if (characters == null || !characters.Any())
                return AppServices.ProductNameWithVersion;

            var trainingChars = characters
                .OfType<CCPCharacter>()
                .Where(c => c.IsTraining && c.CurrentlyTrainingSkill != null)
                .ToList();

            if (trainingChars.Count == 0)
            {
                int total = characters.Count();
                return $"EveLens — {total} character{(total != 1 ? "s" : "")}, none training";
            }

            var display = Settings.UI.SystemTrayTooltip.Display;

            string countText = $"{trainingChars.Count} training";

            var next = trainingChars
                .OrderBy(c => c.CurrentlyTrainingSkill!.EndTime)
                .First();
            var skill = next.CurrentlyTrainingSkill!;
            string timeStr = TimeFormatHelper.FormatRemaining(skill.RemainingTime);
            string name = next.Name.Length > 16 ? next.Name[..16] + "..." : next.Name;
            string nextText = $"{name} — {skill.SkillName} {skill.Level} ({timeStr})";

            return display switch
            {
                TrayTooltipDisplay.TrainingCountOnly => $"EveLens — {countText}",
                TrayTooltipDisplay.NextFinisherOnly => $"EveLens — {nextText}",
                _ => trainingChars.Count == 1
                    ? $"EveLens — {next.Name}: {skill.SkillName} {skill.Level} ({timeStr})"
                    : $"EveLens — {countText} | Next: {nextText}",
            };
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

        private static void ToggleMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
        {
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    var window = desktop.MainWindow;
                    if (window == null)
                        return;

                    if (window.IsVisible && window.WindowState != WindowState.Minimized)
                        window.Hide();
                    else
                        ShowMainWindow(desktop);
                }
                catch (Exception ex)
                {
                    AppServices.TraceService?.Trace(
                        $"ToggleMainWindow failed: {ex.Message}", printMethod: false);
                }
            });
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

        /// <summary>
        /// Registers EveLens with the Linux desktop environment on first launch.
        /// Copies the .desktop file and icon to ~/.local/share so the desktop
        /// shows the correct EveLens icon instead of a generic gear.
        /// </summary>
        private static void RegisterLinuxDesktopEntry()
        {
            string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string desktopDir = Path.Combine(home, ".local", "share", "applications");
            string iconDir = Path.Combine(home, ".local", "share", "icons", "hicolor", "256x256", "apps");
            string desktopFile = Path.Combine(desktopDir, "evelens.desktop");
            string iconFile = Path.Combine(iconDir, "evelens.png");

            // Skip if already registered
            if (File.Exists(desktopFile) && File.Exists(iconFile))
                return;

            // Find the icon from the app's base directory
            string? appDir = AppContext.BaseDirectory;
            string? iconSource = null;

            // AppImage: icon is in usr/share/icons relative to the AppDir
            string appImageIcon = Path.Combine(appDir, "..", "usr", "share", "icons",
                "hicolor", "256x256", "apps", "evelens.png");
            if (File.Exists(appImageIcon))
                iconSource = Path.GetFullPath(appImageIcon);

            // Portable: icon might be alongside the binary
            if (iconSource == null)
            {
                string localIcon = Path.Combine(appDir, "evelens.png");
                if (File.Exists(localIcon))
                    iconSource = localIcon;
            }

            if (iconSource == null)
            {
                AppServices.TraceService?.Trace("Linux desktop: no icon found, skipping registration", printMethod: false);
                return;
            }

            // Copy icon
            Directory.CreateDirectory(iconDir);
            File.Copy(iconSource, iconFile, true);

            // Write .desktop file pointing to the actual executable
            string execPath = Environment.ProcessPath ?? Path.Combine(appDir, "EveLens");
            Directory.CreateDirectory(desktopDir);
            File.WriteAllText(desktopFile,
                $"""
                [Desktop Entry]
                Type=Application
                Name=EveLens
                Comment=Character Intelligence for EVE Online
                Exec={execPath}
                Icon=evelens
                Categories=Game;Utility;
                Terminal=false
                StartupWMClass=EveLens
                """);

            AppServices.TraceService?.Trace("Linux desktop: registered .desktop and icon", printMethod: false);
        }
    }
}
