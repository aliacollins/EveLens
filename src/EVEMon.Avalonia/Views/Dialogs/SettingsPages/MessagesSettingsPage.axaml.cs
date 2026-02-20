using Avalonia.Controls;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class MessagesSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public MessagesSettingsPage()
        {
            InitializeComponent();
        }

        public MessagesSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            switch (_settings.UI.PlanWindow.ObsoleteEntryRemovalBehaviour)
            {
                case ObsoleteEntryRemovalBehaviour.AlwaysAsk:
                    AlwaysAskRadio.IsChecked = true;
                    break;
                case ObsoleteEntryRemovalBehaviour.RemoveConfirmed:
                    RemoveConfirmedRadio.IsChecked = true;
                    break;
                case ObsoleteEntryRemovalBehaviour.RemoveAll:
                    RemoveAllRadio.IsChecked = true;
                    break;
            }
        }

        public void ApplyToSettings()
        {
            if (AlwaysAskRadio.IsChecked == true)
                _settings.UI.PlanWindow.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.AlwaysAsk;
            else if (RemoveConfirmedRadio.IsChecked == true)
                _settings.UI.PlanWindow.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.RemoveConfirmed;
            else if (RemoveAllRadio.IsChecked == true)
                _settings.UI.PlanWindow.ObsoleteEntryRemovalBehaviour = ObsoleteEntryRemovalBehaviour.RemoveAll;
        }
    }
}
