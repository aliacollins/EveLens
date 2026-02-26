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
using EveLens.Common.Enumerations.CCPAPI;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.ViewModels;
using EveLens.Common.ViewModels.Lists;

namespace EveLens.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterWalletJournalView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private WalletJournalListViewModel? _viewModel;
        private bool _populatingCombos;

        public CharacterWalletJournalView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterWalletJournalUpdatedEvent>(OnDataUpdated);
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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.WalletJournal))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.WalletJournal))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new WalletJournalListViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            PopulateCombos();
            UpdateGrid();
            UpdateStatusBar();
        }

        private void PopulateCombos()
        {
            if (_viewModel == null) return;
            _populatingCombos = true;

            // Type filter combo
            var typeCombo = this.FindControl<ComboBox>("TypeFilterCombo");
            if (typeCombo != null)
            {
                var types = new List<string> { "All Types" };
                types.AddRange(_viewModel.AvailableTypes);
                typeCombo.ItemsSource = types;
                typeCombo.SelectedIndex = string.IsNullOrEmpty(_viewModel.TypeFilter)
                    ? 0
                    : types.IndexOf(_viewModel.TypeFilter);
                if (typeCombo.SelectedIndex < 0) typeCombo.SelectedIndex = 0;
            }

            // Grouping combo
            var groupCombo = this.FindControl<ComboBox>("GroupingCombo");
            if (groupCombo != null)
            {
                var groupings = new[]
                {
                    "No grouping",
                    "Group by Date",
                    "Group by Date (Desc)",
                    "Group by Type",
                    "Group by Type (Desc)",
                    "Group by Issuer",
                    "Group by Issuer (Desc)",
                    "Group by Recipient",
                    "Group by Recipient (Desc)"
                };
                groupCombo.ItemsSource = groupings;
                groupCombo.SelectedIndex = (int)_viewModel.Grouping;
            }

            _populatingCombos = false;
        }

        private void UpdateGrid()
        {
            if (_viewModel == null) return;

            var groupsList = this.FindControl<ItemsControl>("JournalGroupsList");
            var scrollViewer = this.FindControl<ScrollViewer>("GroupedList");
            var emptyState = this.FindControl<Shared.EmptyState>("EmptyState");

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
                    groupsList.ItemsSource = groupedItems
                        .Select(g => new JournalGroupDisplay(g))
                        .ToList();
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
                var net = _viewModel.NetAmount;
                var sign = net >= 0 ? "+" : "";
                statusText.Text = $"Journal: {_viewModel.TotalItemCount} entries | Net: {sign}{net:N2} ISK";
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
            UpdateGrid();
            UpdateStatusBar();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            var filterBox = this.FindControl<TextBox>("FilterBox");
            if (filterBox != null)
                filterBox.Text = string.Empty;
        }

        private void OnTypeFilterChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_populatingCombos || _viewModel == null) return;

            var combo = this.FindControl<ComboBox>("TypeFilterCombo");
            if (combo?.SelectedIndex is null or 0)
            {
                _viewModel.TypeFilter = null;
            }
            else
            {
                _viewModel.TypeFilter = combo.SelectedItem as string;
            }
            UpdateGrid();
            UpdateStatusBar();
        }

        private void OnGroupingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_populatingCombos || _viewModel == null) return;

            var combo = this.FindControl<ComboBox>("GroupingCombo");
            if (combo != null)
            {
                _viewModel.Grouping = (WalletJournalGrouping)combo.SelectedIndex;
                UpdateGrid();
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
            oc?.EnableEndpoint(ESIAPICharacterMethods.WalletJournal);
            LoadData();
        }

        private void OnDataUpdated(CharacterWalletJournalUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnGroupHeaderClicked(object? sender, PointerPressedEventArgs e)
        {
            if (sender is not Border border) return;
            if (border.DataContext is JournalGroupDisplay group)
            {
                group.IsExpanded = !group.IsExpanded;
            }
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            var groupsList = this.FindControl<ItemsControl>("JournalGroupsList");
            if (groupsList?.ItemsSource is not IEnumerable<JournalGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = false;
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            var groupsList = this.FindControl<ItemsControl>("JournalGroupsList");
            if (groupsList?.ItemsSource is not IEnumerable<JournalGroupDisplay> groups) return;
            foreach (var g in groups) g.IsExpanded = true;
        }
    }

    /// <summary>
    /// Display model for a journal group — compact chevron header with INPC for expand/collapse.
    /// </summary>
    internal sealed class JournalGroupDisplay : INotifyPropertyChanged
    {
        private bool _isExpanded = true;

        public string Name { get; }
        public string CountText { get; }
        public List<JournalEntryDisplay> Items { get; }

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

        public JournalGroupDisplay(ListGrouping<WalletJournal> group)
        {
            Name = group.Key;
            CountText = $"{group.Items.Count} {(group.Items.Count == 1 ? "entry" : "entries")}";
            Items = group.Items.Select(j => new JournalEntryDisplay(j)).ToList();
        }
    }

    /// <summary>
    /// Display wrapper for a journal entry — adds Avalonia-specific properties.
    /// </summary>
    internal sealed class JournalEntryDisplay
    {
        private readonly WalletJournal _journal;

        public JournalEntryDisplay(WalletJournal journal)
        {
            _journal = journal;
        }

        public DateTime Date => _journal.Date;
        public string Type => _journal.Type;
        public string Reason => _journal.Reason;
        public decimal Amount => _journal.Amount;
        public decimal Balance => _journal.Balance;
        public string Issuer => _journal.Issuer;
        public string Recipient => _journal.Recipient;

        public IBrush AmountBrush => Amount >= 0
            ? Application.Current?.FindResource("EveSuccessGreenBrush") as IBrush ?? Brushes.LightGreen
            : Application.Current?.FindResource("EveErrorRedBrush") as IBrush ?? Brushes.Red;
    }
}
