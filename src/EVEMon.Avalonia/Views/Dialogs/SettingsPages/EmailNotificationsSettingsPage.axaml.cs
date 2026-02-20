using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class EmailNotificationsSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;
        private bool _isUpdating;

        public EmailNotificationsSettingsPage()
        {
            InitializeComponent();
        }

        public EmailNotificationsSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            SendMailCheckBox.IsCheckedChanged += (_, _) =>
                MailOptionsPanel.IsEnabled = SendMailCheckBox.IsChecked == true;

            AuthRequiredCheckBox.IsCheckedChanged += (_, _) =>
                AuthPanel.IsEnabled = AuthRequiredCheckBox.IsChecked == true;

            ProviderCombo.SelectionChanged += OnProviderSelectionChanged;
        }

        private void LoadFromSettings()
        {
            _isUpdating = true;
            var n = _settings.Notifications;

            SendMailCheckBox.IsChecked = n.SendMailAlert;
            MailOptionsPanel.IsEnabled = n.SendMailAlert;

            // Provider
            int providerIdx = n.EmailSmtpServerProvider switch
            {
                "Gmail" => 0,
                "Outlook" => 1,
                "Yahoo" => 2,
                "Custom" => 3,
                _ => 4 // Default
            };
            ProviderCombo.SelectedIndex = providerIdx;

            SmtpServerTextBox.Text = n.EmailSmtpServerAddress ?? string.Empty;
            PortNumber.Value = n.EmailPortNumber;
            RequiresSslCheckBox.IsChecked = n.EmailServerRequiresSsl;

            AuthRequiredCheckBox.IsChecked = n.EmailAuthenticationRequired;
            AuthPanel.IsEnabled = n.EmailAuthenticationRequired;
            AuthUserTextBox.Text = n.EmailAuthenticationUserName ?? string.Empty;
            AuthPasswordTextBox.Text = n.EmailAuthenticationPassword ?? string.Empty;

            FromAddressTextBox.Text = n.EmailFromAddress ?? string.Empty;
            ToAddressTextBox.Text = n.EmailToAddress ?? string.Empty;

            ShortFormatCheckBox.IsChecked = n.UseEmailShortFormat;
            _isUpdating = false;
        }

        private void OnProviderSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating)
                return;

            switch (ProviderCombo.SelectedIndex)
            {
                case 0: // Gmail
                    SmtpServerTextBox.Text = "smtp.gmail.com";
                    PortNumber.Value = 587;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
                case 1: // Outlook
                    SmtpServerTextBox.Text = "smtp-mail.outlook.com";
                    PortNumber.Value = 587;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
                case 2: // Yahoo
                    SmtpServerTextBox.Text = "smtp.mail.yahoo.com";
                    PortNumber.Value = 465;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
                // Custom and Default: leave as-is
            }
        }

        public void ApplyToSettings()
        {
            var n = _settings.Notifications;

            n.SendMailAlert = SendMailCheckBox.IsChecked == true;

            n.EmailSmtpServerProvider = ProviderCombo.SelectedIndex switch
            {
                0 => "Gmail",
                1 => "Outlook",
                2 => "Yahoo",
                3 => "Custom",
                _ => "Default"
            };

            n.EmailSmtpServerAddress = SmtpServerTextBox.Text ?? string.Empty;
            n.EmailPortNumber = (int)(PortNumber.Value ?? 25);
            n.EmailServerRequiresSsl = RequiresSslCheckBox.IsChecked == true;

            n.EmailAuthenticationRequired = AuthRequiredCheckBox.IsChecked == true;
            n.EmailAuthenticationUserName = AuthUserTextBox.Text ?? string.Empty;
            n.EmailAuthenticationPassword = AuthPasswordTextBox.Text ?? string.Empty;

            n.EmailFromAddress = FromAddressTextBox.Text ?? string.Empty;
            n.EmailToAddress = ToAddressTextBox.Text ?? string.Empty;

            n.UseEmailShortFormat = ShortFormatCheckBox.IsChecked == true;
        }
    }
}
