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
    /// ViewModel for the character wallet journal list.
    /// </summary>
    public sealed class WalletJournalListViewModel : ListViewModel<WalletJournal, WalletJournalColumn, WalletJournalGrouping>
    {
        public WalletJournalListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterWalletJournalUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        public WalletJournalListViewModel() : base()
        {
            SubscribeForCharacter<CharacterWalletJournalUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<WalletJournal> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<WalletJournal>();

            return ccp.WalletJournal;
        }

        protected override bool MatchesFilter(WalletJournal x, string filter)
        {
            return x.Type.Contains(filter, ignoreCase: true) ||
                   x.Reason.Contains(filter, ignoreCase: true) ||
                   x.Issuer.Contains(filter, ignoreCase: true) ||
                   x.Recipient.Contains(filter, ignoreCase: true) ||
                   x.TaxReceiver.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(WalletJournal x, WalletJournal y, WalletJournalColumn column)
        {
            return column switch
            {
                WalletJournalColumn.Date => x.Date.CompareTo(y.Date),
                WalletJournalColumn.Type => string.Compare(x.Type, y.Type, StringComparison.OrdinalIgnoreCase),
                WalletJournalColumn.Amount => x.Amount.CompareTo(y.Amount),
                WalletJournalColumn.Balance => x.Balance.CompareTo(y.Balance),
                WalletJournalColumn.Reason => string.Compare(x.Reason, y.Reason, StringComparison.OrdinalIgnoreCase),
                WalletJournalColumn.Issuer => string.Compare(x.Issuer, y.Issuer, StringComparison.OrdinalIgnoreCase),
                WalletJournalColumn.Recipient => string.Compare(x.Recipient, y.Recipient, StringComparison.OrdinalIgnoreCase),
                WalletJournalColumn.TaxReceiver => string.Compare(x.TaxReceiver, y.TaxReceiver, StringComparison.OrdinalIgnoreCase),
                WalletJournalColumn.TaxAmount => x.TaxAmount.CompareTo(y.TaxAmount),
                WalletJournalColumn.ID => x.ID.CompareTo(y.ID),
                _ => 0
            };
        }

        protected override string GetGroupKey(WalletJournal item, WalletJournalGrouping grouping)
        {
            return grouping switch
            {
                WalletJournalGrouping.Date or WalletJournalGrouping.DateDesc => item.Date.ToShortDateString(),
                WalletJournalGrouping.Type or WalletJournalGrouping.TypeDesc => item.Type,
                WalletJournalGrouping.Issuer or WalletJournalGrouping.IssuerDesc => item.Issuer,
                WalletJournalGrouping.Recipient or WalletJournalGrouping.RecipientDesc => item.Recipient,
                _ => string.Empty
            };
        }
    }
}
