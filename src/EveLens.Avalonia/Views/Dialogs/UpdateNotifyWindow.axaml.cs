// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Threading;
using EveLens.Common;
using EveLens.Common.CustomEventArgs;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class UpdateNotifyWindow : Window
    {
        private UpdateAvailableEventArgs? _args;
        private CancellationTokenSource? _downloadCts;

        public UpdateNotifyWindow()
        {
            InitializeComponent();

            DownloadBtn.Click += OnDownloadClick;
            RemindBtn.Click += (_, _) => Close();
            SkipBtn.Click += OnSkipClick;
        }

        public void Initialize(UpdateAvailableEventArgs args)
        {
            _args = args;
            CurrentVersionText.Text = $"v{args.CurrentVersion}";
            NewVersionText.Text = $"v{args.NewestVersion}";

            // Show "Download & Install" for auto-installable, "View Release" otherwise
            if (!args.CanAutoInstall)
                DownloadBtn.Content = "View Release";

            BuildReleaseNotes(args);
        }

        private void BuildReleaseNotes(UpdateAvailableEventArgs args)
        {
            ReleaseNotesPanel.Children.Clear();

            if (args.ReleaseHistory.Count > 0)
            {
                for (int i = 0; i < args.ReleaseHistory.Count; i++)
                {
                    var release = args.ReleaseHistory[i];

                    var versionHeader = new TextBlock
                    {
                        Text = $"v{release.Version}",
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = (IBrush)Application.Current!.FindResource("EveAccentPrimaryBrush")!,
                        Margin = new Thickness(0, i > 0 ? 12 : 0, 0, 2)
                    };
                    ReleaseNotesPanel.Children.Add(versionHeader);

                    if (!string.IsNullOrWhiteSpace(release.Date))
                    {
                        var dateText = new TextBlock
                        {
                            Text = release.Date,
                            FontSize = 10,
                            Foreground = (IBrush)Application.Current!.FindResource("EveTextDisabledBrush")!,
                            Margin = new Thickness(0, 0, 0, 4)
                        };
                        ReleaseNotesPanel.Children.Add(dateText);
                    }

                    if (!string.IsNullOrWhiteSpace(release.Message))
                    {
                        var messageText = new TextBlock
                        {
                            Text = release.Message,
                            FontSize = 11,
                            TextWrapping = TextWrapping.Wrap,
                            LineHeight = 18,
                            Foreground = (IBrush)Application.Current!.FindResource("EveTextPrimaryBrush")!,
                            Margin = new Thickness(0, 0, 0, 4)
                        };
                        ReleaseNotesPanel.Children.Add(messageText);
                    }

                    if (i < args.ReleaseHistory.Count - 1)
                    {
                        var separator = new Border
                        {
                            Height = 1,
                            Background = (IBrush)Application.Current!.FindResource("EveBorderBrush")!,
                            Margin = new Thickness(0, 8, 0, 0)
                        };
                        ReleaseNotesPanel.Children.Add(separator);
                    }
                }
            }
            else
            {
                var fallbackText = new TextBlock
                {
                    Text = string.IsNullOrWhiteSpace(args.UpdateMessage)
                        ? "No release notes available."
                        : args.UpdateMessage,
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    LineHeight = 18,
                    Foreground = (IBrush)Application.Current!.FindResource("EveTextSecondaryBrush")!
                };
                ReleaseNotesPanel.Children.Add(fallbackText);
            }
        }

        private async void OnDownloadClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_args == null) return;

                // If can't auto-install, fall back to opening browser
                if (!_args.CanAutoInstall)
                {
                    if (_args.InstallerUrl != null)
                        Util.OpenURL(_args.InstallerUrl);
                    Close();
                    return;
                }

                // Switch to download mode
                ButtonPanel.IsVisible = false;
                ProgressPanel.IsVisible = true;
                ProgressText.Text = "Downloading update...";

                _downloadCts = new CancellationTokenSource();

                var progress = new Progress<double>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        DownloadProgress.Value = p * 100;
                        int pct = (int)(p * 100);
                        ProgressText.Text = pct < 100
                            ? $"Downloading... {pct}%"
                            : "Download complete. Applying update...";
                    });
                });

                string downloadedFile = await AutoUpdateService.DownloadAsync(
                    _args.InstallerUrl, progress, _downloadCts.Token);

                // Apply and restart
                ProgressText.Text = "Applying update...";
                AutoUpdateService.ApplyAndRestart(downloadedFile, _args.AutoInstallArguments);
            }
            catch (OperationCanceledException)
            {
                // Download cancelled — restore buttons
                ButtonPanel.IsVisible = true;
                ProgressPanel.IsVisible = false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Auto-update failed: {ex}");
                ProgressText.Text = $"Update failed: {ex.Message}";
                ProgressText.Foreground = (IBrush?)Application.Current?.FindResource("EveErrorRedBrush");

                // Restore buttons after a delay so user can try again or dismiss
                ButtonPanel.IsVisible = true;
            }
        }

        private void OnSkipClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_args != null)
                {
                    Common.Settings.Updates.MostRecentDeniedUpgrade = _args.NewestVersion.ToString();
                    Common.Settings.Save();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving denied upgrade: {ex}");
            }

            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            _downloadCts?.Cancel();
            _downloadCts?.Dispose();
            base.OnClosed(e);
        }
    }
}
