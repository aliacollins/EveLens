using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character assets list. First concrete ListViewModel subclass (proof of concept).
    /// Replaces ~1000 lines of filter/sort/group logic in CharacterAssetsList.cs with ~140 lines.
    /// </summary>
    public sealed class AssetsListViewModel : ListViewModel<Asset, AssetColumn, AssetGrouping>
    {
        public AssetsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterAssetsUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterInfoUpdatedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
            Subscribe<EveFlagsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ItemPricesUpdatedEvent>(e => Refresh());
        }

        public AssetsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterAssetsUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterInfoUpdatedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
            Subscribe<EveFlagsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ItemPricesUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<Asset> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<Asset>();

            return ccp.Assets.Where(x => x.Item != null && x.SolarSystem != null);
        }

        protected override bool MatchesFilter(Asset x, string filter)
        {
            return ((x.Item?.ID ?? 0) != 0 && (x.Item!.Name.Contains(filter, ignoreCase: true) ||
                    x.Item.GroupName.Contains(filter, ignoreCase: true) ||
                    x.Item.CategoryName.Contains(filter, ignoreCase: true) ||
                    x.TypeOfBlueprint.Contains(filter, ignoreCase: true))) ||
                   x.Container.Contains(filter, ignoreCase: true) ||
                   x.Flag.Contains(filter, ignoreCase: true) ||
                   x.Location.Contains(filter, ignoreCase: true) ||
                   ((x.SolarSystem?.ID ?? 0) != 0 &&
                       (x.SolarSystem!.Name.Contains(filter, ignoreCase: true) ||
                       x.SolarSystem.Constellation.Name.Contains(filter, ignoreCase: true) ||
                       x.SolarSystem.Constellation.Region.Name.Contains(filter, ignoreCase: true)));
        }

        protected override int CompareItems(Asset x, Asset y, AssetColumn column)
        {
            return column switch
            {
                AssetColumn.ItemName => string.Compare(x.Item.Name, y.Item.Name, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Quantity => x.Quantity.CompareTo(y.Quantity),
                AssetColumn.UnitaryPrice => x.Price.CompareTo(y.Price),
                AssetColumn.TotalPrice => x.Cost.CompareTo(y.Cost),
                AssetColumn.Volume => x.TotalVolume.CompareTo(y.TotalVolume),
                AssetColumn.BlueprintType => string.Compare(x.TypeOfBlueprint, y.TypeOfBlueprint, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Group => string.Compare(x.Item.GroupName, y.Item.GroupName, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Category => string.Compare(x.Item.CategoryName, y.Item.CategoryName, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Container => string.Compare(x.Container, y.Container, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Flag => string.Compare(x.Flag, y.Flag, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Location => string.Compare(x.Location, y.Location, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Region => string.Compare(
                    x.SolarSystem.Constellation.Region.Name,
                    y.SolarSystem.Constellation.Region.Name,
                    StringComparison.OrdinalIgnoreCase),
                AssetColumn.SolarSystem => string.Compare(x.SolarSystem.Name, y.SolarSystem.Name, StringComparison.OrdinalIgnoreCase),
                AssetColumn.FullLocation => string.Compare(x.FullLocation, y.FullLocation, StringComparison.OrdinalIgnoreCase),
                AssetColumn.Jumps => x.Jumps.CompareTo(y.Jumps),
                _ => 0
            };
        }

        protected override string GetGroupKey(Asset item, AssetGrouping grouping)
        {
            return grouping switch
            {
                AssetGrouping.Group or AssetGrouping.GroupDesc => item.Item.GroupName,
                AssetGrouping.Category or AssetGrouping.CategoryDesc => item.Item.CategoryName,
                AssetGrouping.Container or AssetGrouping.ContainerDesc => item.Container,
                AssetGrouping.Location or AssetGrouping.LocationDesc => item.Location,
                AssetGrouping.Region or AssetGrouping.RegionDesc => item.SolarSystem.Constellation.Region.Name,
                AssetGrouping.Jumps or AssetGrouping.JumpsDesc => item.JumpsText,
                _ => string.Empty
            };
        }
    }
}
