// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class G15SettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public G15SettingsPage()
        {
            InitializeComponent();
        }

        public G15SettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            EnabledCheckBox.IsCheckedChanged += (_, _) =>
                G15OptionsPanel.IsEnabled = EnabledCheckBox.IsChecked == true;
        }

        private void LoadFromSettings()
        {
            EnabledCheckBox.IsChecked = _settings.G15.Enabled;
            UseCharCycleCheckBox.IsChecked = _settings.G15.UseCharactersCycle;
            CharCycleInterval.Value = _settings.G15.CharactersCycleInterval;
            UseTimeCycleCheckBox.IsChecked = _settings.G15.UseTimeFormatsCycle;
            TimeCycleInterval.Value = _settings.G15.TimeFormatsCycleInterval;
            ShowSystemTimeCheckBox.IsChecked = _settings.G15.ShowSystemTime;
            ShowEVETimeCheckBox.IsChecked = _settings.G15.ShowEVETime;

            G15OptionsPanel.IsEnabled = _settings.G15.Enabled;
        }

        public void ApplyToSettings()
        {
            _settings.G15.Enabled = EnabledCheckBox.IsChecked == true;
            _settings.G15.UseCharactersCycle = UseCharCycleCheckBox.IsChecked == true;
            _settings.G15.CharactersCycleInterval = (int)(CharCycleInterval.Value ?? 20);
            _settings.G15.UseTimeFormatsCycle = UseTimeCycleCheckBox.IsChecked == true;
            _settings.G15.TimeFormatsCycleInterval = (int)(TimeCycleInterval.Value ?? 10);
            _settings.G15.ShowSystemTime = ShowSystemTimeCheckBox.IsChecked == true;
            _settings.G15.ShowEVETime = ShowEVETimeCheckBox.IsChecked == true;
        }
    }
}
