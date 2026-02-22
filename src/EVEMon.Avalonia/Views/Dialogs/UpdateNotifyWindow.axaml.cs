// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.CustomEventArgs;

namespace EVEMon.Avalonia.Views.Dialogs
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
            ReleaseNotesText.Text = string.IsNullOrWhiteSpace(args.UpdateMessage)
                ? "No release notes available."
                : args.UpdateMessage;
        }

        private void OnDownloadClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (_args?.InstallerUrl != null)
                {
                    Process.Start(new ProcessStartInfo(_args.InstallerUrl.ToString())
                    {
                        UseShellExecute = true
                    });
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
