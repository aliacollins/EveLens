// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using EveLens.Common;

namespace EveLens.Avalonia
{
    internal static class Program
    {
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern void SetCurrentProcessExplicitAppUserModelID(string appID);

        [STAThread]
        public static void Main(string[] args)
        {
            // Set taskbar identity so Windows groups the window as "EveLens"
            // instead of using the executable name "EveLens.Avalonia.exe"
            SetCurrentProcessExplicitAppUserModelID("EveLens");

            // When restarting (theme change, data update), the old instance needs time
            // to fully exit and release its named semaphore before we check it.
            if (args.Contains("--restart-delay"))
            {
                Thread.Sleep(2000);
                args = args.Where(a => a != "--restart-delay").ToArray();
            }

            // Single-instance check — prevent multiple EveLens sessions
            var instance = InstanceManager.Instance;
            if (!instance.CreatedNew)
            {
                instance.Signal();
                ShowAlreadyRunningDialog();
                return;
            }

            // Force Western number formatting (XXX,XXX.XX) regardless of system locale
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
            CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .LogToTrace();

        private static void ShowAlreadyRunningDialog()
        {
            AppBuilder.Configure<AlreadyRunningApp>()
                .UsePlatformDetect()
                .StartWithClassicDesktopLifetime(Array.Empty<string>());
        }
    }

    /// <summary>
    /// Minimal Avalonia app that shows a single "already running" dialog and exits.
    /// </summary>
    internal class AlreadyRunningApp : Application
    {
        public override void Initialize()
        {
            Styles.Add(new FluentTheme());
            RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Dark;
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new Window
                {
                    Title = "EveLens",
                    Width = 420,
                    Height = 200,
                    CanResize = false,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    Background = new SolidColorBrush(Color.Parse("#1A1A2E")),
                };

                var okButton = new Button
                {
                    Content = "OK",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Padding = new Thickness(24, 6),
                    FontSize = 12,
                    Cursor = new global::Avalonia.Input.Cursor(
                        global::Avalonia.Input.StandardCursorType.Hand),
                };
                okButton.Click += (_, _) => window.Close();

                window.Content = new StackPanel
                {
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Spacing = 6,
                    Children =
                    {
                        new TextBlock
                        {
                            Text = "EveLens is already running",
                            FontSize = 16,
                            FontWeight = FontWeight.SemiBold,
                            Foreground = new SolidColorBrush(Color.Parse("#E6A817")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                        },
                        new TextBlock
                        {
                            Text = "Check your taskbar or system tray for the existing session.",
                            FontSize = 12,
                            Foreground = new SolidColorBrush(Color.Parse("#AAAAAA")),
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(0, 0, 0, 12),
                        },
                        okButton,
                    },
                };

                desktop.MainWindow = window;
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
