using System;
using System.Collections.Generic;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character wallet transactions list.
    /// </summary>
    public sealed class WalletTransactionsListViewModel : ListViewModel<WalletTransaction, WalletTransactionColumn, WalletTransactionGrouping>
    {
        public WalletTransactionsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterWalletTransactionsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        public WalletTransactionsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterWalletTransactionsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<WalletTransaction> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<WalletTransaction>();

            return ccp.WalletTransactions;
        }

        protected override bool MatchesFilter(WalletTransaction x, string filter)
        {
            return x.ItemName.Contains(filter, ignoreCase: true) ||
                   x.ClientName.Contains(filter, ignoreCase: true) ||
                   x.Station.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Name.Contains(filter, ignoreCase: true) ||
                   x.Station.SolarSystemChecked.Constellation.Region.Name.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(WalletTransaction x, WalletTransaction y, WalletTransactionColumn column)
        {
            return column switch
            {
                WalletTransactionColumn.Date => x.Date.CompareTo(y.Date),
                WalletTransactionColumn.ItemName => string.Compare(x.ItemName, y.ItemName, StringComparison.OrdinalIgnoreCase),
                WalletTransactionColumn.Price => x.Price.CompareTo(y.Price),
                WalletTransactionColumn.Quantity => x.Quantity.CompareTo(y.Quantity),
                WalletTransactionColumn.Credit => x.Credit.CompareTo(y.Credit),
                WalletTransactionColumn.Client => string.Compare(x.ClientName, y.ClientName, StringComparison.OrdinalIgnoreCase),
                WalletTransactionColumn.Station => string.Compare(x.Station.Name, y.Station.Name, StringComparison.OrdinalIgnoreCase),
                WalletTransactionColumn.SolarSystem => string.Compare(x.Station.SolarSystemChecked.Name, y.Station.SolarSystemChecked.Name, StringComparison.OrdinalIgnoreCase),
                WalletTransactionColumn.Region => string.Compare(x.Station.SolarSystemChecked.Constellation.Region.Name, y.Station.SolarSystemChecked.Constellation.Region.Name, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(WalletTransaction item, WalletTransactionGrouping grouping)
        {
            return grouping switch
            {
                WalletTransactionGrouping.Date or WalletTransactionGrouping.DateDesc => item.Date.ToShortDateString(),
                WalletTransactionGrouping.ItemType or WalletTransactionGrouping.ItemTypeDesc => item.ItemName,
                WalletTransactionGrouping.Client or WalletTransactionGrouping.ClientDesc => item.ClientName,
                WalletTransactionGrouping.Location or WalletTransactionGrouping.LocationDesc => item.Station.Name,
                _ => string.Empty
            };
        }
    }
}
