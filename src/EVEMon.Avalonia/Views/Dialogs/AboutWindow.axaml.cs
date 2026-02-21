// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
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
                VersionText.Text = $"Version {version} | {bitness}";
                CopyrightText.Text = BuildInfo.Copyright;
            }
            catch
            {
                VersionText.Text = "Version information unavailable";
                CopyrightText.Text = BuildInfo.Copyright;
            }
        }

        private void PopulateContributors()
        {
            var goldBrush = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold;
            var textBrush = (IBrush?)Application.Current?.FindResource("EveTextPrimaryBrush") ?? Brushes.White;
            var dimBrush = (IBrush?)Application.Current?.FindResource("EveTextSecondaryBrush") ?? Brushes.Gray;

            // Header
            AddSectionHeader("CONTRIBUTORS");
            AddName("Originally by Six Anari", dimBrush);
            AddSpacer(8);

            // Active Developer
            AddSectionHeader("ACTIVE DEVELOPER");
            AddName("Alia Collins", goldBrush);
            AddSpacer(8);

            // Developers (Retired)
            AddSectionHeader("DEVELOPERS (RETIRED)");
            foreach (var name in new[]
            {
                "Peter Han", "Blitz Bandis", "Jimi", "Araan Sunn",
                "Six Anari", "Anders Chydenius", "Brad Stone",
                "Eewec Ourbyni", "Richard Slater", "Vehlin",
                "Collin Grady", "DCShadow", "DonQuiche", "Grauw",
                "Jalon Mevek", "Labogh", "romanl", "Safrax",
                "Stevil Knevil", "TheBelgarion"
            })
            {
                AddName(name, textBrush);
            }
            AddSpacer(8);

            // Consultants
            AddSectionHeader("CONSULTANTS");
            foreach (var name in new[]
            {
                "Desmont McCallock", "Tonto Aansen", "Saeka Tansen",
                "MrCue", "Candle"
            })
            {
                AddName(name, textBrush);
            }
            AddSpacer(8);

            // Community Contributors
            AddSectionHeader("COMMUNITY");
            foreach (var name in new[]
            {
                "InfinitasX", "Adrienne Adler", "Torgo",
                "alebrophy", "DiagonalyStraight", "Lukas Friedrichsen"
            })
            {
                AddName(name, textBrush);
            }
            AddSpacer(8);

            // AI
            AddSectionHeader("AI PARTNER");
            AddName("Claude (Anthropic)", textBrush);
        }

        private void AddSectionHeader(string text)
        {
            var goldBrush = (IBrush?)Application.Current?.FindResource("EveAccentPrimaryBrush") ?? Brushes.Gold;
            ContributorsPanel.Children.Add(new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = goldBrush,
                Margin = new Thickness(0, 4, 0, 2)
            });
        }

        private void AddName(string name, IBrush foreground)
        {
            ContributorsPanel.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 11,
                Foreground = foreground,
                Margin = new Thickness(8, 1, 0, 1)
            });
        }

        private void AddSpacer(double height)
        {
            ContributorsPanel.Children.Add(new Border { Height = height });
        }

        private void WireEvents()
        {
            OkButton.Click += (_, _) => Close();
            WebsiteLink.Click += (_, _) => OpenUrl(BuildInfo.Website);
            GitHubLink.Click += (_, _) => OpenUrl(BuildInfo.Repository);
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
