using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Common.ViewModels;
using EVEMon.Common.ViewModels.Lists;

namespace EVEMon.Avalonia.Views.CharacterMonitor
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
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.WalletJournal))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;
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
    }
}
