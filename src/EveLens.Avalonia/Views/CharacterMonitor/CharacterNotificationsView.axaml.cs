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
using Avalonia.VisualTree;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterNotificationsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private NotificationsListViewModel? _viewModel;
        private bool _populatingCombo;

        public CharacterNotificationsView()
        {
            InitializeComponent();
            LocalizeUI();
        }
        private void LocalizeUI()
        {
            EnableTitle.Text = Loc.Get("ListView.EnableNotifications");
            EnableSubtext.Text = Loc.Get("ListView.EnableToFetch");
            EnableBtn.Content = Loc.Get("ListView.EnableNotificationsBtn");
            ScopeTitle.Text = Loc.Get("ListView.ScopeNotAuthorized");
            ScopeSubtext.Text = Loc.Get("ListView.ScopeNotAuthorizedDesc");
            CollapseAllBtn.Content = Loc.Get("Action.CollapseAll");
            ExpandAllBtn.Content = Loc.Get("Action.ExpandAll");
        }


        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterEVENotificationsUpdatedEvent>(OnDataUpdated);
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
            if (this.GetVisualRoot() == null) return;

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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.Notifications))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.Notifications))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new NotificationsListViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            PopulateGroupingCombo();
            UpdateList();
            UpdateStatusBar();
        }

        private void PopulateGroupingCombo()
        {
            if (_viewModel == null) return;
            _populatingCombo = true;

            var groupCombo = this.FindControl<ComboBox>("GroupingCombo");
            if (groupCombo != null)
            {
                var groupings = new[]
                {
                    "Group by Type",
                    "Group by Type (Desc)",
                    "Group by Date",
                    "Group by Date (Desc)",
                    "Group by Sender",
                    "Group by Sender (Desc)"
                };
                groupCombo.ItemsSource = groupings;
                groupCombo.SelectedIndex = (int)_viewModel.Grouping;
            }

            _populatingCombo = false;
        }

        private void UpdateList()
        {
            if (_viewModel == null) return;

            var groupsList = this.FindControl<ItemsControl>("NotificationsGroupsList");
            var scrollViewer = this.FindControl<ScrollViewer>("GroupedList");
            var emptyState = this.FindControl<TextBlock>("EmptyState");

            var groupedItems = _viewModel.GroupedItems;

            if (groupedItems.Count == 0 || _viewModel.TotalItemCount == 0)
            {
                if (scrollViewer != null) scrollViewer.IsVisible = false;
                if (emptyState != null) emptyState.IsVisible = true;
            }
            else
            {
                if (groupsList != null)
                {
                    var groups = groupedItems
                        .Select(g => new NotificationGroupDisplay(g))
                        .ToList();
                    CollapseStateHelper.InitializeGroups(_characterId, "Notifications", groups);
                    groupsList.ItemsSource = groups;
                }
                if (scrollViewer != null) scrollViewer.IsVisible = true;
                if (emptyState != null) emptyState.IsVisible = false;
            }
        }

        private void UpdateStatusBar()
        {
            if (_viewModel == null) return;

            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                statusText.Text = $"Notifications: {_viewModel.TotalItemCount} {(_viewModel.TotalItemCount == 1 ? "item" : "items")}";
            }
        }

        private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;

            var filterBox = this.FindControl<TextBox>("FilterBox");
            var clearBtn = this.FindControl<Button>("ClearFilterButton");
            var text = filterBox?.Text ?? string.Empty;

            if (clearBtn != null)
                clearBtn.IsVisible = !string.IsNullOrEmpty(text);

            _viewModel.TextFilter = text;
            UpdateList();
            UpdateStatusBar();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            var filterBox = this.FindControl<TextBox>("FilterBox");
            if (filterBox != null)
                filterBox.Text = string.Empty;
        }

        private void OnGroupingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_populatingCombo || _viewModel == null) return;

            var combo = this.FindControl<ComboBox>("GroupingCombo");
            if (combo != null)
            {
                _viewModel.Grouping = (EVENotificationsGrouping)combo.SelectedIndex;
                UpdateList();
                UpdateStatusBar();
            }
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
            oc?.EnableEndpoint(ESIAPICharacterMethods.Notifications);
            LoadData();
        }

        private void OnDataUpdated(CharacterEVENotificationsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is NotificationGroupDisplay group)
            {
                group.IsExpanded = !group.IsExpanded;
                SaveCollapseState();
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            var groupsList = this.FindControl<ItemsControl>("NotificationsGroupsList");
            if (groupsList?.ItemsSource is not IEnumerable<NotificationGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = false;
            SaveCollapseState();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            var groupsList = this.FindControl<ItemsControl>("NotificationsGroupsList");
            if (groupsList?.ItemsSource is not IEnumerable<NotificationGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = true;
            SaveCollapseState();
        }

        private void SaveCollapseState()
        {
            var groupsList = this.FindControl<ItemsControl>("NotificationsGroupsList");
            if (_characterId != 0 && groupsList?.ItemsSource is IEnumerable<NotificationGroupDisplay> groups)
                CollapseStateHelper.SaveGroups(_characterId, "Notifications", groups);
        }
    }

    /// <summary>
    /// Display model for a notification group — compact chevron header with INPC for expand/collapse.
    /// </summary>
    internal sealed class NotificationGroupDisplay : INotifyPropertyChanged, ICollapsibleGroup
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public List<EveNotification> Items { get; }

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

        public NotificationGroupDisplay(ListGrouping<EveNotification> group)
        {
            Name = group.Key;
            CountText = $"{group.Items.Count} {(group.Items.Count == 1 ? "notification" : "notifications")}";
            Items = group.Items.ToList();
        }
    }
}
