using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Models;
using EVEMon.Common.ViewModels;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Common.Events;
using EVEMon.Common.Services;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterAssetsView : UserControl
    {
        private IDisposable? _dataUpdatedSub;
        private long _characterId;
        private AssetBrowserViewModel? _viewModel;

        public CharacterAssetsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            _dataUpdatedSub ??= AppServices.EventAggregator?.Subscribe<CharacterAssetsUpdatedEvent>(OnDataUpdated);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnDetachedFromVisualTree(e);
            _dataUpdatedSub?.Dispose();
            _dataUpdatedSub = null;
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
            if (character is not CCPCharacter) return;

            _characterId = character.CharacterID;

            // Check if on-demand endpoint is enabled
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            var prompt = this.FindControl<Border>("EnablePrompt");
            var emptyState = this.FindControl<Border>("EmptyState");
            var content = this.FindControl<DockPanel>("DataContent");
            if (oc != null && !oc.IsEndpointEnabled(ESIAPICharacterMethods.AssetList))
            {
                if (prompt != null) prompt.IsVisible = true;
                if (emptyState != null) emptyState.IsVisible = false;
                if (content != null) content.IsVisible = false;
                return;
            }
            if (prompt != null) prompt.IsVisible = false;

            _viewModel ??= new AssetBrowserViewModel();
            if (_viewModel.Character != character)
                _viewModel.Character = character;
            else
                _viewModel.ForceRefresh();

            bool hasData = _viewModel.TotalItems > 0;
            if (emptyState != null) emptyState.IsVisible = !hasData;
            if (content != null) content.IsVisible = hasData;

            if (!hasData) return;

            // Flatten all groups into a single list for the DataGrid (Law 20: .ToList())
            var flatItems = new List<AssetFlatEntry>();

            if (_viewModel.IsHierarchical)
            {
                // Hierarchical mode: flatten region→system→station→items
                foreach (var region in _viewModel.HierarchicalGroups)
                {
                    foreach (var system in region.Systems)
                    {
                        foreach (var station in system.Stations)
                        {
                            string locationText = $"{station.Name}, {system.Name}";
                            foreach (var item in station.Items)
                                flatItems.Add(new AssetFlatEntry(item, region.Name, locationText));
                        }
                    }
                }
            }
            else
            {
                // Flat mode: flatten group→items
                foreach (var group in _viewModel.Groups)
                {
                    foreach (var item in group.Items)
                        flatItems.Add(new AssetFlatEntry(item, group.Name, item.LocationShort));
                }
            }

            AssetsGrid.ItemsSource = flatItems;

            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Items: {_viewModel.TotalItems:N0} in {_viewModel.GroupCount} {(_viewModel.IsHierarchical ? "regions" : "groups")}  |  Est. Value: {_viewModel.TotalValue:N0} ISK";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.GroupMode = GroupByCombo.SelectedIndex;
            LoadData();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_viewModel == null) return;
            _viewModel.Filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_viewModel.Filter);
            LoadData();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_viewModel == null) return;
            FilterBox.Text = string.Empty;
            _viewModel.Filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            LoadData();
        }

        private void OnCopyItemName(object? sender, RoutedEventArgs e)
        {
            var item = AssetsGrid.SelectedItem as AssetFlatEntry;
            if (item != null)
                TopLevel.GetTopLevel(this)?.Clipboard?.SetTextAsync(item.ItemName);
        }

        private void OnEnableEndpoint(object? sender, RoutedEventArgs e)
        {
            var parentView = this.FindAncestorOfType<CharacterMonitorView>();
            var oc = parentView?.DataContext as ObservableCharacter;
            oc?.EnableEndpoint(ESIAPICharacterMethods.AssetList);
            LoadData();
        }

        private void OnDataUpdated(CharacterAssetsUpdatedEvent evt)
        {
            if (evt.Character?.CharacterID == _characterId)
                global::Avalonia.Threading.Dispatcher.UIThread.Post(LoadData);
        }
    }

    /// <summary>Flat display entry for asset DataGrid rows.</summary>
    internal sealed class AssetFlatEntry
    {
        public string ItemName { get; }
        public string QuantityText { get; }
        public string LocationText { get; }
        public string ValueText { get; }
        public string GroupName { get; }

        public AssetFlatEntry(AssetBrowserItemEntry item, string groupName, string locationText)
        {
            ItemName = item.ItemName;
            QuantityText = item.QuantityText;
            LocationText = locationText;
            ValueText = item.ValueText;
            GroupName = groupName;
        }
    }
}
