// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using Avalonia.Controls;
using EVEMon.Common.MarketPricer;
using EVEMon.Common.Serialization.Settings;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class MarketPriceProvidersSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;

        public MarketPriceProvidersSettingsPage()
        {
            InitializeComponent();
        }

        public MarketPriceProvidersSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();
        }

        private void LoadFromSettings()
        {
            ProviderCombo.Items.Clear();

            var providers = ItemPricer.Providers.Select(p => p.Name).ToList();
            foreach (var name in providers)
                ProviderCombo.Items.Add(name);

            int idx = providers.IndexOf(_settings.MarketPricer.ProviderName);
            ProviderCombo.SelectedIndex = idx >= 0 ? idx : 0;
        }

        public void ApplyToSettings()
        {
            if (ProviderCombo.SelectedItem is string name)
                _settings.MarketPricer.ProviderName = name;
        }
    }
}
