// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Enumerations.UISettings;
using EveLens.Common.Events;
using EveLens.Common.Extensions;
using EveLens.Common.Models;
using EveLens.Common.SettingsObjects;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character wallet journal list.
    /// </summary>
    public sealed class WalletJournalListViewModel : ListViewModel<WalletJournal, WalletJournalColumn, WalletJournalGrouping>
    {
        private string? _typeFilter;
        private IReadOnlyList<string> _availableTypes = Array.Empty<string>();
        private decimal _netAmount;

        public WalletJournalListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterWalletJournalUpdatedEvent>(e => { UpdateAvailableTypes(); Refresh(); });
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
            PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Items)) UpdateNetAmount(); };
        }

        public WalletJournalListViewModel() : base()
        {
            SubscribeForCharacter<CharacterWalletJournalUpdatedEvent>(e => { UpdateAvailableTypes(); Refresh(); });
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
            PropertyChanged += (_, e) => { if (e.PropertyName == nameof(Items)) UpdateNetAmount(); };
        }

        /// <summary>
        /// Gets or sets the type filter. When set, only journal entries matching this type are shown.
        /// </summary>
        public string? TypeFilter
        {
            get => _typeFilter;
            set
            {
                if (SetProperty(ref _typeFilter, value))
                    Refresh();
            }
        }

        /// <summary>
        /// Gets the distinct types available in the source data for filtering.
        /// </summary>
        public IReadOnlyList<string> AvailableTypes
        {
            get => _availableTypes;
            private set => SetProperty(ref _availableTypes, value);
        }

        /// <summary>
        /// Gets the net amount (sum of all filtered item amounts).
        /// </summary>
        public decimal NetAmount
        {
            get => _netAmount;
            private set => SetProperty(ref _netAmount, value);
        }

        protected override IEnumerable<WalletJournal> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<WalletJournal>();

            var items = ccp.WalletJournal;

            if (!string.IsNullOrEmpty(_typeFilter))
                return items.Where(j => string.Equals(j.Type, _typeFilter, StringComparison.OrdinalIgnoreCase));

            return items;
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

        protected override DateTime GetItemTimestamp(WalletJournal item) => item.Date;

        protected override void OnCharacterChanged()
        {
            UpdateAvailableTypes();
            base.OnCharacterChanged();
        }

        private void UpdateAvailableTypes()
        {
            if (Character is not CCPCharacter ccp)
            {
                AvailableTypes = Array.Empty<string>();
                return;
            }

            AvailableTypes = ccp.WalletJournal
                .Select(j => j.Type)
                .Where(t => !string.IsNullOrEmpty(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void UpdateNetAmount()
        {
            NetAmount = Items.Count > 0
                ? Items.Sum(i => i.Amount)
                : 0m;
        }
    }
}
