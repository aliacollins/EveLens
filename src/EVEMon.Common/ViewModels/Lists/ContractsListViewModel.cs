using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.CCPAPI;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character contracts list.
    /// </summary>
    public sealed class ContractsListViewModel : ListViewModel<Contract, ContractColumn, ContractGrouping>
    {
        private bool _hideInactive;
        private IssuedFor _showIssuedFor = IssuedFor.All;
        private int _outstandingCount;
        private int _completedCount;

        public ContractsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<ContractsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
            PropertyChanged += OnSelfPropertyChanged;
        }

        public ContractsListViewModel() : base()
        {
            SubscribeForCharacter<ContractsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<ConquerableStationListUpdatedEvent>(e => Refresh());
            PropertyChanged += OnSelfPropertyChanged;
        }

        private void OnSelfPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Items))
                UpdateCounts();
        }

        private void UpdateCounts()
        {
            int outstanding = 0;
            int completed = 0;
            foreach (var item in Items)
            {
                if (item.IsAvailable)
                    outstanding++;
                else if (item.State == ContractState.Finished)
                    completed++;
            }
            OutstandingCount = outstanding;
            CompletedCount = completed;
        }

        public bool HideInactive
        {
            get => _hideInactive;
            set { if (SetProperty(ref _hideInactive, value)) Refresh(); }
        }

        public IssuedFor ShowIssuedFor
        {
            get => _showIssuedFor;
            set { if (SetProperty(ref _showIssuedFor, value)) Refresh(); }
        }

        /// <summary>
        /// Gets the count of outstanding (active) contracts after filtering.
        /// </summary>
        public int OutstandingCount
        {
            get => _outstandingCount;
            private set => SetProperty(ref _outstandingCount, value);
        }

        /// <summary>
        /// Gets the count of completed (finished) contracts after filtering.
        /// </summary>
        public int CompletedCount
        {
            get => _completedCount;
            private set => SetProperty(ref _completedCount, value);
        }

        /// <summary>
        /// Returns the issued date for new-item tracking.
        /// </summary>
        protected override DateTime GetItemTimestamp(Contract item) => item.Issued;

        protected override IEnumerable<Contract> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<Contract>();

            IEnumerable<Contract> contracts = ccp.Contracts
                .Where(x => x.ContractType != ContractType.None &&
                             x.StartStation != null && x.EndStation != null);

            if (_hideInactive)
                contracts = contracts.Where(x => x.IsAvailable);

            if (_showIssuedFor != IssuedFor.All)
                contracts = contracts.Where(x => x.IssuedFor == _showIssuedFor);

            return contracts;
        }

        protected override bool MatchesFilter(Contract x, string filter)
        {
            return x.ContractText.Contains(filter, ignoreCase: true) ||
                   x.ContractType.GetDescription().Contains(filter, ignoreCase: true) ||
                   x.Status.GetDescription().Contains(filter, ignoreCase: true) ||
                   x.Issuer.Contains(filter, ignoreCase: true) ||
                   x.Assignee.Contains(filter, ignoreCase: true) ||
                   x.Acceptor.Contains(filter, ignoreCase: true) ||
                   x.Description.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(Contract x, Contract y, ContractColumn column)
        {
            return column switch
            {
                ContractColumn.ContractText => string.Compare(x.ContractText, y.ContractText, StringComparison.OrdinalIgnoreCase),
                ContractColumn.ContractType => x.ContractType.CompareTo(y.ContractType),
                ContractColumn.Status => x.Status.CompareTo(y.Status),
                ContractColumn.Issuer => string.Compare(x.Issuer, y.Issuer, StringComparison.OrdinalIgnoreCase),
                ContractColumn.Assignee => string.Compare(x.Assignee, y.Assignee, StringComparison.OrdinalIgnoreCase),
                ContractColumn.Price => x.Price.CompareTo(y.Price),
                ContractColumn.Issued => x.Issued.CompareTo(y.Issued),
                ContractColumn.Expiration => x.Expiration.CompareTo(y.Expiration),
                _ => 0
            };
        }

        protected override string GetGroupKey(Contract item, ContractGrouping grouping)
        {
            return grouping switch
            {
                ContractGrouping.State or ContractGrouping.StateDesc => item.Status.GetDescription(),
                ContractGrouping.ContractType or ContractGrouping.ContractTypeDesc => item.ContractType.GetDescription(),
                ContractGrouping.Issued or ContractGrouping.IssuedDesc => item.Issued.ToShortDateString(),
                ContractGrouping.StartLocation or ContractGrouping.StartLocationDesc => item.StartStation?.Name ?? string.Empty,
                _ => string.Empty
            };
        }
    }
}
