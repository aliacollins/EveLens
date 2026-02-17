using Avalonia.Controls;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class NotificationsSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public NotificationsSettingsPage()
        {
            InitializeComponent();
        }

        public NotificationsSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            PlaySoundOnSkillCompleteCheckBox.IsChecked = _settings.Notifications.PlaySoundOnSkillCompletion;
        }

        public void ApplyToSettings()
        {
            _settings.Notifications.PlaySoundOnSkillCompletion = PlaySoundOnSkillCompleteCheckBox.IsChecked == true;
        }
    }
}
