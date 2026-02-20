using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.Dialogs.SettingsPages
{
    public partial class EsiScopeSettingsPage : UserControl
    {
        private readonly SerializableSettings _settings = null!;
        private bool _isUpdating;

        public EsiScopeSettingsPage()
        {
            InitializeComponent();
        }

        public EsiScopeSettingsPage(SerializableSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            LoadFromSettings();

            PresetCombo.SelectionChanged += OnPresetSelectionChanged;
            CustomizeScopesButton.Click += OnCustomizeScopesClick;
        }

        private void LoadFromSettings()
        {
            _isUpdating = true;

            PresetCombo.Items.Clear();

            // Add preset options
            foreach (string key in EsiScopePresets.PresetKeys)
            {
                if (EsiScopePresets.PresetDisplayNames.TryGetValue(key, out string? displayName))
                    PresetCombo.Items.Add(displayName);
            }

            // Add Custom option
            if (EsiScopePresets.PresetDisplayNames.TryGetValue(EsiScopePresets.Custom, out string? customName))
                PresetCombo.Items.Add(customName);

            // Select current preset
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
                selectedIndex = PresetCombo.Items.Count - 1;

            if (selectedIndex < PresetCombo.Items.Count)
                PresetCombo.SelectedIndex = selectedIndex;

            UpdateDescription();
            _isUpdating = false;
        }

        private string GetSelectedPresetKey()
        {
            int index = PresetCombo.SelectedIndex;
            if (index < 0)
                return EsiScopePresets.FullMonitoring;

            if (index < EsiScopePresets.PresetKeys.Count)
                return EsiScopePresets.PresetKeys[index];

            return EsiScopePresets.Custom;
        }

        private void UpdateDescription()
        {
            string presetKey = GetSelectedPresetKey();

            if (presetKey == EsiScopePresets.Custom)
            {
                PresetDescription.Text = EsiScopePresets.GetCustomDescription(
                    _settings.EsiCustomScopes);
            }
            else if (EsiScopePresets.PresetDescriptions.TryGetValue(presetKey, out string? desc))
            {
                PresetDescription.Text = desc;
            }
            else
            {
                PresetDescription.Text = string.Empty;
            }
        }

        private void OnPresetSelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_isUpdating)
                return;

            UpdateDescription();
        }

        private async void OnCustomizeScopesClick(object? sender, RoutedEventArgs e)
        {
            try
            {
                string presetKey = GetSelectedPresetKey();
                HashSet<string> currentScopes;

                if (presetKey == EsiScopePresets.Custom && _settings.EsiCustomScopes.Count > 0)
                    currentScopes = new HashSet<string>(_settings.EsiCustomScopes);
                else
                    currentScopes = EsiScopePresets.GetScopesForPreset(presetKey);

                var editor = new EsiScopeEditorWindow(currentScopes);
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel is Window parentWindow)
                {
                    await editor.ShowDialog(parentWindow);
                }
                else
                {
                    editor.Show();
                }

                if (!editor.DialogResult)
                    return;

                // Update settings with results
                string detectedPreset = editor.SelectedPreset;
                _settings.EsiScopePreset = detectedPreset;

                _settings.EsiCustomScopes.Clear();
                if (detectedPreset == EsiScopePresets.Custom)
                {
                    foreach (string scope in editor.SelectedScopes)
                        _settings.EsiCustomScopes.Add(scope);
                }

                // Update combo to match
                _isUpdating = true;
                for (int i = 0; i < EsiScopePresets.PresetKeys.Count; i++)
                {
                    if (EsiScopePresets.PresetKeys[i] == detectedPreset)
                    {
                        PresetCombo.SelectedIndex = i;
                        break;
                    }
                }

                if (detectedPreset == EsiScopePresets.Custom)
                    PresetCombo.SelectedIndex = PresetCombo.Items.Count - 1;

                _isUpdating = false;
                UpdateDescription();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error opening scope editor: {ex}");
            }
        }

        public void ApplyToSettings()
        {
            string presetKey = GetSelectedPresetKey();
            _settings.EsiScopePreset = presetKey;

            // Custom scopes are already updated by the editor dialog
            // For non-custom presets, clear custom scopes
            if (presetKey != EsiScopePresets.Custom)
            {
                _settings.EsiCustomScopes.Clear();
            }
        }
    }
}
