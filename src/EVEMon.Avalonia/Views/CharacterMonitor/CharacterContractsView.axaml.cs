// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.ComponentModel;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterContractsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private ContractsListViewModel? _viewModel;

        // Maps IssuedFor ComboBox indices to IssuedFor enum values
        private static readonly IssuedFor[] IssuedForValues = { IssuedFor.All, IssuedFor.Character, IssuedFor.Corporation };

        // Maps Grouping ComboBox indices to ContractGrouping enum values.
        // Index 0 = default (no grouping). State=0 is the default enum value so the base class
        // treats it as "no grouping". Use StateDesc (value 1) for actual status grouping.
        private static readonly ContractGrouping[] GroupingValues =
        {
            default,
            ContractGrouping.StateDesc,
            ContractGrouping.ContractType,
            ContractGrouping.Issued,
            ContractGrouping.StartLocation
        };

        public CharacterContractsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterContractsEndedEvent>(OnDataUpdated);
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
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.Contracts))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            if (oc != null && !oc.HasScopeFor(ESIAPICharacterMethods.Contracts))
            {
                if (scopePrompt != null) scopePrompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (scopePrompt != null) scopePrompt.IsVisible = false;
            if (content != null) content.IsVisible = true;

            if (_viewModel == null)
            {
                _viewModel = new ContractsListViewModel();
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            }

            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            UpdateGrid();
            UpdateStatusBar();
            UpdateEmptyState();
            DataContext = _viewModel;
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
            if (_viewModel != null)
            {
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                _viewModel.Dispose();
                _viewModel = null;
            }
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.Contracts);
            LoadData();
        }

        private void OnDataUpdated(CharacterContractsEndedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ContractsListViewModel.Items)
                or nameof(ContractsListViewModel.TotalItemCount)
                or nameof(ContractsListViewModel.OutstandingCount)
                or nameof(ContractsListViewModel.CompletedCount))
            {
                UpdateGrid();
                UpdateStatusBar();
                UpdateEmptyState();
            }
        }

        private void OnFilterTextChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var filterBox = this.FindControl<TextBox>("FilterBox");
            var clearBtn = this.FindControl<Button>("ClearFilterButton");
            string text = filterBox?.Text ?? string.Empty;
            _viewModel.TextFilter = text;
            if (clearBtn != null)
                clearBtn.IsVisible = !string.IsNullOrEmpty(text);
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            var filterBox = this.FindControl<TextBox>("FilterBox");
            if (filterBox != null)
                filterBox.Text = string.Empty;
        }

        private void OnHideInactiveChanged(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            var toggle = this.FindControl<ToggleButton>("HideInactiveToggle");
            _viewModel.HideInactive = toggle?.IsChecked == true;
        }

        private void OnIssuedForChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var combo = this.FindControl<ComboBox>("IssuedForCombo");
            int idx = combo?.SelectedIndex ?? 0;
            if (idx >= 0 && idx < IssuedForValues.Length)
                _viewModel.ShowIssuedFor = IssuedForValues[idx];
        }

        private void OnGroupingChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            var combo = this.FindControl<ComboBox>("GroupingCombo");
            int idx = combo?.SelectedIndex ?? 0;
            if (idx >= 0 && idx < GroupingValues.Length)
                _viewModel.Grouping = GroupingValues[idx];
        }

        private void UpdateGrid()
        {
            if (_viewModel == null) return;
            var grid = this.FindControl<DataGrid>("ItemsGrid");
            if (grid != null)
                grid.ItemsSource = _viewModel.Items.ToList();
        }

        private void UpdateStatusBar()
        {
            if (_viewModel == null) return;
            var statusText = this.FindControl<TextBlock>("StatusBarText");
            if (statusText != null)
                statusText.Text = $"Contracts: {_viewModel.TotalItemCount} | Outstanding: {_viewModel.OutstandingCount} | Completed: {_viewModel.CompletedCount}";
        }

        private void UpdateEmptyState()
        {
            if (_viewModel == null) return;
            var emptyState = this.FindControl<Shared.EmptyState>("EmptyState");
            var grid = this.FindControl<DataGrid>("ItemsGrid");
            bool isEmpty = _viewModel.TotalItemCount == 0;
            if (emptyState != null) emptyState.IsVisible = isEmpty;
            if (grid != null) grid.IsVisible = !isEmpty;
        }
    }
}
