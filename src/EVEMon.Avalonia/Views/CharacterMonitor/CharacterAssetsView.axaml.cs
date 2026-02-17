using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using EVEMon.Common.Models;

namespace EVEMon.Avalonia.Views.CharacterMonitor
{
    public partial class CharacterAssetsView : UserControl
    {
        private List<Asset>? _allAssets;
        private List<AssetGroupEntry>? _groups;
        private string _filter = string.Empty;
        private int _groupMode = 1; // 0=Location, 1=Region, 2=Category, 3=Container

        public CharacterAssetsView()
        {
            InitializeComponent();
        }

        protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
        {
            base.OnAttachedToVisualTree(e);
            LoadData();
        }

        protected override void OnDataContextChanged(EventArgs e)
        {
            base.OnDataContextChanged(e);
            LoadData();
        }

        private void LoadData()
        {
            Character? character = DataContext as Character;
            if (character == null)
            {
                var parent = this.FindAncestorOfType<CharacterMonitorView>();
                character = parent?.DataContext as Character;
            }
            if (character is not CCPCharacter ccp) return;

            _allAssets = ccp.Assets
                .Where(a => a.Item != null && a.SolarSystem != null)
                .ToList();
            BuildGroups();
        }

        private void BuildGroups()
        {
            if (_allAssets == null) return;

            var filtered = _allAssets.AsEnumerable();
            if (!string.IsNullOrEmpty(_filter))
            {
                filtered = filtered.Where(a =>
                    (a.Item?.Name ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                    (a.Location ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                    (a.Item?.GroupName ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase));
            }

            var items = filtered.ToList();

            Func<Asset, string> groupKeySelector = _groupMode switch
            {
                1 => a => a.SolarSystem?.Constellation?.Region?.Name ?? "Unknown",
                2 => a => a.Item?.CategoryName ?? "Unknown",
                3 => a => string.IsNullOrEmpty(a.Container) ? "(No container)" : a.Container,
                _ => a => a.Location ?? "Unknown"
            };

            _groups = items
                .GroupBy(groupKeySelector)
                .OrderBy(g => g.Key)
                .Select(g => new AssetGroupEntry(g.Key, g.ToList()))
                .ToList();

            AssetGroupsList.ItemsSource = _groups;

            long totalItems = items.Sum(a => a.Quantity);
            double totalValue = items.Sum(a => a.Cost);
            var statusCtl = this.FindControl<TextBlock>("StatusText");
            if (statusCtl != null)
                statusCtl.Text = $"Items: {totalItems:N0} in {_groups.Count} locations  |  Est. Value: {totalValue:N0} ISK";
        }

        private void OnGroupByChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (_allAssets == null) return; // not loaded yet
            _groupMode = GroupByCombo.SelectedIndex;
            BuildGroups();
        }

        private void OnFilterChanged(object? sender, TextChangedEventArgs e)
        {
            if (_allAssets == null) return;
            _filter = FilterBox.Text?.Trim() ?? string.Empty;
            ClearFilterBtn.IsVisible = !string.IsNullOrEmpty(_filter);
            BuildGroups();
        }

        private void OnClearFilter(object? sender, RoutedEventArgs e)
        {
            if (_allAssets == null) return;
            FilterBox.Text = string.Empty;
            _filter = string.Empty;
            ClearFilterBtn.IsVisible = false;
            BuildGroups();
        }

        private void OnCollapseAll(object? sender, RoutedEventArgs e)
        {
            if (_groups == null) return;
            foreach (var g in _groups) g.IsExpanded = false;
            BuildGroups();
        }

        private void OnExpandAll(object? sender, RoutedEventArgs e)
        {
            if (_groups == null) return;
            foreach (var g in _groups) g.IsExpanded = true;
            BuildGroups();
        }
    }

    internal sealed class AssetGroupEntry
    {
        public string Name { get; }
        public List<AssetItemEntry> Items { get; }
        public bool IsExpanded { get; set; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }
        public double TotalVolume { get; }

        public string ItemCountText => $"{ItemCount} items ({TotalQuantity:N0} units)";
        public string ValueText => TotalValue > 0 ? $"{TotalValue:N0} ISK" : "";
        public string VolumeText => $"{TotalVolume:N0} m³";

        public AssetGroupEntry(string name, List<Asset> assets)
        {
            Name = name;
            ItemCount = assets.Count;
            TotalQuantity = assets.Sum(a => a.Quantity);
            TotalValue = assets.Sum(a => a.Cost);
            TotalVolume = assets.Sum(a => a.TotalVolume);
            IsExpanded = true;
            Items = assets
                .OrderBy(a => a.Item.Name)
                .Select(a => new AssetItemEntry(a))
                .ToList();
        }
    }

    internal sealed class AssetItemEntry
    {
        public string ItemName { get; }
        public string QuantityText { get; }
        public string LocationShort { get; }
        public string ValueText { get; }

        public AssetItemEntry(Asset asset)
        {
            ItemName = asset.Item?.Name ?? "Unknown";
            QuantityText = asset.Quantity > 1 ? $"x{asset.Quantity:N0}" : "";
            LocationShort = !string.IsNullOrEmpty(asset.Container) ? asset.Container : "";
            ValueText = asset.Cost > 0 ? $"{asset.Cost:N0} ISK" : "";
        }
    }
}
