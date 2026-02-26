// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the asset browser view showing character assets grouped by location, region, category, or container.
    /// </summary>
    public sealed class AssetBrowserViewModel : CharacterViewModelBase
    {
        private List<Asset>? _allAssets;
        private List<AssetBrowserGroupEntry> _groups = new();
        private List<AssetRegionGroup> _hierarchicalGroups = new();
        private string _filter = string.Empty;
        private int _groupMode = 1; // 0=Location, 1=Region, 2=Category, 3=Container
        private long _totalItems;
        private double _totalValue;
        private int _groupCount;

        public AssetBrowserViewModel() : base()
        {
            SubscribeForCharacter<CharacterAssetsUpdatedEvent>(e => Reload());
            Subscribe<SettingsChangedEvent>(e => Reload());
        }

        public AssetBrowserViewModel(IEventAggregator agg, IDispatcher? disp = null)
            : base(agg, disp) { }

        /// <summary>
        /// Gets the list of flat asset groups (used for Category and Container modes).
        /// </summary>
        public List<AssetBrowserGroupEntry> Groups => _groups;

        /// <summary>
        /// Gets the hierarchical asset groups (used for Location and Region modes).
        /// Region > System > Station > Items.
        /// </summary>
        public List<AssetRegionGroup> HierarchicalGroups => _hierarchicalGroups;

        /// <summary>
        /// Gets whether the current group mode uses hierarchical display.
        /// </summary>
        public bool IsHierarchical => _groupMode <= 1;

        /// <summary>
        /// Gets or sets the grouping mode: 0=Location, 1=Region, 2=Category, 3=Container.
        /// </summary>
        public int GroupMode
        {
            get => _groupMode;
            set
            {
                if (_groupMode != value)
                {
                    _groupMode = value;
                    BuildGroups();
                }
            }
        }

        /// <summary>
        /// Gets or sets the text filter for assets.
        /// </summary>
        public string Filter
        {
            get => _filter;
            set
            {
                var newFilter = value ?? string.Empty;
                if (_filter != newFilter)
                {
                    _filter = newFilter;
                    BuildGroups();
                }
            }
        }

        /// <summary>
        /// Gets the total item count (sum of quantities).
        /// </summary>
        public long TotalItems => _totalItems;

        /// <summary>
        /// Gets the total estimated value in ISK.
        /// </summary>
        public double TotalValue => _totalValue;

        /// <summary>
        /// Gets the number of asset groups.
        /// </summary>
        public int GroupCount => _groupCount;

        /// <summary>
        /// Rebuilds asset data when the character changes.
        /// </summary>
        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            Reload();
        }

        private void Reload()
        {
            if (Character is CCPCharacter ccp)
            {
                _allAssets = ccp.Assets
                    .Where(a => a.Item != null && a.SolarSystem != null)
                    .ToList();
            }
            else
            {
                _allAssets = null;
            }

            BuildGroups();
        }

        /// <summary>
        /// Builds the asset groups based on the current filter and grouping mode.
        /// </summary>
        public void BuildGroups()
        {
            if (_allAssets == null)
            {
                _groups = new List<AssetBrowserGroupEntry>();
                _hierarchicalGroups = new List<AssetRegionGroup>();
                _totalItems = 0;
                _totalValue = 0;
                _groupCount = 0;
                return;
            }

            var filtered = _allAssets.AsEnumerable();

            if (!string.IsNullOrEmpty(_filter))
            {
                filtered = filtered.Where(a =>
                    (a.Item?.Name ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                    (a.Location ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase) ||
                    (a.Item?.GroupName ?? "").Contains(_filter, StringComparison.OrdinalIgnoreCase));
            }

            var items = filtered.ToList();

            if (IsHierarchical)
            {
                _groups = new List<AssetBrowserGroupEntry>();
                _hierarchicalGroups = BuildHierarchicalGroups(items);
                _groupCount = _hierarchicalGroups.Count;
            }
            else
            {
                _hierarchicalGroups = new List<AssetRegionGroup>();

                Func<Asset, string> groupKeySelector = _groupMode switch
                {
                    2 => a => a.Item?.CategoryName ?? "Unknown",
                    3 => a => string.IsNullOrEmpty(a.Container) ? "(No container)" : a.Container,
                    _ => a => a.Location ?? "Unknown"
                };

                _groups = items
                    .GroupBy(groupKeySelector)
                    .OrderBy(g => g.Key)
                    .Select(g => new AssetBrowserGroupEntry(g.Key, g.ToList()))
                    .ToList();

                _groupCount = _groups.Count;
            }

            _totalItems = items.Sum(a => a.Quantity);
            _totalValue = items.Sum(a => a.Cost);
        }

        private static List<AssetRegionGroup> BuildHierarchicalGroups(List<Asset> items)
        {
            return items
                .GroupBy(a => a.SolarSystem?.Constellation?.Region?.Name ?? "Unknown")
                .OrderBy(rg => rg.Key)
                .Select(regionGroup =>
                {
                    var systems = regionGroup
                        .GroupBy(a => a.SolarSystem?.Name ?? "Unknown")
                        .OrderBy(sg => sg.Key)
                        .Select(systemGroup =>
                        {
                            var stations = systemGroup
                                .GroupBy(a => a.Location ?? "Unknown")
                                .OrderBy(stg => stg.Key)
                                .Select(stationGroup => new AssetStationGroup(
                                    stationGroup.Key,
                                    stationGroup.ToList()))
                                .ToList();

                            return new AssetSystemGroup(systemGroup.Key, stations);
                        })
                        .ToList();

                    return new AssetRegionGroup(regionGroup.Key, systems);
                })
                .ToList();
        }

        /// <summary>
        /// Collapses all asset groups (both flat and hierarchical).
        /// </summary>
        public void CollapseAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = false;

            foreach (var region in _hierarchicalGroups)
            {
                region.IsExpanded = false;
                foreach (var system in region.Systems)
                {
                    system.IsExpanded = false;
                    foreach (var station in system.Stations)
                        station.IsExpanded = false;
                }
            }
        }

        /// <summary>
        /// Expands all asset groups (both flat and hierarchical).
        /// </summary>
        public void ExpandAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = true;

            foreach (var region in _hierarchicalGroups)
            {
                region.IsExpanded = true;
                foreach (var system in region.Systems)
                {
                    system.IsExpanded = true;
                    foreach (var station in system.Stations)
                        station.IsExpanded = true;
                }
            }
        }
    }

    /// <summary>
    /// Represents a region-level group in the hierarchical asset view.
    /// </summary>
    public sealed class AssetRegionGroup : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; }
        public List<AssetSystemGroup> Systems { get; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded == value) return; _isExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded))); PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Chevron))); }
        }
        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";
        public string CountText => $"{ItemCount} items";
        public string SummaryText => TotalValue > 0 ? $"{TotalValue:N0} ISK" : "";

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public AssetRegionGroup(string name, List<AssetSystemGroup> systems)
        {
            Name = name;
            Systems = systems;
            ItemCount = systems.Sum(s => s.ItemCount);
            TotalQuantity = systems.Sum(s => s.TotalQuantity);
            TotalValue = systems.Sum(s => s.TotalValue);
        }
    }

    /// <summary>
    /// Represents a solar system-level group in the hierarchical asset view.
    /// </summary>
    public sealed class AssetSystemGroup : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; }
        public List<AssetStationGroup> Stations { get; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded == value) return; _isExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded))); PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Chevron))); }
        }
        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";
        public string CountText => $"{ItemCount} items";

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public AssetSystemGroup(string name, List<AssetStationGroup> stations)
        {
            Name = name;
            Stations = stations;
            ItemCount = stations.Sum(st => st.ItemCount);
            TotalQuantity = stations.Sum(st => st.TotalQuantity);
            TotalValue = stations.Sum(st => st.TotalValue);
        }
    }

    /// <summary>
    /// Represents a station-level group in the hierarchical asset view.
    /// </summary>
    public sealed class AssetStationGroup : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; }
        public List<AssetBrowserItemEntry> Items { get; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded == value) return; _isExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded))); PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Chevron))); }
        }
        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";
        public string CountText => $"{ItemCount} items";
        public string SummaryText => TotalValue > 0 ? $"{TotalValue:N0} ISK" : "";

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public AssetStationGroup(string name, List<Asset> assets)
        {
            Name = name;
            ItemCount = assets.Count;
            TotalQuantity = assets.Sum(a => a.Quantity);
            TotalValue = assets.Sum(a => a.Cost);
            Items = assets
                .OrderBy(a => a.Item.Name)
                .Select(a => new AssetBrowserItemEntry(a))
                .ToList();
        }
    }

    /// <summary>
    /// Represents a group of assets with summary information (flat grouping).
    /// </summary>
    public sealed class AssetBrowserGroupEntry : System.ComponentModel.INotifyPropertyChanged
    {
        private bool _isExpanded;

        public string Name { get; }
        public List<AssetBrowserItemEntry> Items { get; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }
        public double TotalVolume { get; }

        public bool IsExpanded
        {
            get => _isExpanded;
            set { if (_isExpanded == value) return; _isExpanded = value; PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsExpanded))); PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(Chevron))); }
        }
        public string Chevron => _isExpanded ? "\u25BE" : "\u25B8";
        public string CountText => $"{ItemCount} items";
        public string SummaryText => TotalValue > 0 ? $"{TotalValue:N0} ISK" : "";

        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;

        public AssetBrowserGroupEntry(string name, List<Asset> assets)
        {
            Name = name;
            ItemCount = assets.Count;
            TotalQuantity = assets.Sum(a => a.Quantity);
            TotalValue = assets.Sum(a => a.Cost);
            TotalVolume = assets.Sum(a => a.TotalVolume);
            Items = assets
                .OrderBy(a => a.Item.Name)
                .Select(a => new AssetBrowserItemEntry(a))
                .ToList();
        }
    }

    /// <summary>
    /// Represents a single asset item entry.
    /// </summary>
    public sealed class AssetBrowserItemEntry
    {
        public string ItemName { get; }
        public string QuantityText { get; }
        public string LocationShort { get; }
        public string ValueText { get; }

        public AssetBrowserItemEntry(Asset asset)
        {
            ItemName = asset.Item?.Name ?? "Unknown";
            QuantityText = asset.Quantity > 1 ? $"x{asset.Quantity:N0}" : "";
            LocationShort = !string.IsNullOrEmpty(asset.Container) ? asset.Container : "";
            ValueText = asset.Cost > 0 ? $"{asset.Cost:N0} ISK" : "";
        }
    }
}
