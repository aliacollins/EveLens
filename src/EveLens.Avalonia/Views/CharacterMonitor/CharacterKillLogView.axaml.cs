// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.VisualTree;
using EveLens.Avalonia.Converters;
using EveLens.Common.Enumerations;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterKillLogView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private KillLogListViewModel? _viewModel;
        private List<KillLogGroupDisplay> _groups = new();

        public CharacterKillLogView()
        {
            InitializeComponent();
            LocalizeUI();
        }
        private void LocalizeUI()
        {
            EnableTitle.Text = Loc.Get("ListView.EnableKillLog");
            EnableSubtext.Text = Loc.Get("ListView.EnableToFetch");
            EnableBtn.Content = Loc.Get("ListView.EnableKillLogBtn");
            ScopeTitle.Text = Loc.Get("ListView.ScopeNotAuthorized");
            ScopeSubtext.Text = Loc.Get("ListView.ScopeNotAuthorizedDesc");
            GroupByLabel.Text = Loc.Get("ListView.GroupBy");
            CollapseAllBtn.Content = Loc.Get("Action.CollapseAll");
            ExpandAllBtn.Content = Loc.Get("Action.ExpandAll");
        }


        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterKillLogUpdatedEvent>(OnDataUpdated);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            if (_viewModel != null && DataContext is Character)
                LoadData();
        }

        private void LoadData()
        {
            Character? character = DataContext as Character
                ?? (DataContext as ObservableCharacter)?.Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = (parent?.DataContext as ObservableCharacter)?.Character
                    ?? parent?.DataContext as Character;
            }
            if (character == null) return;

            _characterId = character.CharacterID;

            // Check if on-demand endpoint is enabled
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var scopePrompt = this.FindControl<Border>("ScopePrompt");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.KillLog))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.KillLog))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new KillLogListViewModel();

            // Apply grouping from ComboBox (before character set to avoid double refresh)
            var grouping = GroupByCombo.SelectedIndex switch
            {
                0 => KillLogGrouping.KillsVsLosses,
                1 => KillLogGrouping.Date,
                2 => KillLogGrouping.ShipType,
                3 => KillLogGrouping.Corporation,
                _ => KillLogGrouping.KillsVsLosses
            };

            if (_viewModel.Character != character)
            {
                _viewModel.Grouping = grouping;
                _viewModel.Character = character; // triggers Refresh via OnCharacterChanged
            }
            else
            {
                _viewModel.Grouping = grouping; // triggers Refresh if grouping changed
                _viewModel.ForceRefresh(); // ensure data is current
            }

            _viewModel.UpdateCounts();

            BuildDisplayGroups();
            UpdateUI();
        }

        private void BuildDisplayGroups()
        {
            if (_viewModel == null) return;

            _groups = _viewModel.GroupedItems
                .Where(g => g.Items.Count > 0)
                .Select(g => new KillLogGroupDisplay(
                    g.Key,
                    g.Items.Select(item => new KillLogItemDisplay(item, _viewModel.IsLoss(item))).ToList()))
                .ToList();
        }

        private void UpdateUI()
        {
            if (_viewModel == null) return;

            var emptyState = this.FindControl<UserControl>("EmptyState");
            var scroller = this.FindControl<ScrollViewer>("MainScroller");
            var groupsList = this.FindControl<ItemsControl>("KillGroupsList");

            bool hasItems = _viewModel.TotalItemCount > 0;
            if (emptyState != null) emptyState.IsVisible = !hasItems;
            if (scroller != null) scroller.IsVisible = hasItems;
            if (groupsList != null)
                groupsList.ItemsSource = _groups;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Kill Log: {_viewModel.TotalItemCount}  |  Kills: {_viewModel.KillCount}  |  Losses: {_viewModel.LossCount}";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;

            _viewModel.Grouping = GroupByCombo.SelectedIndex switch
            {
                0 => KillLogGrouping.KillsVsLosses,
                1 => KillLogGrouping.Date,
                2 => KillLogGrouping.ShipType,
                3 => KillLogGrouping.Corporation,
                _ => KillLogGrouping.KillsVsLosses
            };

            _viewModel.UpdateCounts();
            BuildDisplayGroups();
            UpdateUI();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.TextFilter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.TextFilter);
            _viewModel.UpdateCounts();
            BuildDisplayGroups();
            UpdateUI();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.TextFilter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            _viewModel.UpdateCounts();
            BuildDisplayGroups();
            UpdateUI();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _groups)
                g.IsExpanded = false;
            KillGroupsList.ItemsSource = null;
            KillGroupsList.ItemsSource = _groups;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            foreach (var g in _groups)
                g.IsExpanded = true;
            KillGroupsList.ItemsSource = null;
            KillGroupsList.ItemsSource = _groups;
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border border && border.DataContext is KillLogGroupDisplay group)
                group.IsExpanded = !group.IsExpanded;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.KillLog);
            LoadData();
        }

        private void OnDataUpdated(CharacterKillLogUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }

    /// <summary>
    /// Display model for a group of kill log entries in the chevron view.
    /// </summary>
    internal sealed class KillLogGroupDisplay : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public List<KillLogItemDisplay> Items { get; }
        public string ItemCountText { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                if (_isExpanded == value) return;
                _isExpanded = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsExpanded)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Chevron)));
            }
        }

        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";

        public event PropertyChangedEventHandler? PropertyChanged;

        public KillLogGroupDisplay(string name, List<KillLogItemDisplay> items)
        {
            Name = string.IsNullOrEmpty(name) ? "All" : name;
            Items = items;
            ItemCountText = $"{items.Count} entries";
        }
    }

    /// <summary>
    /// Display model for a single kill log entry with pre-computed display properties.
    /// </summary>
    internal sealed class KillLogItemDisplay
    {
        private static readonly IBrush KillNameBrush = new SolidColorBrush(Color.Parse("#FFDAA520")); // Gold
        private static readonly IBrush LossNameBrush = new SolidColorBrush(Color.Parse("#FFCF6679")); // Red

        public KillLogItemDisplay(KillLog killLog, bool isLoss)
        {
            IsLoss = isLoss;

            var victim = killLog.Victim;
            var attackerCount = killLog.Attackers?.Count() ?? 0;

            TimeText = killLog.KillTime.ToString("MMM dd HH:mm");
            AttackersText = $"Attackers: {attackerCount}";

            if (isLoss)
            {
                PrimaryName = victim.ShipTypeName ?? "Unknown Ship";
                PrimaryNameBrush = LossNameBrush;

                var finalBlow = killLog.Attackers?.FirstOrDefault(a => a.FinalBlow);
                string finalBlowName = finalBlow?.Name ?? "Unknown";
                string systemName = killLog.SolarSystem?.Name ?? "Unknown";
                SecondaryInfo = $"{systemName}  |  Final blow: {finalBlowName}";
            }
            else
            {
                PrimaryName = victim.Name ?? "Unknown Pilot";
                PrimaryNameBrush = KillNameBrush;

                string shipType = victim.ShipTypeName ?? "Unknown Ship";
                string corpName = victim.CorporationName ?? "";
                SecondaryInfo = $"{shipType}  |  {corpName}";
            }

            // Load ship image asynchronously
            LoadShipImage(killLog);
        }

        public bool IsLoss { get; }
        public string PrimaryName { get; }
        public IBrush PrimaryNameBrush { get; }
        public string SecondaryInfo { get; }
        public string TimeText { get; }
        public string AttackersText { get; }
        public global::Avalonia.Media.Imaging.Bitmap? ShipImage { get; private set; }

        private void LoadShipImage(KillLog killLog)
        {
            var rawImage = killLog.VictimShipImage;
            if (rawImage != null)
            {
                ShipImage = DrawingImageToAvaloniaConverter.Instance.Convert(
                    rawImage, typeof(global::Avalonia.Media.Imaging.Bitmap), null,
                    System.Globalization.CultureInfo.InvariantCulture) as global::Avalonia.Media.Imaging.Bitmap;
            }

            // Subscribe for async image updates
            killLog.KillLogVictimShipImageUpdated += (sender, _) =>
            {
                if (sender is KillLog kl)
                {
                    var img = DrawingImageToAvaloniaConverter.Instance.Convert(
                        kl.VictimShipImage, typeof(global::Avalonia.Media.Imaging.Bitmap), null,
                        System.Globalization.CultureInfo.InvariantCulture) as global::Avalonia.Media.Imaging.Bitmap;

                    global::Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        ShipImage = img;
                    });
                }
            };
        }
    }
}
