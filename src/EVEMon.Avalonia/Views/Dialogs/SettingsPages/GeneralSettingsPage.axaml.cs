// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Diagnostics;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Avalonia.Services;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class GeneralSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;
        private string _initialTheme = string.Empty;

        public event Action? RestartRequested;

        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        public GeneralSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            PopulateThemeCombo();
            LoadFromSettings();
            OpenDataDirButton.Click += OnOpenDataDirClick;
            ThemeCombo.SelectionChanged += OnThemeSelectionChanged;
            RestartNowButton.Click += OnRestartNowClick;
        }

        private void PopulateThemeCombo()
        {
            foreach (var (_, displayName) in ThemeManager.AvailableThemes)
                ThemeCombo.Items.Add(new ComboBoxItem { Content = displayName });
        }

        private void LoadFromSettings()
        {
            SafeForWorkCheckBox.IsChecked = _settings.UI.SafeForWork;
            CompatibilityCombo.SelectedIndex = (int)_settings.Compatibility;

            // Select the current theme in the combo
            _initialTheme = ThemeManager.CurrentTheme;
            int themeIndex = ThemeManager.AvailableThemes
                .Select((t, i) => (t.Name, Index: i))
                .FirstOrDefault(x => x.Name == _initialTheme).Index;
            ThemeCombo.SelectedIndex = themeIndex;
        }

        public void ApplyToSettings()
        {
            _settings.UI.SafeForWork = SafeForWorkCheckBox.IsChecked == true;
            _settings.Compatibility = (CompatibilityMode)Math.Max(0, CompatibilityCombo.SelectedIndex);

            // Persist theme selection
            int selectedIndex = Math.Max(0, ThemeCombo.SelectedIndex);
            if (selectedIndex < ThemeManager.AvailableThemes.Count)
            {
                string themeName = ThemeManager.AvailableThemes[selectedIndex].Name;
                _settings.UI.ThemeName = themeName;
                ThemeManager.WriteThemePreference(themeName);
            }
        }

        private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ThemeCombo.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= ThemeManager.AvailableThemes.Count)
                return;

            string selectedTheme = ThemeManager.AvailableThemes[selectedIndex].Name;
            ThemeRestartPanel.IsVisible = selectedTheme != _initialTheme;
        }

        private void OnRestartNowClick(object? sender, RoutedEventArgs e)
        {
            RestartRequested?.Invoke();
        }

        private static void OnOpenDataDirClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string path = AppServices.ApplicationPaths.DataDirectory;
                Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
                // Silently fail if directory cannot be opened
            }
        }
    }
}
