// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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
    /// ViewModel for the character EVE notifications list.
    /// </summary>
    public sealed class NotificationsListViewModel : ListViewModel<EveNotification, EveNotificationColumn, EVENotificationsGrouping>
    {
        public NotificationsListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterEVENotificationsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        public NotificationsListViewModel() : base()
        {
            SubscribeForCharacter<CharacterEVENotificationsUpdatedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
        }

        protected override IEnumerable<EveNotification> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<EveNotification>();

            return ccp.EVENotifications;
        }

        protected override bool MatchesFilter(EveNotification x, string filter)
        {
            return x.SenderName.Contains(filter, ignoreCase: true) ||
                   x.Title.Contains(filter, ignoreCase: true) ||
                   x.Text.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(EveNotification x, EveNotification y, EveNotificationColumn column)
        {
            return column switch
            {
                EveNotificationColumn.SentDate => x.SentDate.CompareTo(y.SentDate),
                EveNotificationColumn.SenderName => string.Compare(x.SenderName, y.SenderName, StringComparison.OrdinalIgnoreCase),
                EveNotificationColumn.Type => string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(EveNotification item, EVENotificationsGrouping grouping)
        {
            return grouping switch
            {
                EVENotificationsGrouping.Type or EVENotificationsGrouping.TypeDesc => item.Title,
                EVENotificationsGrouping.SentDate or EVENotificationsGrouping.SentDateDesc => item.SentDate.ToShortDateString(),
                EVENotificationsGrouping.Sender or EVENotificationsGrouping.SenderDesc => item.SenderName,
                _ => string.Empty
            };
        }
    }
}
