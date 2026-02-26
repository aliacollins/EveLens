// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using EveLens.Avalonia.ViewModels;
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;
using EveLens.Avalonia.Views.Shared;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterMailMessagesView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private IDisposable? _bodyDownloadedSub;
        private long _characterId;
        private MailMessagesListViewModel? _viewModel;

        // Grouping modes mapped to ComboBox indices
        private static readonly EVEMailMessagesGrouping[] GroupingModes =
        {
            EVEMailMessagesGrouping.State,
            EVEMailMessagesGrouping.SentDate,
            EVEMailMessagesGrouping.Sender,
            EVEMailMessagesGrouping.Subject,
            EVEMailMessagesGrouping.Recipient,
            EVEMailMessagesGrouping.CorpOrAlliance,
        };

        public CharacterMailMessagesView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterEVEMailMessagesUpdatedEvent>(OnDataUpdated);
            _bodyDownloadedSub ??= AppServices.EventAggregator?.Subscribe<CharacterEVEMailBodyDownloadedEvent>(OnBodyDownloaded);
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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.MailMessages))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.MailMessages))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new MailMessagesListViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            _viewModel.MarkAsViewed();
            PopulateView();
        }

        private void PopulateView()
        {
            if (_viewModel == null) return;

            var grouped = _viewModel.GroupedItems;
            var emptyState = this.FindControl<UserControl>("EmptyState");
            var scroller = this.FindControl<ScrollViewer>("MailScroller");

            if (grouped == null || grouped.Count == 0 || grouped.All(g => g.Items.Count == 0))
            {
                if (emptyState != null) emptyState.IsVisible = true;
                if (scroller != null) scroller.IsVisible = false;
                StatusText.Text = "Mail: 0 messages";
                return;
            }

            if (emptyState != null) emptyState.IsVisible = false;
            if (scroller != null) scroller.IsVisible = true;

            var groups = new List<MailGroupEntry>();
            foreach (var group in grouped)
            {
                var entries = group.Items
                    .Select(mail => new MailDisplayEntry(mail, _viewModel.IsNewItem(mail)))
                    .ToList();
                if (entries.Count > 0)
                    groups.Add(new MailGroupEntry(group.Key, entries));
            }

            MailGroupsList.ItemsSource = groups;
            StatusText.Text = $"Mail: {_viewModel.TotalItemCount} message{(_viewModel.TotalItemCount == 1 ? "" : "s")}";
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
            _bodyDownloadedSub?.Dispose();
            _bodyDownloadedSub = null;
            _viewModel?.Dispose();
            _viewModel = null;
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.MailMessages);
            LoadData();
        }

        private void OnDataUpdated(CharacterEVEMailMessagesUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnBodyDownloaded(CharacterEVEMailBodyDownloadedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(PopulateView);
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            int idx = GroupByCombo.SelectedIndex;
            if (idx >= 0 && idx < GroupingModes.Length)
            {
                _viewModel.Grouping = GroupingModes[idx];
                PopulateView();
            }
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.TextFilter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.TextFilter);
            PopulateView();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.TextFilter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            PopulateView();
        }

        private void OnMailItemClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border { Tag: MailDisplayEntry entry })
            {
                // Check if body is available
                if (!entry.HasBody)
                {
                    // Trigger download
                    entry.Mail.GetMailBody();

                    // Show window with loading message
                    var window = new MailReadingWindow();
                    window.SetMail(entry.Subject, entry.SenderName, entry.SentDateText,
                        "Loading mail body from EVE servers...\n\nPlease wait a moment and try reopening this message.");
                    window.Show();
                }
                else
                {
                    // Body is already available
                    var window = new MailReadingWindow();
                    window.SetMail(entry.Subject, entry.SenderName, entry.SentDateText, entry.BodyText);
                    window.Show();
                }
            }
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is Border { DataContext: MailGroupEntry group })
                group.IsExpanded = !group.IsExpanded;
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (MailGroupsList.ItemsSource is IEnumerable<MailGroupEntry> groups)
                foreach (var g in groups) g.IsExpanded = false;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (MailGroupsList.ItemsSource is IEnumerable<MailGroupEntry> groups)
                foreach (var g in groups) g.IsExpanded = true;
        }
    }
}
