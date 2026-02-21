// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using Avalonia.Controls;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class IconsSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public IconsSettingsPage()
        {
            InitializeComponent();
        }

        public IconsSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            // IconsGroupIndex is 1-based; ComboBox is 0-based
            IconSetCombo.SelectedIndex = Math.Max(0, _settings.UI.SkillBrowser.IconsGroupIndex - 1);
        }

        public void ApplyToSettings()
        {
            _settings.UI.SkillBrowser.IconsGroupIndex = Math.Max(0, IconSetCombo.SelectedIndex) + 1;
        }
    }
}
