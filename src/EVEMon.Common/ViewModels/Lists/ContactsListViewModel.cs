// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Enumerations;
using EVEMon.Common.Enumerations.UISettings;
using EVEMon.Common.Events;
using EVEMon.Common.Extensions;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.ViewModels.Lists
{
    /// <summary>
    /// ViewModel for the character contacts list.
    /// </summary>
    public sealed class ContactsListViewModel : ListViewModel<Contact, ContactColumn, ContactGrouping>
    {
        public ContactsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterContactsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        public ContactsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterContactsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<Contact> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<Contact>();

            return ccp.Contacts;
        }

        protected override bool MatchesFilter(Contact x, string filter)
        {
            return x.Name.Contains(filter, ignoreCase: true) ||
                   x.Group.ToString().Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(Contact x, Contact y, ContactColumn column)
        {
            return column switch
            {
                ContactColumn.Name => string.Compare(x.Name, y.Name, StringComparison.OrdinalIgnoreCase),
                ContactColumn.Standing => x.Standing.CompareTo(y.Standing),
                ContactColumn.Group => x.Group.CompareTo(y.Group),
                ContactColumn.InWatchlist => x.IsInWatchlist.CompareTo(y.IsInWatchlist),
                _ => 0
            };
        }

        protected override string GetGroupKey(Contact item, ContactGrouping grouping)
        {
            return grouping switch
            {
                ContactGrouping.ContactGroup => item.Group.ToString(),
                ContactGrouping.StandingBracket => Standing.Status(item.Standing).ToString(),
                _ => string.Empty
            };
        }
    }
}
