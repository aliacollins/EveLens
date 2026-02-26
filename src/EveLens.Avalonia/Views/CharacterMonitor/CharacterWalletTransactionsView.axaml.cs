// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
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
    public partial class CharacterWalletTransactionsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private WalletTransactionsListViewModel? _viewModel;
        private bool _populatingCombos;

        public CharacterWalletTransactionsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterWalletTransactionsUpdatedEvent>(OnDataUpdated);
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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.WalletTransactions))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.WalletTransactions))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            _viewModel ??= new WalletTransactionsListViewModel();
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

            var groupCombo = this.FindControl<ComboBox>("GroupingCombo");
            if (groupCombo != null)
            {
                var groupings = new[]
                {
                    "No grouping",
                    "Group by Date",
                    "Group by Date (Desc)",
                    "Group by Item Type",
                    "Group by Item Type (Desc)",
                    "Group by Client",
                    "Group by Client (Desc)",
                    "Group by Station",
                    "Group by Station (Desc)"
                };
                groupCombo.ItemsSource = groupings;
                groupCombo.SelectedIndex = (int)_viewModel.Grouping;
            }

            _populatingCombos = false;
        }

        private void UpdateGrid()
        {
            if (_viewModel == null) return;

            var grid = this.FindControl<DataGrid>("ItemsGrid");
            var emptyState = this.FindControl<Shared.EmptyState>("EmptyState");

            var items = _viewModel.Items.ToList();

            if (items.Count == 0)
            {
                if (grid != null) grid.IsVisible = false;
                if (emptyState != null) emptyState.IsVisible = true;
            }
            else
            {
                if (grid != null)
                {
                    grid.IsVisible = true;
                    grid.ItemsSource = items;
                }
                if (emptyState != null) emptyState.IsVisible = false;
            }
        }

        private void UpdateStatusBar()
        {
            if (_viewModel == null) return;

            var statusText = this.FindControl<TextBlock>("StatusText");
            if (statusText != null)
            {
                var net = _viewModel.NetCredit;
                var sign = net >= 0 ? "+" : "";
                statusText.Text = $"Transactions: {_viewModel.TotalItemCount} | Net: {sign}{net:N2} ISK";
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

        private void OnGroupingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_populatingCombos || _viewModel == null) return;

            var combo = this.FindControl<ComboBox>("GroupingCombo");
            if (combo != null)
            {
                _viewModel.Grouping = (WalletTransactionGrouping)combo.SelectedIndex;
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
            oc?.EnableEndpoint(ESIAPICharacterMethods.WalletTransactions);
            LoadData();
        }

        private void OnDataUpdated(CharacterWalletTransactionsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }
}
