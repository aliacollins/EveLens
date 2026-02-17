using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
            PopulateVersionInfo();
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
                VersionText.Text = $"Version {version}  |  {bitness}";
                BuildInfoText.Text = $".NET {Environment.Version}  |  Avalonia UI";
                CopyrightText.Text = fvi.LegalCopyright ?? "Copyright EVEMon Contributors";
            }
            catch
            {
                VersionText.Text = "Version information unavailable";
                BuildInfoText.Text = string.Empty;
                CopyrightText.Text = "Copyright EVEMon Contributors";
            }
        }

        private void WireEvents()
        {
            OkButton.Click += OnOkClick;
            WebsiteLink.Click += OnWebsiteLinkClick;
            GitHubLink.Click += OnGitHubLinkClick;
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            Close();
        }

        private static void OnWebsiteLinkClick(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://evemon.dev");
        }

        private static void OnGitHubLinkClick(object? sender, RoutedEventArgs e)
        {
            OpenUrl("https://github.com/aliacollins/evemon");
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
