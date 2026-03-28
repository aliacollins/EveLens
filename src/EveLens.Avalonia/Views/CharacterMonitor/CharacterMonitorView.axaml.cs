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
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Core.Events;

using EveLens.Core.Events;
using EveLens.Avalonia.Services;
namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMonitorView : UserControl
    {
        private readonly Dictionary<ESIAPICharacterMethods, CheckBox> _endpointCheckBoxes = new();
        private IDisposable? _fetchCompletedSub;
        private bool _esiReady;
        private global::Avalonia.Threading.DispatcherTimer? _loadingTimer;
        private int _loadingMessageIndex;

        private static readonly Random s_rng = new();
        private static readonly string[] LoadingMessages = new[]
        {
            "Shooot! You clicked too fast — still syncing your skills...",
            "Hold your horses, capsuleer! Fetching your wallet data...",
            "Whoa! Still pulling your asset list from ESI...",
            "One sec — counting your market orders...",
            "Hang on! Your contracts are loading...",
            "Almost! Just syncing your industry jobs...",
            "Hold on — rounding up your mail and notifications...",
            "Your skill queue is loading... patience, capsuleer!",
            "Pulling your kill reports from CONCORD...",
            "Syncing your standings... some agents are slow",
            "Just a moment — checking your clone implants...",
            "Loading your loyalty points across all corps...",
            "Fetching employment history... it's a long resume!",
            "One moment — your wallet journal has a lot of entries...",
            "Still connecting to ESI — your data is on its way!",
            "Preparing your character sheet... almost there!",
            "Syncing contact list... you know a lot of people!",
            "Hang tight — pulling your transaction history...",
            "Loading medal collection... very distinguished!",
            "Your planetary colonies are reporting in...",
        };

        /// <summary>
        /// Maps ESI endpoint methods to their corresponding TabItem x:Names.
        /// </summary>
        private static readonly Dictionary<ESIAPICharacterMethods, string> EndpointToTabName = new()
        {
            { ESIAPICharacterMethods.Clones, "TabClones" },
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
            // Re-sync tabs when ESI fetches complete — scopes may not be available
            // at initial load if the token hasn't refreshed yet.
            _fetchCompletedSub ??= AppServices.EventAggregator?.Subscribe<MonitorFetchCompletedEvent>(evt =>
            {
                var oc = DataContext as ObservableCharacter;
                if (oc != null && evt.CharacterId == oc.CharacterID)
                {
                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        _esiReady = true;
                        SyncTabVisibility();
                    });
                }
            });
            SyncTabVisibility();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            _esiReady = false;
            SyncTabVisibility();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _fetchCompletedSub?.Dispose();
            _fetchCompletedSub = null;
        }

        /// <summary>
        /// Shows or hides on-demand tabs based on which endpoints the user has enabled.
        /// Core tabs (Skills, Queue) are always visible.
        /// If ESI identity hasn't linked yet, shows a loading overlay until the first
        /// fetch completes and scopes become available.
        /// </summary>
        private void SyncTabVisibility()
        {
            var oc = DataContext as ObservableCharacter;
            if (oc == null) return;

            bool anyEnabled = false;
            bool anyVisible = false;

            foreach (var kvp in EndpointToTabName)
            {
                var tab = this.FindControl<TabItem>(kvp.Value);
                if (tab == null) continue;

                bool enabled = oc.IsEndpointEnabled(kvp.Key);
                bool hasScope = oc.HasScopeFor(kvp.Key);
                tab.IsVisible = enabled && hasScope;

                if (enabled) anyEnabled = true;
                if (enabled && hasScope) anyVisible = true;
            }

            // If user has enabled endpoints but none are showing (scope check fails),
            // ESI identity hasn't linked yet — show loading overlay with rotating messages
            var overlay = this.FindControl<Border>("LoadingOverlay");
            if (overlay != null)
            {
                bool waiting = anyEnabled && !anyVisible && !_esiReady;
                overlay.IsVisible = waiting;

                if (waiting && _loadingTimer == null)
                {
                    // Show a random first message
                    var title = this.FindControl<TextBlock>("LoadingTitle");
                    if (title != null)
                        title.Text = LoadingMessages[s_rng.Next(LoadingMessages.Length)];

                    int tickCount = 0;

                    _loadingTimer = new global::Avalonia.Threading.DispatcherTimer
                    {
                        Interval = TimeSpan.FromSeconds(2),
                        IsEnabled = true
                    };
                    _loadingTimer.Tick += (_, _) =>
                    {
                        tickCount++;
                        // Random message each tick
                        var t = this.FindControl<TextBlock>("LoadingTitle");
                        if (t != null)
                            t.Text = LoadingMessages[s_rng.Next(LoadingMessages.Length)];

                        // Safety timeout: 6 seconds (3 ticks at 2s)
                        if (tickCount >= 3)
                        {
                            StopLoadingOverlay();
                            _esiReady = true;
                            SyncTabVisibility();
                        }
                    };
                }
                else if (!waiting)
                {
                    StopLoadingOverlay();
                }
            }
        }

        private void StopLoadingOverlay()
        {
            _loadingTimer?.Stop();
            _loadingTimer = null;
            var overlay = this.FindControl<Border>("LoadingOverlay");
            if (overlay != null)
                overlay.IsVisible = false;
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
                    FontSize = FontScaleService.Body,
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
