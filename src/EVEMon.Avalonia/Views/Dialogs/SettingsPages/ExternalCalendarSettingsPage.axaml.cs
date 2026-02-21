// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class ExternalCalendarSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public ExternalCalendarSettingsPage()
        {
            InitializeComponent();
        }

        public ExternalCalendarSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            EnabledCheckBox.IsCheckedChanged += (_, _) =>
                CalendarOptionsPanel.IsEnabled = EnabledCheckBox.IsChecked == true;

            GoogleRadio.IsCheckedChanged += (_, _) => UpdateProviderPanels();
            OutlookRadio.IsCheckedChanged += (_, _) => UpdateProviderPanels();
        }

        private void LoadFromSettings()
        {
            var cal = _settings.Calendar;

            EnabledCheckBox.IsChecked = cal.Enabled;
            CalendarOptionsPanel.IsEnabled = cal.Enabled;

            // Provider
            if (cal.Provider == CalendarProvider.Google)
                GoogleRadio.IsChecked = true;
            else
                OutlookRadio.IsChecked = true;

            // Google settings
            GoogleCalNameTextBox.Text = cal.GoogleCalendarName ?? string.Empty;
            GoogleReminderCombo.SelectedIndex = (int)cal.GoogleEventReminder;

            // Outlook settings
            OutlookDefaultCalCheckBox.IsChecked = cal.UseOutlookDefaultCalendar;
            OutlookCalPathTextBox.Text = cal.OutlookCustomCalendarPath ?? string.Empty;

            // Reminders
            UseRemindingCheckBox.IsChecked = cal.UseReminding;
            RemindingInterval.Value = cal.RemindingInterval;
            UseAltRemindingCheckBox.IsChecked = cal.UseAlternateReminding;
            EarlyTimePicker.SelectedTime = cal.EarlyReminding.TimeOfDay;
            LateTimePicker.SelectedTime = cal.LateReminding.TimeOfDay;

            LastQueuedSkillOnlyCheckBox.IsChecked = cal.LastQueuedSkillOnly;

            UpdateProviderPanels();
        }

        private void UpdateProviderPanels()
        {
            GooglePanel.IsVisible = GoogleRadio.IsChecked == true;
            OutlookPanel.IsVisible = OutlookRadio.IsChecked == true;
        }

        public void ApplyToSettings()
        {
            var cal = _settings.Calendar;

            cal.Enabled = EnabledCheckBox.IsChecked == true;

            // Provider
            cal.Provider = GoogleRadio.IsChecked == true
                ? CalendarProvider.Google
                : CalendarProvider.Outlook;

            // Google settings
            cal.GoogleCalendarName = GoogleCalNameTextBox.Text ?? string.Empty;
            cal.GoogleEventReminder = (GoogleCalendarReminder)Math.Max(0, GoogleReminderCombo.SelectedIndex);

            // Outlook settings
            cal.UseOutlookDefaultCalendar = OutlookDefaultCalCheckBox.IsChecked == true;
            cal.OutlookCustomCalendarPath = OutlookCalPathTextBox.Text ?? string.Empty;

            // Reminders
            cal.UseReminding = UseRemindingCheckBox.IsChecked == true;
            cal.RemindingInterval = (int)(RemindingInterval.Value ?? 10);
            cal.UseAlternateReminding = UseAltRemindingCheckBox.IsChecked == true;
            cal.EarlyReminding = DateTime.Today.Add(EarlyTimePicker.SelectedTime ?? TimeSpan.FromHours(8));
            cal.LateReminding = DateTime.Today.Add(LateTimePicker.SelectedTime ?? TimeSpan.FromHours(20));

            cal.LastQueuedSkillOnly = LastQueuedSkillOnlyCheckBox.IsChecked == true;
        }
    }
}
