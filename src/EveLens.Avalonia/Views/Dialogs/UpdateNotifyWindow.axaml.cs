// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;
using EveLens.Common;
using EveLens.Common.CustomEventArgs;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class UpdateNotifyWindow : Window
    {
        private UpdateAvailableEventArgs? _args;

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

                    // Version header
                    var versionHeader = new TextBlock
                    {
                        Text = $"v{release.Version}",
                        FontSize = 12,
                        FontWeight = FontWeight.SemiBold,
                        Foreground = (IBrush)Application.Current!.FindResource("EveAccentPrimaryBrush")!,
                        Margin = new Thickness(0, i > 0 ? 12 : 0, 0, 2)
                    };
                    ReleaseNotesPanel.Children.Add(versionHeader);

                    // Date
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

                    // Message body
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

                    // Separator between entries (not after last)
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
                // Fallback: single message (backward compat)
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

        private void OnDownloadClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_args?.InstallerUrl != null)
                {
                    Util.OpenURL(_args.InstallerUrl);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening installer URL: {ex}");
            }

            Close();
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
    }
}
