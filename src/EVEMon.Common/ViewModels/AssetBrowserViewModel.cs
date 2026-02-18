using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the asset browser view showing character assets grouped by location, region, category, or container.
    /// </summary>
    public sealed class AssetBrowserViewModel : CharacterViewModelBase
    {
        private List<Asset>? _allAssets;
        private List<AssetBrowserGroupEntry> _groups = new();
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
        /// Gets the list of asset groups after grouping and filtering.
        /// </summary>
        public List<AssetBrowserGroupEntry> Groups => _groups;

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
                .Select(g => new AssetBrowserGroupEntry(g.Key, g.ToList()))
                .ToList();

            _totalItems = items.Sum(a => a.Quantity);
            _totalValue = items.Sum(a => a.Cost);
            _groupCount = _groups.Count;
        }

        /// <summary>
        /// Collapses all asset groups.
        /// </summary>
        public void CollapseAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = false;
        }

        /// <summary>
        /// Expands all asset groups.
        /// </summary>
        public void ExpandAll()
        {
            foreach (var g in _groups)
                g.IsExpanded = true;
        }
    }

    /// <summary>
    /// Represents a group of assets with summary information.
    /// </summary>
    public sealed class AssetBrowserGroupEntry
    {
        public string Name { get; }
        public List<AssetBrowserItemEntry> Items { get; }
        public bool IsExpanded { get; set; }
        public int ItemCount { get; }
        public long TotalQuantity { get; }
        public double TotalValue { get; }
        public double TotalVolume { get; }

        public string ItemCountText => $"{ItemCount} items ({TotalQuantity:N0} units)";
        public string ValueText => TotalValue > 0 ? $"{TotalValue:N0} ISK" : "";
        public string VolumeText => $"{TotalVolume:N0} m³";

        public AssetBrowserGroupEntry(string name, List<Asset> assets)
        {
            Name = name;
            ItemCount = assets.Count;
            TotalQuantity = assets.Sum(a => a.Quantity);
            TotalValue = assets.Sum(a => a.Cost);
            TotalVolume = assets.Sum(a => a.TotalVolume);
            IsExpanded = true;
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
