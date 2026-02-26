// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EveLens.Common.Constants;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.ViewModels;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorView : UserControl
    {
        private readonly Dictionary<ESIAPICharacterMethods, CheckBox> _endpointCheckBoxes = new();

        /// <summary>
        /// Maps ESI endpoint methods to their corresponding TabItem x:Names.
        /// </summary>
        private static readonly Dictionary<ESIAPICharacterMethods, string> EndpointToTabName = new()
        {
            { ESIAPICharacterMethods.EmploymentHistory, "TabEmployment" },
            { ESIAPICharacterMethods.Standings, "TabStandings" },
            { ESIAPICharacterMethods.ContactList, "TabContacts" },
            { ESIAPICharacterMethods.FactionalWarfareStats, "TabFW" },
            { ESIAPICharacterMethods.Medals, "TabMedals" },
            { ESIAPICharacterMethods.LoyaltyPoints, "TabLP" },
            { ESIAPICharacterMethods.AssetList, "TabAssets" },
            { ESIAPICharacterMethods.MarketOrders, "TabOrders" },
            { ESIAPICharacterMethods.Contracts, "TabContracts" },
            { ESIAPICharacterMethods.IndustryJobs, "TabIndustry" },
            { ESIAPICharacterMethods.WalletJournal, "TabJournal" },
            { ESIAPICharacterMethods.WalletTransactions, "TabTransactions" },
            { ESIAPICharacterMethods.MailMessages, "TabMail" },
            { ESIAPICharacterMethods.Notifications, "TabNotify" },
            { ESIAPICharacterMethods.KillLog, "TabKills" },
            { ESIAPICharacterMethods.PlanetaryColonies, "TabPI" },
            { ESIAPICharacterMethods.ResearchPoints, "TabResearch" },
        };

        public CharacterMonitorView()
        {
            InitializeComponent();

            // Populate endpoint toggles when the flyout opens
            var gearBtn = this.FindControl<Button>("EndpointGearBtn");
            if (gearBtn?.Flyout is Flyout flyout)
            {
                flyout.Opened += OnFlyoutOpened;
            }
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            SyncTabVisibility();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            SyncTabVisibility();
        }

        /// <summary>
        /// Shows or hides on-demand tabs based on which endpoints the user has enabled.
        /// Core tabs (Skills, Queue) are always visible.
        /// </summary>
        private void SyncTabVisibility()
        {
            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            foreach (var kvp in EndpointToTabName)
            {
                var tab = this.FindControl<TabItem>(kvp.Value);
                if (tab != null)
                    tab.IsVisible = oc.IsEndpointEnabled(kvp.Key) && oc.HasScopeFor(kvp.Key);
            }
        }

        private void OnFlyoutOpened(object? sender, EventArgs e)
        {
            PopulateEndpointToggles();
        }

        private void PopulateEndpointToggles()
        {
            var panel = this.FindControl<StackPanel>("EndpointToggles");
            if (panel == null) return;

            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            // Rebuild checkboxes each time to reflect current state
            panel.Children.Clear();
            _endpointCheckBoxes.Clear();

            foreach (var kvp in EndpointClassification.TabToEndpoint)
            {
                var method = kvp.Value;
                string displayName = EndpointClassification.EndpointDisplayName(method);
                bool isEnabled = oc.IsEndpointEnabled(method) && oc.HasScopeFor(method);

                var cb = new CheckBox
                {
                    Content = displayName,
                    IsChecked = isEnabled,
                    FontSize = 11,
                    Padding = new Thickness(4, 1),
                    Tag = method,
                };
                cb.IsCheckedChanged += OnEndpointToggleChanged;

                _endpointCheckBoxes[method] = cb;
                panel.Children.Add(cb);
            }
        }

        private void OnEndpointToggleChanged(object? sender, RoutedEventArgs e)
        {
            if (sender is not CheckBox cb || cb.Tag is not ESIAPICharacterMethods method)
                return;

            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            if (cb.IsChecked == true)
                oc.EnableEndpoint(method);
            else
                oc.DisableEndpoint(method);

            // Update tab visibility immediately
            if (EndpointToTabName.TryGetValue(method, out string? tabName))
            {
                var tab = this.FindControl<TabItem>(tabName);
                if (tab != null)
                    tab.IsVisible = cb.IsChecked == true;
            }
        }

        private void OnEnableAllEndpoints(object? sender, RoutedEventArgs e)
        {
            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            foreach (var kvp in EndpointClassification.TabToEndpoint)
            {
                oc.EnableEndpoint(kvp.Value);
                if (_endpointCheckBoxes.TryGetValue(kvp.Value, out var cb))
                    cb.IsChecked = true;
            }

            SyncTabVisibility();
        }

        private void OnDisableAllEndpoints(object? sender, RoutedEventArgs e)
        {
            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            foreach (var kvp in EndpointClassification.TabToEndpoint)
            {
                oc.DisableEndpoint(kvp.Value);
                if (_endpointCheckBoxes.TryGetValue(kvp.Value, out var cb))
                    cb.IsChecked = false;
            }

            SyncTabVisibility();
        }
    }
}
