// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using EveLens.Common.Services;

namespace EveLens.Avalonia.Views.Dialogs
{
    public partial class EsiScopeEditorWindow : Window
    {
        private readonly Dictionary<string, CheckBox> _groupCheckBoxes = new();
        private readonly Dictionary<string, CheckBox> _scopeCheckBoxes = new();
        private bool _isUpdating;

        /// <summary>
        /// The scopes selected by the user when OK is clicked.
        /// </summary>
        public HashSet<string> SelectedScopes { get; private set; } = new();

        /// <summary>
        /// The detected preset for the current selection.
        /// </summary>
        public string SelectedPreset { get; private set; } = EsiScopePresets.FullMonitoring;

        /// <summary>
        /// Whether the user clicked OK (true) or Cancel (false).
        /// </summary>
        public bool DialogResult { get; private set; }

        public EsiScopeEditorWindow()
        {
            InitializeComponent();
        }

        public EsiScopeEditorWindow(HashSet<string> currentScopes) : this()
        {
            SelectedScopes = new HashSet<string>(currentScopes);
            BuildScopeTree();
            ApplyChecks(currentScopes);

            SelectAllButton.Click += OnSelectAllClick;
            ClearAllButton.Click += OnClearAllClick;
            OkButton.Click += OnOkClick;
            CancelButton.Click += OnCancelClick;
        }

        private void BuildScopeTree()
        {
            ScopePanel.Children.Clear();
            _groupCheckBoxes.Clear();
            _scopeCheckBoxes.Clear();

            foreach (var group in EsiScopePresets.FeatureGroups)
            {
                // Group checkbox
                var groupCheckBox = new CheckBox
                {
                    Content = group.Name,
                    FontWeight = global::Avalonia.Media.FontWeight.SemiBold,
                    FontSize = 12,
                    Tag = group.Name
                };
                groupCheckBox.IsCheckedChanged += OnGroupCheckChanged;
                _groupCheckBoxes[group.Name] = groupCheckBox;
                ScopePanel.Children.Add(groupCheckBox);

                // Individual scope checkboxes (indented)
                foreach (string scope in group.Scopes)
                {
                    var scopeCheckBox = new CheckBox
                    {
                        Content = scope,
                        FontSize = 11,
                        Margin = new global::Avalonia.Thickness(20, 0, 0, 0),
                        Tag = scope
                    };
                    scopeCheckBox.IsCheckedChanged += OnScopeCheckChanged;
                    _scopeCheckBoxes[scope] = scopeCheckBox;
                    ScopePanel.Children.Add(scopeCheckBox);
                }

                // Small gap between groups
                ScopePanel.Children.Add(new Border { Height = 4 });
            }
        }

        private void ApplyChecks(HashSet<string> scopes)
        {
            _isUpdating = true;

            foreach (var group in EsiScopePresets.FeatureGroups)
            {
                bool allChecked = true;
                foreach (string scope in group.Scopes)
                {
                    bool isChecked = scopes.Contains(scope);
                    if (_scopeCheckBoxes.TryGetValue(scope, out var cb))
                        cb.IsChecked = isChecked;
                    if (!isChecked)
                        allChecked = false;
                }

                if (_groupCheckBoxes.TryGetValue(group.Name, out var groupCb))
                    groupCb.IsChecked = allChecked && group.Scopes.Length > 0;
            }

            _isUpdating = false;
        }

        private void OnGroupCheckChanged(object? sender, RoutedEventArgs e)
        {
            if (_isUpdating || sender is not CheckBox groupCb)
                return;

            string? groupName = groupCb.Tag as string;
            if (groupName == null)
                return;

            var group = EsiScopePresets.FeatureGroups.FirstOrDefault(g => g.Name == groupName);
            if (group == null)
                return;

            _isUpdating = true;

            foreach (string scope in group.Scopes)
            {
                if (_scopeCheckBoxes.TryGetValue(scope, out var scopeCb))
                    scopeCb.IsChecked = groupCb.IsChecked == true;
            }

            // Enforce mandatory Skills & Training Queue
            EnforceMandatorySkills();

            _isUpdating = false;
        }

        private void OnScopeCheckChanged(object? sender, RoutedEventArgs e)
        {
            if (_isUpdating || sender is not CheckBox scopeCb)
                return;

            _isUpdating = true;

            // Update parent group checkbox
            foreach (var group in EsiScopePresets.FeatureGroups)
            {
                if (!group.Scopes.Contains(scopeCb.Tag as string))
                    continue;

                bool allChecked = group.Scopes.All(s =>
                    _scopeCheckBoxes.TryGetValue(s, out var cb) && cb.IsChecked == true);

                if (_groupCheckBoxes.TryGetValue(group.Name, out var groupCb))
                    groupCb.IsChecked = allChecked;

                break;
            }

            // Enforce mandatory Skills & Training Queue
            EnforceMandatorySkills();

            _isUpdating = false;
        }

        private void EnforceMandatorySkills()
        {
            var skillsGroup = EsiScopePresets.FeatureGroups.FirstOrDefault(
                g => g.Name == "Skills & Training Queue");
            if (skillsGroup == null)
                return;

            bool anyChecked = skillsGroup.Scopes.Any(s =>
                _scopeCheckBoxes.TryGetValue(s, out var cb) && cb.IsChecked == true);

            if (!anyChecked)
            {
                // Re-enable skills as mandatory
                foreach (string scope in skillsGroup.Scopes)
                {
                    if (_scopeCheckBoxes.TryGetValue(scope, out var cb))
                        cb.IsChecked = true;
                }

                if (_groupCheckBoxes.TryGetValue("Skills & Training Queue", out var groupCb))
                    groupCb.IsChecked = true;
            }
        }

        private HashSet<string> CollectCheckedScopes()
        {
            var scopes = new HashSet<string>();
            foreach (var kvp in _scopeCheckBoxes)
            {
                if (kvp.Value.IsChecked == true)
                    scopes.Add(kvp.Key);
            }
            return scopes;
        }

        private void OnSelectAllClick(object? sender, RoutedEventArgs e)
        {
            ApplyChecks(new HashSet<string>(EsiScopePresets.AllScopes));
        }

        private void OnClearAllClick(object? sender, RoutedEventArgs e)
        {
            // Clear all, then re-enable mandatory skills
            ApplyChecks(new HashSet<string>());

            _isUpdating = true;
            var skillsScopes = EsiScopePresets.FeatureGroups
                .First(g => g.Name == "Skills & Training Queue").Scopes;
            var mandatory = new HashSet<string>(skillsScopes);
            foreach (string scope in mandatory)
            {
                if (_scopeCheckBoxes.TryGetValue(scope, out var cb))
                    cb.IsChecked = true;
            }

            if (_groupCheckBoxes.TryGetValue("Skills & Training Queue", out var groupCb))
                groupCb.IsChecked = true;
            _isUpdating = false;
        }

        private void OnOkClick(object? sender, RoutedEventArgs e)
        {
            SelectedScopes = CollectCheckedScopes();
            SelectedPreset = EsiScopePresets.DetectPreset(SelectedScopes);
            DialogResult = true;
            Close();
        }

        private void OnCancelClick(object? sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
