// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using EveLens.Avalonia.Services;
using EveLens.Common;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.MarketPricer;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class SettingsWindow : Window
    {
        private readonly SerializableSettings _settings;
        private string _initialTheme = string.Empty;
        private bool _isUpdating;

        // Sidebar navigation: section name → (content panel, nav button, keywords)
        private readonly Dictionary<string, (StackPanel Panel, Button NavButton, string[] Keywords)> _sectionMap = new();
        private readonly List<string> _sectionNames = new();
        private string _activeSection = string.Empty;

        public SettingsWindow()
        {
            InitializeComponent();

            // Work on a copy of settings (same pattern as WinForms SettingsForm)
            _settings = Settings.Export();

            BuildSectionMap();
            LoadSettings();
            WireEvents();

            // Select first section on open
            SelectSection("Appearance");
        }

        private void BuildSectionMap()
        {
            AddSection("Appearance", AppearancePanel, NavAppearance,
                new[] { "appearance", "theme", "safe for work", "compatibility", "wine", "data directory" });
            AddSection("Window", WindowPanel, NavWindow,
                new[] { "window", "behavior", "behaviour", "tray", "icon", "close", "minimize",
                        "system tray" });
            AddSection("Notifications", NotificationsPanel, NavNotifications,
                new[] { "notification", "notifications", "sound", "skill", "email", "mail", "smtp",
                        "calendar", "google", "outlook", "reminder", "toast", "alert" });
            AddSection("Data", DataPanel, NavData,
                new[] { "data", "update", "updates", "market", "price", "provider", "clock" });
            AddSection("Network", NetworkPanel, NavNetwork,
                new[] { "network", "proxy", "sso", "client", "secret", "credentials", "http" });
            AddSection("ESI", EsiPanel, NavEsi,
                new[] { "esi", "scope", "scopes", "api", "oauth", "authenticate" });
        }

        private void AddSection(string name, StackPanel panel, Button navButton, string[] keywords)
        {
            _sectionMap[name] = (panel, navButton, keywords);
            _sectionNames.Add(name);
        }

        private void SelectSection(string name)
        {
            if (!_sectionMap.ContainsKey(name))
                return;

            _activeSection = name;

            foreach (var sn in _sectionNames)
            {
                var (panel, navButton, _) = _sectionMap[sn];
                bool isActive = sn == name;

                panel.IsVisible = isActive;

                if (isActive)
                    navButton.Classes.Add("active");
                else
                    navButton.Classes.Remove("active");
            }

            // Reset scroll to top when switching sections
            ContentScroller.Offset = new Vector(0, 0);
        }

        private void LoadSettings()
        {
            _isUpdating = true;

            // --- Appearance ---
            PopulateThemeCombo();
            SafeForWorkToggle.IsChecked = _settings.UI.SafeForWork;
            FontScaleSlider.Value = _settings.UI.FontScalePercent;
            FontScaleLabel.Text = $"{_settings.UI.FontScalePercent}%";
            FontScaleSlider.PropertyChanged += (_, e) =>
            {
                if (_isUpdating || e.Property.Name != "Value") return;
                int pct = (int)FontScaleSlider.Value;
                FontScaleLabel.Text = $"{pct}%";
                _settings.UI.FontScalePercent = pct;
                FontScaleService.Apply(pct);
                Settings.Save();
            };

            // --- Window Behavior ---
            LoadTraySettings();

            // --- Notifications ---
            OsNotificationsToggle.IsChecked = _settings.Notifications.ShowOSNotifications;
            PlaySoundToggle.IsChecked = _settings.Notifications.PlaySoundOnSkillCompletion;
            LoadEmailSettings();
            LoadCalendarSettings();

            // --- Data & Updates ---
            CheckForUpdatesToggle.IsChecked = _settings.Updates.CheckEveLensVersion;
            CheckTimeToggle.IsChecked = _settings.Updates.CheckTimeOnStartup;
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
            MinimizeToTrayToggle.IsChecked = _settings.UI.MinimizeToTray;

            TrayTooltipDisplayCombo.ItemsSource = new[]
            {
                "Training Count + Next Finisher",
                "Training Count Only",
                "Next Finisher Only"
            };
            TrayTooltipDisplayCombo.SelectedIndex = (int)_settings.UI.SystemTrayTooltip.Display;
        }

        private void LoadEmailSettings()
        {
            var n = _settings.Notifications;

            SendMailToggle.IsChecked = n.SendMailAlert;
            EmailOptionsPanel.IsVisible = n.SendMailAlert;

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
            RequiresSslToggle.IsChecked = n.EmailServerRequiresSsl;

            EmailAuthToggle.IsChecked = n.EmailAuthenticationRequired;
            EmailAuthPanel.IsVisible = n.EmailAuthenticationRequired;
            EmailAuthUserTextBox.Text = n.EmailAuthenticationUserName ?? string.Empty;
            EmailAuthPasswordTextBox.Text = n.EmailAuthenticationPassword ?? string.Empty;

            EmailFromTextBox.Text = n.EmailFromAddress ?? string.Empty;
            EmailToTextBox.Text = n.EmailToAddress ?? string.Empty;
            EmailShortFormatToggle.IsChecked = n.UseEmailShortFormat;
        }

        private void LoadCalendarSettings()
        {
            var cal = _settings.Calendar;

            CalendarEnabledToggle.IsChecked = cal.Enabled;
            CalendarOptionsPanel.IsVisible = cal.Enabled;

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
            UseProxyToggle.IsChecked = _settings.Proxy.Enabled;
            ProxyPanel.IsVisible = _settings.Proxy.Enabled;
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
            LoadCharacterScopeTable();
        }

        private void LoadCharacterScopeTable()
        {
            var characters = AppServices.Characters;
            if (characters == null || !characters.Any())
            {
                NoCharactersLabel.IsVisible = true;
                CharacterScopePanel.Children.Clear();
                return;
            }

            NoCharactersLabel.IsVisible = false;
            string currentGlobalPreset = GetSelectedEsiPresetKey();

            var groups = new Dictionary<string, CharacterScopeGroupEntry>();

            foreach (var character in characters)
            {
                var ccpChar = character as Common.Models.CCPCharacter;
                if (ccpChar == null) continue;

                var key = ccpChar.Identity?.ESIKeys.FirstOrDefault(k => k.Monitored);
                string detectedKey;

                if (key == null || key.AuthorizedScopes.Count == 0)
                    detectedKey = "NoScopes";
                else
                    detectedKey = EsiScopePresets.DetectPreset(key.AuthorizedScopes);

                if (!groups.TryGetValue(detectedKey, out var group))
                {
                    string groupDisplayName;
                    if (detectedKey == "NoScopes")
                        groupDisplayName = "No Scopes";
                    else if (EsiScopePresets.PresetDisplayNames.TryGetValue(detectedKey, out string? gdn))
                        groupDisplayName = gdn;
                    else
                        groupDisplayName = detectedKey;

                    group = new CharacterScopeGroupEntry
                    {
                        GroupName = groupDisplayName,
                        PresetKey = detectedKey,
                        IsCurrentPreset = detectedKey == currentGlobalPreset
                    };
                    groups[detectedKey] = group;
                }

                bool needsReauth = detectedKey != currentGlobalPreset;

                group.Characters.Add(new CharacterScopeEntry
                {
                    Name = character.Name ?? "Unknown",
                    NeedsReauth = needsReauth,
                    Character = ccpChar
                });
            }

            foreach (var g in groups.Values)
                g.CharacterCount = g.Characters.Count;

            var ordered = groups.Values
                .OrderByDescending(g => g.IsCurrentPreset)
                .ThenBy(g => g.GroupName)
                .ToList();

            BuildCharacterScopeGroups(ordered);
        }

        private void BuildCharacterScopeGroups(List<CharacterScopeGroupEntry> groups)
        {
            CharacterScopePanel.Children.Clear();

            foreach (var group in groups)
            {
                // Group header
                var headerGrid = new Grid
                {
                    ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                    Margin = new Thickness(0, 4, 0, 2)
                };

                var headerLeft = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6 };
                headerLeft.Children.Add(new TextBlock
                {
                    Text = group.GroupName,
                    FontSize = FontScaleService.Body,
                    FontWeight = FontWeight.SemiBold,
                    Foreground = FindBrush("EveAccentPrimaryBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });
                headerLeft.Children.Add(new TextBlock
                {
                    Text = $"({group.CharacterCount})",
                    FontSize = FontScaleService.Small,
                    Foreground = FindBrush("EveTextDisabledBrush"),
                    VerticalAlignment = VerticalAlignment.Center
                });
                Grid.SetColumn(headerLeft, 0);
                headerGrid.Children.Add(headerLeft);

                if (group.IsCurrentPreset)
                {
                    var checkMark = new TextBlock
                    {
                        Text = "\u2713",
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveSuccessGreenBrush"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(checkMark, 1);
                    headerGrid.Children.Add(checkMark);
                }

                CharacterScopePanel.Children.Add(headerGrid);

                // Character rows
                foreach (var entry in group.Characters)
                {
                    var rowGrid = new Grid
                    {
                        ColumnDefinitions = ColumnDefinitions.Parse("*,Auto"),
                        Margin = new Thickness(12, 2, 0, 2)
                    };

                    var nameBlock = new TextBlock
                    {
                        Text = entry.Name,
                        FontSize = FontScaleService.Body,
                        Foreground = FindBrush("EveTextPrimaryBrush"),
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    Grid.SetColumn(nameBlock, 0);
                    rowGrid.Children.Add(nameBlock);

                    if (entry.NeedsReauth)
                    {
                        var reauthBtn = new Button
                        {
                            Content = "Re-authenticate",
                            FontSize = FontScaleService.Small,
                            Padding = new Thickness(8, 2),
                            CornerRadius = new CornerRadius(10),
                            Foreground = FindBrush("EveWarningYellowBrush"),
                            Background = Brushes.Transparent,
                            BorderThickness = new Thickness(1),
                            BorderBrush = FindBrush("EveWarningYellowBrush"),
                            Tag = entry.Character,
                            Cursor = new global::Avalonia.Input.Cursor(
                                global::Avalonia.Input.StandardCursorType.Hand)
                        };
                        reauthBtn.Click += OnReauthCharacterClick;
                        Grid.SetColumn(reauthBtn, 1);
                        rowGrid.Children.Add(reauthBtn);
                    }

                    CharacterScopePanel.Children.Add(rowGrid);
                }
            }
        }

        private async void OnReauthCharacterClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                // Apply the current preset selection so ESI scopes are correct during re-auth
                string presetKey = GetSelectedEsiPresetKey();
                Settings.EsiScopePreset = presetKey;

                var dialog = new AddCharacterWindow();
                await dialog.ShowDialog(this);

                if (dialog.CharacterImported)
                    LoadCharacterScopeTable();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error re-authenticating: {ex}");
            }
        }

        private static IBrush FindBrush(string resourceKey)
        {
            return (IBrush?)Application.Current?.FindResource(resourceKey) ?? Brushes.Gray;
        }

        private sealed class CharacterScopeEntry
        {
            public string Name { get; set; } = string.Empty;
            public bool NeedsReauth { get; set; }
            public Common.Models.CCPCharacter? Character { get; set; }
        }

        private sealed class CharacterScopeGroupEntry
        {
            public string GroupName { get; set; } = string.Empty;
            public string PresetKey { get; set; } = string.Empty;
            public int CharacterCount { get; set; }
            public bool IsCurrentPreset { get; set; }
            public List<CharacterScopeEntry> Characters { get; set; } = new();
        }

        private void WireEvents()
        {
            // Search
            SearchBox.TextChanged += OnSearchTextChanged;
            SearchClearButton.Click += (_, _) => { SearchBox.Text = string.Empty; };

            // Sidebar navigation
            foreach (var sn in _sectionNames)
            {
                var sectionName = sn; // capture for closure
                _sectionMap[sn].NavButton.Click += (_, _) => SelectSection(sectionName);
            }

            // Buttons
            SaveButton.Click += OnSaveClick;
            CancelButton.Click += OnCancelClick;

            // Appearance
            ThemeCombo.SelectionChanged += OnThemeSelectionChanged;
            RestartNowButton.Click += OnRestartNowClick;
            OpenDataDirButton.Click += OnOpenDataDirClick;

            // Email — master toggle reveals/hides sub-panel
            SendMailToggle.IsCheckedChanged += (_, _) =>
                EmailOptionsPanel.IsVisible = SendMailToggle.IsChecked == true;
            EmailAuthToggle.IsCheckedChanged += (_, _) =>
                EmailAuthPanel.IsVisible = EmailAuthToggle.IsChecked == true;
            EmailProviderCombo.SelectionChanged += OnEmailProviderSelectionChanged;

            // Calendar — master toggle reveals/hides sub-panel
            CalendarEnabledToggle.IsCheckedChanged += (_, _) =>
                CalendarOptionsPanel.IsVisible = CalendarEnabledToggle.IsChecked == true;
            GoogleCalendarRadio.IsCheckedChanged += (_, _) => UpdateCalendarProviderPanels();
            OutlookCalendarRadio.IsCheckedChanged += (_, _) => UpdateCalendarProviderPanels();

            // Proxy — master toggle reveals/hides sub-panel
            UseProxyToggle.IsCheckedChanged += (_, _) =>
                ProxyPanel.IsVisible = UseProxyToggle.IsChecked == true;
            UseDefaultCredentialsButton.Click += (_, _) =>
            {
                SsoClientIdTextBox.Text = string.Empty;
                SsoClientSecretTextBox.Text = string.Empty;
            };

            // ESI scopes
            EsiPresetCombo.SelectionChanged += OnEsiPresetSelectionChanged;
            CustomizeScopesButton.Click += OnCustomizeScopesClick;
        }

        // --- Search ---

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            string query = (SearchBox.Text ?? string.Empty).Trim().ToLowerInvariant();
            SearchClearButton.IsVisible = !string.IsNullOrEmpty(query);

            if (string.IsNullOrEmpty(query))
            {
                // Show all sidebar buttons and restore active section
                foreach (var sn in _sectionNames)
                    _sectionMap[sn].NavButton.IsVisible = true;

                if (!string.IsNullOrEmpty(_activeSection))
                    SelectSection(_activeSection);
                return;
            }

            // Filter sidebar buttons by keyword match, auto-select first match
            string? firstMatch = null;
            foreach (var sn in _sectionNames)
            {
                var (_, navButton, keywords) = _sectionMap[sn];
                bool matches = keywords.Any(k => k.Contains(query));
                navButton.IsVisible = matches;

                if (matches && firstMatch == null)
                    firstMatch = sn;
            }

            if (firstMatch != null)
                SelectSection(firstMatch);
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
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    Process.Start(new ProcessStartInfo("xdg-open", path) { UseShellExecute = false });
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                    Process.Start(new ProcessStartInfo("open", path) { UseShellExecute = false });
                else
                    Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
            }
            catch
            {
                // Silently fail if directory cannot be opened
            }
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
                    RequiresSslToggle.IsChecked = true;
                    break;
                case 1: // Outlook
                    SmtpServerTextBox.Text = "smtp-mail.outlook.com";
                    EmailPortNumber.Value = 587;
                    RequiresSslToggle.IsChecked = true;
                    break;
                case 2: // Yahoo
                    SmtpServerTextBox.Text = "smtp.mail.yahoo.com";
                    EmailPortNumber.Value = 465;
                    RequiresSslToggle.IsChecked = true;
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
            LoadCharacterScopeTable();
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
            _settings.UI.SafeForWork = SafeForWorkToggle.IsChecked == true;

            int selectedThemeIndex = Math.Max(0, ThemeCombo.SelectedIndex);
            if (selectedThemeIndex < ThemeManager.AvailableThemes.Count)
            {
                string themeName = ThemeManager.AvailableThemes[selectedThemeIndex].Name;
                _settings.UI.ThemeName = themeName;
                ThemeManager.WriteThemePreference(themeName);
            }

            // Window Behavior — single MinimizeToTray toggle
            _settings.UI.MinimizeToTray = MinimizeToTrayToggle.IsChecked == true;
            if (TrayTooltipDisplayCombo.SelectedIndex >= 0)
                _settings.UI.SystemTrayTooltip.Display = (TrayTooltipDisplay)TrayTooltipDisplayCombo.SelectedIndex;

            // Notifications
            _settings.Notifications.ShowOSNotifications = OsNotificationsToggle.IsChecked == true;
            _settings.Notifications.PlaySoundOnSkillCompletion = PlaySoundToggle.IsChecked == true;

            // Email
            var n = _settings.Notifications;
            n.SendMailAlert = SendMailToggle.IsChecked == true;
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
            n.EmailServerRequiresSsl = RequiresSslToggle.IsChecked == true;
            n.EmailAuthenticationRequired = EmailAuthToggle.IsChecked == true;
            n.EmailAuthenticationUserName = EmailAuthUserTextBox.Text ?? string.Empty;
            n.EmailAuthenticationPassword = EmailAuthPasswordTextBox.Text ?? string.Empty;
            n.EmailFromAddress = EmailFromTextBox.Text ?? string.Empty;
            n.EmailToAddress = EmailToTextBox.Text ?? string.Empty;
            n.UseEmailShortFormat = EmailShortFormatToggle.IsChecked == true;

            // Calendar
            var cal = _settings.Calendar;
            cal.Enabled = CalendarEnabledToggle.IsChecked == true;
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
            _settings.Updates.CheckEveLensVersion = CheckForUpdatesToggle.IsChecked == true;
            _settings.Updates.CheckTimeOnStartup = CheckTimeToggle.IsChecked == true;

            if (MarketPriceProviderCombo.SelectedItem is string provName)
                _settings.MarketPricer.ProviderName = provName;

            // Network
            _settings.Proxy.Enabled = UseProxyToggle.IsChecked == true;
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
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };
            var cancelBtn = new Button
            {
                Content = "Cancel",
                FontSize = FontScaleService.Body,
                Padding = new Thickness(12, 5),
                CornerRadius = new CornerRadius(12)
            };

            var dialog = new Window
            {
                Title = "Restart EveLens",
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
                            Text = "EveLens will restart to apply the new theme.\nPlease save any work in progress (e.g., skill plans being edited).",
                            TextWrapping = TextWrapping.Wrap,
                            FontSize = FontScaleService.Subheading,
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
