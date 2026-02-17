using Avalonia.Controls;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class UpdatesSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public UpdatesSettingsPage()
        {
            InitializeComponent();
        }

        public UpdatesSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            CheckForUpdatesCheckBox.IsChecked = _settings.Updates.CheckEVEMonVersion;
            CheckTimeCheckBox.IsChecked = _settings.Updates.CheckTimeOnStartup;
        }

        public void ApplyToSettings()
        {
            _settings.Updates.CheckEVEMonVersion = CheckForUpdatesCheckBox.IsChecked == true;
            _settings.Updates.CheckTimeOnStartup = CheckTimeCheckBox.IsChecked == true;
        }
    }
}
