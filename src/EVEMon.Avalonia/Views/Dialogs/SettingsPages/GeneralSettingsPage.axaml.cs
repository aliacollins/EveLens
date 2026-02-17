using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class GeneralSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public GeneralSettingsPage()
        {
            InitializeComponent();
        }

        public GeneralSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
            OpenDataDirButton.Click += OnOpenDataDirClick;
        }

        private void LoadFromSettings()
        {
            SafeForWorkCheckBox.IsChecked = _settings.UI.SafeForWork;
            CompatibilityCombo.SelectedIndex = (int)_settings.Compatibility;
        }

        public void ApplyToSettings()
        {
            _settings.UI.SafeForWork = SafeForWorkCheckBox.IsChecked == true;
            _settings.Compatibility = (CompatibilityMode)Math.Max(0, CompatibilityCombo.SelectedIndex);
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
