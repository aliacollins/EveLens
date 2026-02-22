// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EVEMon.Avalonia.Services;
using EVEMon.Common;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.MarketPricer;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs
{
    public partial class SettingsWindow : Window
    {
        private readonly SerializableSettings _settings;
        private string _initialTheme = string.Empty;
        private bool _isUpdating;

        // Section expanders + keywords for search filtering
        private readonly List<(Expander Expander, string[] Keywords)> _sections = new();

        // Section expanders + nav buttons + chevron indicators for jump navigation
        private readonly List<(Expander Expander, Button NavButton, TextBlock Chevron)> _navPairs = new();

        public SettingsWindow()
        {
            InitializeComponent();

            // Work on a copy of settings (same pattern as WinForms SettingsForm)
            _settings = Settings.Export();

            BuildSectionMaps();
            LoadSettings();
            WireEvents();
        }

        private void BuildSectionMaps()
        {
            _sections.Add((AppearanceExpander, new[]
            {
                "appearance", "theme", "safe for work", "compatibility", "wine", "data directory"
            }));
            _sections.Add((WindowBehaviorExpander, new[]
            {
                "window", "behavior", "behaviour", "tray", "icon", "close", "minimize",
                "popup", "taskbar", "system tray"
            }));
            _sections.Add((NotificationsExpander, new[]
            {
                "notification", "notifications", "sound", "skill", "email", "mail", "smtp",
                "calendar", "google", "outlook", "reminder", "toast", "alert"
            }));
            _sections.Add((DataUpdatesExpander, new[]
            {
                "data", "update", "updates", "market", "price", "provider", "clock"
            }));
            _sections.Add((NetworkExpander, new[]
            {
                "network", "proxy", "sso", "client", "secret", "credentials", "http"
            }));
            _sections.Add((EsiScopesExpander, new[]
            {
                "esi", "scope", "scopes", "api", "oauth", "authenticate"
            }));

            _navPairs.Add((AppearanceExpander, NavAppearance, AppearanceChevron));
            _navPairs.Add((WindowBehaviorExpander, NavWindow, WindowChevron));
            _navPairs.Add((NotificationsExpander, NavNotifications, NotificationsChevron));
            _navPairs.Add((DataUpdatesExpander, NavData, DataChevron));
            _navPairs.Add((NetworkExpander, NavNetwork, NetworkChevron));
            _navPairs.Add((EsiScopesExpander, NavEsi, EsiChevron));
        }

        private void LoadSettings()
        {
            _isUpdating = true;

            // --- Appearance ---
            PopulateThemeCombo();
            SafeForWorkCheckBox.IsChecked = _settings.UI.SafeForWork;
            CompatibilityCombo.SelectedIndex = (int)_settings.Compatibility;

            // --- Window Behavior ---
            LoadTraySettings();

            // --- Notifications ---
            OsNotificationsCheckBox.IsChecked = _settings.Notifications.ShowOSNotifications;
            PlaySoundCheckBox.IsChecked = _settings.Notifications.PlaySoundOnSkillCompletion;
            LoadEmailSettings();
            LoadCalendarSettings();

            // --- Data & Updates ---
            CheckForUpdatesCheckBox.IsChecked = _settings.Updates.CheckEVEMonVersion;
            CheckTimeCheckBox.IsChecked = _settings.Updates.CheckTimeOnStartup;
            PopulateMarketPriceProviders();

            // --- Network ---
            LoadNetworkSettings();

            // --- ESI Scopes ---
            LoadEsiScopeSettings();

            _isUpdating = false;
        }

        private void PopulateThemeCombo()
        {
            foreach (var (_, displayName) in ThemeManager.AvailableThemes)
                ThemeCombo.Items.Add(new ComboBoxItem { Content = displayName });

            _initialTheme = ThemeManager.CurrentTheme;
            int themeIndex = ThemeManager.AvailableThemes
                .Select((t, i) => (t.Name, Index: i))
                .FirstOrDefault(x => x.Name == _initialTheme).Index;
            ThemeCombo.SelectedIndex = themeIndex;
        }

        private void LoadTraySettings()
        {
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

            UpdateTrayDisables();
        }

        private void LoadEmailSettings()
        {
            var n = _settings.Notifications;

            SendMailCheckBox.IsChecked = n.SendMailAlert;
            EmailOptionsPanel.IsEnabled = n.SendMailAlert;

            int providerIdx = n.EmailSmtpServerProvider switch
            {
                "Gmail" => 0,
                "Outlook" => 1,
                "Yahoo" => 2,
                "Custom" => 3,
                _ => 4
            };
            EmailProviderCombo.SelectedIndex = providerIdx;
            SmtpServerTextBox.Text = n.EmailSmtpServerAddress ?? string.Empty;
            EmailPortNumber.Value = n.EmailPortNumber;
            RequiresSslCheckBox.IsChecked = n.EmailServerRequiresSsl;

            EmailAuthCheckBox.IsChecked = n.EmailAuthenticationRequired;
            EmailAuthPanel.IsEnabled = n.EmailAuthenticationRequired;
            EmailAuthUserTextBox.Text = n.EmailAuthenticationUserName ?? string.Empty;
            EmailAuthPasswordTextBox.Text = n.EmailAuthenticationPassword ?? string.Empty;

            EmailFromTextBox.Text = n.EmailFromAddress ?? string.Empty;
            EmailToTextBox.Text = n.EmailToAddress ?? string.Empty;
            EmailShortFormatCheckBox.IsChecked = n.UseEmailShortFormat;
        }

        private void LoadCalendarSettings()
        {
            var cal = _settings.Calendar;

            CalendarEnabledCheckBox.IsChecked = cal.Enabled;
            CalendarOptionsPanel.IsEnabled = cal.Enabled;

            if (cal.Provider == CalendarProvider.Google)
                GoogleCalendarRadio.IsChecked = true;
            else
                OutlookCalendarRadio.IsChecked = true;

            GoogleCalNameTextBox.Text = cal.GoogleCalendarName ?? string.Empty;
            GoogleReminderCombo.SelectedIndex = (int)cal.GoogleEventReminder;

            OutlookDefaultCalCheckBox.IsChecked = cal.UseOutlookDefaultCalendar;
            OutlookCalPathTextBox.Text = cal.OutlookCustomCalendarPath ?? string.Empty;

            CalendarRemindingCheckBox.IsChecked = cal.UseReminding;
            CalendarRemindingInterval.Value = cal.RemindingInterval;
            CalendarAltRemindingCheckBox.IsChecked = cal.UseAlternateReminding;
            CalendarEarlyTimePicker.SelectedTime = cal.EarlyReminding.TimeOfDay;
            CalendarLateTimePicker.SelectedTime = cal.LateReminding.TimeOfDay;

            CalendarLastQueuedOnlyCheckBox.IsChecked = cal.LastQueuedSkillOnly;

            UpdateCalendarProviderPanels();
        }

        private void PopulateMarketPriceProviders()
        {
            MarketPriceProviderCombo.Items.Clear();

            var providers = ItemPricer.Providers.Select(p => p.Name).ToList();
            foreach (var name in providers)
                MarketPriceProviderCombo.Items.Add(name);

            int idx = providers.IndexOf(_settings.MarketPricer.ProviderName);
            MarketPriceProviderCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        private void LoadNetworkSettings()
        {
            UseProxyCheckBox.IsChecked = _settings.Proxy.Enabled;
            ProxyPanel.IsEnabled = _settings.Proxy.Enabled;
            ProxyHostTextBox.Text = _settings.Proxy.Host ?? string.Empty;
            ProxyPortTextBox.Text = _settings.Proxy.Port.ToString();

            SsoClientIdTextBox.Text = _settings.SSOClientID ?? string.Empty;
            SsoClientSecretTextBox.Text = _settings.SSOClientSecret ?? string.Empty;
        }

        private void LoadEsiScopeSettings()
        {
            EsiPresetCombo.Items.Clear();

            foreach (string key in EsiScopePresets.PresetKeys)
            {
                if (EsiScopePresets.PresetDisplayNames.TryGetValue(key, out string? displayName))
                    EsiPresetCombo.Items.Add(displayName);
            }

            if (EsiScopePresets.PresetDisplayNames.TryGetValue(EsiScopePresets.Custom, out string? customName))
                EsiPresetCombo.Items.Add(customName);

            string currentPreset = _settings.EsiScopePreset ?? EsiScopePresets.FullMonitoring;
            int selectedIndex = 0;

            for (int i = 0; i < EsiScopePresets.PresetKeys.Count; i++)
            {
                if (EsiScopePresets.PresetKeys[i] == currentPreset)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (currentPreset == EsiScopePresets.Custom)
                selectedIndex = EsiPresetCombo.Items.Count - 1;

            if (selectedIndex < EsiPresetCombo.Items.Count)
                EsiPresetCombo.SelectedIndex = selectedIndex;

            UpdateEsiDescription();
        }

        private void WireEvents()
        {
            // Search
            SearchBox.TextChanged += OnSearchTextChanged;
            SearchClearButton.Click += (_, _) => { SearchBox.Text = string.Empty; };

            // Navigation buttons + chevron indicators
            foreach (var (expander, navButton, chevron) in _navPairs)
            {
                navButton.Click += (_, _) => ScrollToExpander(expander);

                // Update chevron when expand/collapse changes
                // ▾ = expanded (U+25BE), ▸ = collapsed (U+25B8)
                expander.PropertyChanged += (_, e) =>
                {
                    if (e.Property == Expander.IsExpandedProperty)
                        chevron.Text = expander.IsExpanded ? "\u25BE" : "\u25B8";
                };
            }

            // Buttons
            SaveButton.Click += OnSaveClick;
            CancelButton.Click += OnCancelClick;

            // Appearance
            ThemeCombo.SelectionChanged += OnThemeSelectionChanged;
            RestartNowButton.Click += OnRestartNowClick;
            OpenDataDirButton.Click += OnOpenDataDirClick;

            // Tray behavior
            TrayDisabledRadio.IsCheckedChanged += (_, _) => UpdateTrayDisables();
            TrayMinimizedRadio.IsCheckedChanged += (_, _) => UpdateTrayDisables();
            TrayAlwaysRadio.IsCheckedChanged += (_, _) => UpdateTrayDisables();

            // Email
            SendMailCheckBox.IsCheckedChanged += (_, _) =>
                EmailOptionsPanel.IsEnabled = SendMailCheckBox.IsChecked == true;
            EmailAuthCheckBox.IsCheckedChanged += (_, _) =>
                EmailAuthPanel.IsEnabled = EmailAuthCheckBox.IsChecked == true;
            EmailProviderCombo.SelectionChanged += OnEmailProviderSelectionChanged;

            // Calendar
            CalendarEnabledCheckBox.IsCheckedChanged += (_, _) =>
                CalendarOptionsPanel.IsEnabled = CalendarEnabledCheckBox.IsChecked == true;
            GoogleCalendarRadio.IsCheckedChanged += (_, _) => UpdateCalendarProviderPanels();
            OutlookCalendarRadio.IsCheckedChanged += (_, _) => UpdateCalendarProviderPanels();

            // Proxy
            UseProxyCheckBox.IsCheckedChanged += (_, _) =>
                ProxyPanel.IsEnabled = UseProxyCheckBox.IsChecked == true;
            UseDefaultCredentialsButton.Click += (_, _) =>
            {
                SsoClientIdTextBox.Text = string.Empty;
                SsoClientSecretTextBox.Text = string.Empty;
            };

            // ESI scopes
            EsiPresetCombo.SelectionChanged += OnEsiPresetSelectionChanged;
            CustomizeScopesButton.Click += OnCustomizeScopesClick;
        }

        // --- Navigation ---

        private void ScrollToExpander(Expander expander)
        {
            // Ensure the section is expanded
            expander.IsExpanded = true;

            // Scroll the expander into view
            // Use a brief delay so the layout can update after expansion
            global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var transform = expander.TransformToVisual(SectionsPanel);
                if (transform != null)
                {
                    var point = transform.Value.Transform(new Point(0, 0));
                    SettingsScroller.Offset = new Vector(0, Math.Max(0, point.Y));
                }
            }, global::Avalonia.Threading.DispatcherPriority.Loaded);
        }

        // --- Search ---

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            string query = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            SearchClearButton.IsVisible = !string.IsNullOrEmpty(query);

            if (string.IsNullOrEmpty(query))
            {
                // Show all sections and nav buttons
                foreach (var (expander, _) in _sections)
                    expander.IsVisible = true;
                foreach (var (_, navButton, _) in _navPairs)
                    navButton.IsVisible = true;
                return;
            }

            // Filter sections by keyword match
            foreach (var (expander, keywords) in _sections)
            {
                bool matches = keywords.Any(k => k.Contains(query));
                expander.IsVisible = matches;

                // Also expand matching sections so their content is visible
                if (matches)
                    expander.IsExpanded = true;
            }

            // Sync nav button visibility with section visibility
            foreach (var (expander, navButton, _) in _navPairs)
            {
                navButton.IsVisible = expander.IsVisible;
            }
        }

        // --- Theme ---

        private void OnThemeSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            int selectedIndex = ThemeCombo.SelectedIndex;
            if (selectedIndex < 0 || selectedIndex >= ThemeManager.AvailableThemes.Count)
                return;

            string selectedTheme = ThemeManager.AvailableThemes[selectedIndex].Name;
            ThemeRestartPanel.IsVisible = selectedTheme != _initialTheme;
        }

        private async void OnRestartNowClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                bool confirmed = await ShowRestartConfirmation();
                if (!confirmed)
                    return;

                CollectSettings();
                await Settings.ImportAsync(_settings, true);
                Close();
                AppServices.ApplicationLifecycle.Restart();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error during restart: {ex}");
            }
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

        // --- Tray ---

        private void UpdateTrayDisables()
        {
            CloseMinTrayRadio.IsEnabled = TrayDisabledRadio.IsChecked != true;

            if (TrayDisabledRadio.IsChecked == true && CloseMinTrayRadio.IsChecked == true)
                CloseExitRadio.IsChecked = true;
        }

        // --- Email provider presets ---

        private void OnEmailProviderSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating)
                return;

            switch (EmailProviderCombo.SelectedIndex)
            {
                case 0: // Gmail
                    SmtpServerTextBox.Text = "smtp.gmail.com";
                    EmailPortNumber.Value = 587;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
                case 1: // Outlook
                    SmtpServerTextBox.Text = "smtp-mail.outlook.com";
                    EmailPortNumber.Value = 587;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
                case 2: // Yahoo
                    SmtpServerTextBox.Text = "smtp.mail.yahoo.com";
                    EmailPortNumber.Value = 465;
                    RequiresSslCheckBox.IsChecked = true;
                    break;
            }
        }

        // --- Calendar ---

        private void UpdateCalendarProviderPanels()
        {
            GoogleCalendarPanel.IsVisible = GoogleCalendarRadio.IsChecked == true;
            OutlookCalendarPanel.IsVisible = OutlookCalendarRadio.IsChecked == true;
        }

        // --- ESI Scopes ---

        private string GetSelectedEsiPresetKey()
        {
            int index = EsiPresetCombo.SelectedIndex;
            if (index < 0)
                return EsiScopePresets.FullMonitoring;

            if (index < EsiScopePresets.PresetKeys.Count)
                return EsiScopePresets.PresetKeys[index];

            return EsiScopePresets.Custom;
        }

        private void UpdateEsiDescription()
        {
            string presetKey = GetSelectedEsiPresetKey();

            if (presetKey == EsiScopePresets.Custom)
            {
                EsiPresetDescription.Text = EsiScopePresets.GetCustomDescription(
                    _settings.EsiCustomScopes);
            }
            else if (EsiScopePresets.PresetDescriptions.TryGetValue(presetKey, out string? desc))
            {
                EsiPresetDescription.Text = desc;
            }
            else
            {
                EsiPresetDescription.Text = string.Empty;
            }
        }

        private void OnEsiPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating)
                return;

            UpdateEsiDescription();
        }

        private async void OnCustomizeScopesClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string presetKey = GetSelectedEsiPresetKey();
                HashSet<string> currentScopes;

                if (presetKey == EsiScopePresets.Custom && _settings.EsiCustomScopes.Count > 0)
                    currentScopes = new HashSet<string>(_settings.EsiCustomScopes);
                else
                    currentScopes = EsiScopePresets.GetScopesForPreset(presetKey);

                var editor = new EsiScopeEditorWindow(currentScopes);
                await editor.ShowDialog(this);

                if (!editor.DialogResult)
                    return;

                string detectedPreset = editor.SelectedPreset;
                _settings.EsiScopePreset = detectedPreset;

                _settings.EsiCustomScopes.Clear();
                if (detectedPreset == EsiScopePresets.Custom)
                {
                    foreach (string scope in editor.SelectedScopes)
                        _settings.EsiCustomScopes.Add(scope);
                }

                _isUpdating = true;
                for (int i = 0; i < EsiScopePresets.PresetKeys.Count; i++)
                {
                    if (EsiScopePresets.PresetKeys[i] == detectedPreset)
                    {
                        EsiPresetCombo.SelectedIndex = i;
                        break;
                    }
                }

                if (detectedPreset == EsiScopePresets.Custom)
                    EsiPresetCombo.SelectedIndex = EsiPresetCombo.Items.Count - 1;

                _isUpdating = false;
                UpdateEsiDescription();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening scope editor: {ex}");
            }
        }

        // --- Save / Cancel ---

        private void CollectSettings()
        {
            // Appearance
            _settings.UI.SafeForWork = SafeForWorkCheckBox.IsChecked == true;
            _settings.Compatibility = (CompatibilityMode)Math.Max(0, CompatibilityCombo.SelectedIndex);

            int selectedThemeIndex = Math.Max(0, ThemeCombo.SelectedIndex);
            if (selectedThemeIndex < ThemeManager.AvailableThemes.Count)
            {
                string themeName = ThemeManager.AvailableThemes[selectedThemeIndex].Name;
                _settings.UI.ThemeName = themeName;
                ThemeManager.WriteThemePreference(themeName);
            }

            // Window Behavior — Tray
            if (TrayDisabledRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.Disabled;
            else if (TrayMinimizedRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.ShowWhenMinimized;
            else if (TrayAlwaysRadio.IsChecked == true)
                _settings.UI.SystemTrayIcon = SystemTrayBehaviour.AlwaysVisible;

            if (CloseExitRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.Exit;
            else if (CloseMinTrayRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTray;
            else if (CloseMinTaskbarRadio.IsChecked == true)
                _settings.UI.MainWindowCloseBehaviour = CloseBehaviour.MinimizeToTaskbar;

            if (PopupFormRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.PopupForm;
            else if (PopupTooltipRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.WindowsTooltip;
            else if (PopupDisabledRadio.IsChecked == true)
                _settings.UI.SystemTrayPopup.Style = TrayPopupStyles.Disabled;

            // Notifications
            _settings.Notifications.ShowOSNotifications = OsNotificationsCheckBox.IsChecked == true;
            _settings.Notifications.PlaySoundOnSkillCompletion = PlaySoundCheckBox.IsChecked == true;

            // Email
            var n = _settings.Notifications;
            n.SendMailAlert = SendMailCheckBox.IsChecked == true;
            n.EmailSmtpServerProvider = EmailProviderCombo.SelectedIndex switch
            {
                0 => "Gmail",
                1 => "Outlook",
                2 => "Yahoo",
                3 => "Custom",
                _ => "Default"
            };
            n.EmailSmtpServerAddress = SmtpServerTextBox.Text ?? string.Empty;
            n.EmailPortNumber = (int)(EmailPortNumber.Value ?? 25);
            n.EmailServerRequiresSsl = RequiresSslCheckBox.IsChecked == true;
            n.EmailAuthenticationRequired = EmailAuthCheckBox.IsChecked == true;
            n.EmailAuthenticationUserName = EmailAuthUserTextBox.Text ?? string.Empty;
            n.EmailAuthenticationPassword = EmailAuthPasswordTextBox.Text ?? string.Empty;
            n.EmailFromAddress = EmailFromTextBox.Text ?? string.Empty;
            n.EmailToAddress = EmailToTextBox.Text ?? string.Empty;
            n.UseEmailShortFormat = EmailShortFormatCheckBox.IsChecked == true;

            // Calendar
            var cal = _settings.Calendar;
            cal.Enabled = CalendarEnabledCheckBox.IsChecked == true;
            cal.Provider = GoogleCalendarRadio.IsChecked == true
                ? CalendarProvider.Google
                : CalendarProvider.Outlook;
            cal.GoogleCalendarName = GoogleCalNameTextBox.Text ?? string.Empty;
            cal.GoogleEventReminder = (GoogleCalendarReminder)Math.Max(0, GoogleReminderCombo.SelectedIndex);
            cal.UseOutlookDefaultCalendar = OutlookDefaultCalCheckBox.IsChecked == true;
            cal.OutlookCustomCalendarPath = OutlookCalPathTextBox.Text ?? string.Empty;
            cal.UseReminding = CalendarRemindingCheckBox.IsChecked == true;
            cal.RemindingInterval = (int)(CalendarRemindingInterval.Value ?? 10);
            cal.UseAlternateReminding = CalendarAltRemindingCheckBox.IsChecked == true;
            cal.EarlyReminding = DateTime.Today.Add(CalendarEarlyTimePicker.SelectedTime ?? TimeSpan.FromHours(8));
            cal.LateReminding = DateTime.Today.Add(CalendarLateTimePicker.SelectedTime ?? TimeSpan.FromHours(20));
            cal.LastQueuedSkillOnly = CalendarLastQueuedOnlyCheckBox.IsChecked == true;

            // Data & Updates
            _settings.Updates.CheckEVEMonVersion = CheckForUpdatesCheckBox.IsChecked == true;
            _settings.Updates.CheckTimeOnStartup = CheckTimeCheckBox.IsChecked == true;

            if (MarketPriceProviderCombo.SelectedItem is string provName)
                _settings.MarketPricer.ProviderName = provName;

            // Network
            _settings.Proxy.Enabled = UseProxyCheckBox.IsChecked == true;
            _settings.Proxy.Host = ProxyHostTextBox.Text ?? string.Empty;
            if (int.TryParse(ProxyPortTextBox.Text, out int port) && port >= 0 && port <= 65535)
                _settings.Proxy.Port = port;
            _settings.SSOClientID = (SsoClientIdTextBox.Text ?? string.Empty).Trim();
            _settings.SSOClientSecret = (SsoClientSecretTextBox.Text ?? string.Empty).Trim();

            // ESI Scopes
            string presetKey = GetSelectedEsiPresetKey();
            _settings.EsiScopePreset = presetKey;
            if (presetKey != EsiScopePresets.Custom)
                _settings.EsiCustomScopes.Clear();
        }

        private async void OnSaveClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                CollectSettings();
                await Settings.ImportAsync(_settings, true);
                Close(true);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving settings: {ex}");
                Close(true);
            }
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            Close(false);
        }

        private async Task<bool> ShowRestartConfirmation()
        {
            bool result = false;

            var restartBtn = new Button
            {
                Content = "Restart",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };
            var cancelBtn = new Button
            {
                Content = "Cancel",
                FontSize = 11,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };

            var dialog = new Window
            {
                Title = "Restart EVEMon",
                Width = 420, Height = 170,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                CanResize = false,
                Content = new DockPanel
                {
                    Margin = new Thickness(16),
                    Children =
                    {
                        new StackPanel
                        {
                            Orientation = Orientation.Horizontal,
                            Spacing = 8,
                            HorizontalAlignment = HorizontalAlignment.Right,
                            Margin = new Thickness(0, 12, 0, 0),
                            [DockPanel.DockProperty] = Dock.Bottom,
                            Children = { restartBtn, cancelBtn }
                        },
                        new TextBlock
                        {
                            Text = "EVEMon will restart to apply the new theme.\nPlease save any work in progress (e.g., skill plans being edited).",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = 12,
                            VerticalAlignment = VerticalAlignment.Center
                        }
                    }
                }
            };

            restartBtn.Click += (_, _) => { result = true; dialog.Close(); };
            cancelBtn.Click += (_, _) => { dialog.Close(); };

            await dialog.ShowDialog(this);
            return result;
        }
    }
}
