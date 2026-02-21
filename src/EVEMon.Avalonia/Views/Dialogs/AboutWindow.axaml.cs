// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            PopulateVersionInfo();
            PopulateContributors();
            WireEvents();
        }

        private void PopulateVersionInfo()
        {
            try
            {
                var fvi = AppServices.FileVersionInfo;
                string version = AppServices.IsDebugBuild
                    ? $"{fvi.FileVersion} (Debug)"
                    : fvi.ProductVersion ?? fvi.FileVersion ?? "Unknown";

                string bitness = Environment.Is64BitProcess ? "64-bit" : "32-bit";
                VersionText.Text = $"v{version} | {bitness}";
                BuildInfoText.Text = $".NET {Environment.Version} | Avalonia";
                CopyrightText.Text = BuildInfo.Copyright;
            }
            catch
            {
                VersionText.Text = "Version information unavailable";
                BuildInfoText.Text = string.Empty;
                CopyrightText.Text = BuildInfo.Copyright;
            }
        }

        private void PopulateContributors()
        {
            var contributors = new List<(string Name, string Role)>
            {
                ("Alia Collins", "EVEMon NexT lead"),
                ("Six Anari", "Original creator"),
                ("Jimi C", "Lead developer"),
                ("stillfront", "Core team"),
                ("Peter Han", "ESI maintainer"),
                ("Desmont McCallock", "ESI implementation"),
                ("Tonto Aansen", "Contributor"),
                ("Saeka Tansen", "Contributor"),
                ("MrCue", "Contributor"),
                ("Candle", "Contributor"),
                ("InfinitasX", "Community maintainer"),
                ("Adrienne Adler", "Contributor"),
                ("Torgo", "Contributor"),
                ("alebrophy", "Contributor"),
                ("DiagonalyStraight", "Contributor"),
                ("Lukas Friedrichsen", "Contributor"),
                ("Claude (Anthropic)", "AI development partner"),
            };

            var goldBrush = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold;
            var textBrush = (IBrush?)Application.Current?.FindResource("EveTextPrimaryBrush") ?? Brushes.White;
            var dimBrush = (IBrush?)Application.Current?.FindResource("EveTextDisabledBrush") ?? Brushes.Gray;
            var bgBrush = (IBrush?)Application.Current?.FindResource("EveBackgroundMediumBrush") ?? Brushes.DarkGray;

            var panel = new WrapPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            foreach (var (name, role) in contributors)
            {
                var initial = new TextBlock
                {
                    Text = name[0].ToString(),
                    FontSize = 9,
                    FontWeight = FontWeight.Bold,
                    Foreground = goldBrush,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var initialCircle = new Border
                {
                    Width = 18, Height = 18,
                    CornerRadius = new CornerRadius(9),
                    Background = bgBrush,
                    Child = initial
                };

                var nameText = new TextBlock
                {
                    Text = name,
                    FontSize = 11,
                    Foreground = textBrush,
                    VerticalAlignment = VerticalAlignment.Center
                };

                var chip = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 5,
                    Children = { initialCircle, nameText }
                };

                var chipBorder = new Border
                {
                    Padding = new Thickness(4, 3, 10, 3),
                    Margin = new Thickness(3),
                    CornerRadius = new CornerRadius(12),
                    Background = Brushes.Transparent,
                    BorderBrush = (IBrush?)Application.Current?.FindResource("EveBorderBrush") ?? Brushes.DarkGray,
                    BorderThickness = new Thickness(1),
                    Child = chip,
                    [ToolTip.TipProperty] = role
                };

                panel.Children.Add(chipBorder);
            }

            ContributorsList.Items.Add(panel);
        }

        private void WireEvents()
        {
            OkButton.Click += (_, _) => Close();
            WebsiteLink.Click += (_, _) => OpenUrl(BuildInfo.Website);
            GitHubLink.Click += (_, _) => OpenUrl(BuildInfo.Repository);
            IssuesLink.Click += (_, _) => OpenUrl(BuildInfo.Repository + "/issues");
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
            }
            catch
            {
                // Silently fail if browser cannot be opened
            }
        }
    }
}
