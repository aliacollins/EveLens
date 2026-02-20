using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class TrayIconSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public TrayIconSettingsPage()
        {
            InitializeComponent();
        }

        public TrayIconSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            TrayDisabledRadio.IsCheckedChanged += OnTrayBehaviourChanged;
            TrayMinimizedRadio.IsCheckedChanged += OnTrayBehaviourChanged;
            TrayAlwaysRadio.IsCheckedChanged += OnTrayBehaviourChanged;
        }

        private void LoadFromSettings()
        {
            // Tray icon behaviour
            switch (_settings.UI.SystemTrayIcon)
            {
                case SystemTrayBehaviour.Disabled:
                    TrayDisabledRadio.IsChecked = true;
                    break;
                case SystemTrayBehaviour.ShowWhenMinimized:
                    TrayMinimizedRadio.IsChecked = true;
                    break;
                case SystemTrayBehaviour.AlwaysVisible:
                    TrayAlwaysRadio.IsChecked = true;
                    break;
            }

            // Close behaviour
            switch (_settings.UI.MainWindowCloseBehaviour)
            {
                case CloseBehaviour.Exit:
                    CloseExitRadio.IsChecked = true;
                    break;
                case CloseBehaviour.MinimizeToTray:
                    CloseMinTrayRadio.IsChecked = true;
                    break;
                case CloseBehaviour.MinimizeToTaskbar:
                    CloseMinTaskbarRadio.IsChecked = true;
                    break;
            }

            // Popup style
            switch (_settings.UI.SystemTrayPopup.Style)
            {
                case TrayPopupStyles.PopupForm:
                    PopupFormRadio.IsChecked = true;
                    break;
                case TrayPopupStyles.WindowsTooltip:
                    PopupTooltipRadio.IsChecked = true;
                    break;
                case TrayPopupStyles.Disabled:
                    PopupDisabledRadio.IsChecked = true;
                    break;
            }

            UpdateDisables();
        }

        private void OnTrayBehaviourChanged(object? sender, RoutedEventArgs e)
        {
            UpdateDisables();
        }

        private void UpdateDisables()
        {
            // "Minimize to Tray" disabled when tray icon is Disabled
            CloseMinTrayRadio.IsEnabled = TrayDisabledRadio.IsChecked != true;

            // If tray disabled and minimize-to-tray was selected, switch to exit
            if (TrayDisabledRadio.IsChecked == true && CloseMinTrayRadio.IsChecked == true)
            {
                CloseExitRadio.IsChecked = true;
            }
        }

        public void ApplyToSettings()
        {
            // Tray icon behaviour
            if (TrayDisabledRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.Disabled;
            else if (TrayMinimizedRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.ShowWhenMinimized;
            else if (TrayAlwaysRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible;

            // Close behaviour
            if (CloseExitRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.Exit;
            else if (CloseMinTrayRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray;
            else if (CloseMinTaskbarRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTaskbar;

            // Popup style
            if (PopupFormRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.PopupForm;
            else if (PopupTooltipRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.WindowsTooltip;
            else if (PopupDisabledRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.Disabled;
        }
    }
}
