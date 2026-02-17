using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Enumerations;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character market orders list.
    /// </summary>
    public sealed class MarketOrdersListViewModel : ListViewModel<MarketOrder, MarketOrderColumn, MarketOrderGrouping>
    {
        private bool _hideInactive;
        private IssuedFor _showIssuedFor = IssuedFor.All;

        public MarketOrdersListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<MarketOrdersUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterMarketOrdersUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        public MarketOrdersListViewModel() : base()
        {
            SubscribeForCharacter<MarketOrdersUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterMarketOrdersUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        /// <summary>
        /// Gets or sets whether to hide inactive orders.
        /// </summary>
        public bool HideInactive
        {
            get => _hideInactive;
            set { if (SetProperty(ref _hideInactive, value)) Refresh(); }
        }

        /// <summary>
        /// Gets or sets the issued-for filter.
        /// </summary>
        public IssuedFor ShowIssuedFor
        {
            get => _showIssuedFor;
            set { if (SetProperty(ref _showIssuedFor, value)) Refresh(); }
        }

        protected override IEnumerable<MarketOrder> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<MarketOrder>();

            IEnumerable<MarketOrder> orders = ccp.MarketOrders
                .Where(x => x.Item != null && x.Station != null);

            if (_hideInactive)
                orders = orders.Where(x => x.IsAvailable);

            if (_showIssuedFor != IssuedFor.All)
                orders = orders.Where(x => x.IssuedFor == _showIssuedFor);

            return orders;
        }

        protected override bool MatchesFilter(MarketOrder x, string filter)
        {
            return x.Item.Name.Contains(filter, ignoreCase: true) ||
                   x.Item.Description.Contains(filter, ignoreCase: true) ||
                   x.Station.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Region.Name.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(MarketOrder x, MarketOrder y, MarketOrderColumn column)
        {
            return column switch
            {
                MarketOrderColumn.Item => string.Compare(x.Item.Name, y.Item.Name, StringComparison.OrdinalIgnoreCase),
                MarketOrderColumn.UnitaryPrice => x.UnitaryPrice.CompareTo(y.UnitaryPrice),
                MarketOrderColumn.TotalPrice => x.TotalPrice.CompareTo(y.TotalPrice),
                MarketOrderColumn.Volume => x.RemainingVolume.CompareTo(y.RemainingVolume),
                MarketOrderColumn.RemainingVolume => x.RemainingVolume.CompareTo(y.RemainingVolume),
                MarketOrderColumn.InitialVolume => x.InitialVolume.CompareTo(y.InitialVolume),
                MarketOrderColumn.Issued => x.Issued.CompareTo(y.Issued),
                MarketOrderColumn.Expiration => x.Expiration.CompareTo(y.Expiration),
                MarketOrderColumn.Station => string.Compare(x.Station.Name, y.Station.Name, StringComparison.OrdinalIgnoreCase),
                MarketOrderColumn.SolarSystem => string.Compare(x.Station.SolarSystemChecked.Name, y.Station.SolarSystemChecked.Name, StringComparison.OrdinalIgnoreCase),
                MarketOrderColumn.Region => string.Compare(x.Station.SolarSystemChecked.Constellation.Region.Name, y.Station.SolarSystemChecked.Constellation.Region.Name, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(MarketOrder item, MarketOrderGrouping grouping)
        {
            return grouping switch
            {
                MarketOrderGrouping.State or MarketOrderGrouping.StateDesc => item.State.ToString(),
                MarketOrderGrouping.OrderType or MarketOrderGrouping.OrderTypeDesc => item is BuyOrder ? "Buying Orders" : "Selling Orders",
                MarketOrderGrouping.Issued or MarketOrderGrouping.IssuedDesc => item.Issued.ToShortDateString(),
                MarketOrderGrouping.ItemType or MarketOrderGrouping.ItemTypeDesc => item.Item.GroupName,
                MarketOrderGrouping.Location or MarketOrderGrouping.LocationDesc => item.Station.Name,
                _ => string.Empty
            };
        }
    }
}
