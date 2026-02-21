using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class NetworkSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public NetworkSettingsPage()
        {
            InitializeComponent();
        }

        public NetworkSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
            UseProxyCheckBox.IsCheckedChanged += (_, _) => UpdateProxyPanelState();
            UseDefaultCredentialsButton.Click += OnUseDefaultCredentialsClick;
        }

        private void LoadFromSettings()
        {
            UseProxyCheckBox.IsChecked = _settings.Proxy.Enabled;
            ProxyHostTextBox.Text = _settings.Proxy.Host ?? string.Empty;
            ProxyPortTextBox.Text = _settings.Proxy.Port.ToString();
            ClientIdTextBox.Text = _settings.SSOClientID ?? string.Empty;
            ClientSecretTextBox.Text = _settings.SSOClientSecret ?? string.Empty;
            UpdateProxyPanelState();
        }

        private void UpdateProxyPanelState()
        {
            ProxyPanel.IsEnabled = UseProxyCheckBox.IsChecked == true;
        }

        private void OnUseDefaultCredentialsClick(object? sender, RoutedEventArgs e)
        {
            ClientIdTextBox.Text = string.Empty;
            ClientSecretTextBox.Text = string.Empty;
        }

        public void ApplyToSettings()
        {
            _settings.Proxy.Enabled = UseProxyCheckBox.IsChecked == true;
            _settings.Proxy.Host = ProxyHostTextBox.Text ?? string.Empty;

            if (int.TryParse(ProxyPortTextBox.Text, out int port) && port >= 0 && port <= 65535)
                _settings.Proxy.Port = port;

            _settings.SSOClientID = (ClientIdTextBox.Text ?? string.Empty).Trim();
            _settings.SSOClientSecret = (ClientSecretTextBox.Text ?? string.Empty).Trim();
        }
    }
}
