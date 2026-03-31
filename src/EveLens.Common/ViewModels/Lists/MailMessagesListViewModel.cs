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
    /// ViewModel for the character EVE mail messages list.
    /// </summary>
    public sealed class MailMessagesListViewModel : ListViewModel<EveMailMessage, EveMailMessageColumn, EVEMailMessagesGrouping>
    {
        public MailMessagesListViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterEVEMailMessagesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterEVEMailBodyDownloadedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        public MailMessagesListViewModel() : base()
        {
            SubscribeForCharacter<CharacterEVEMailMessagesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterEVEMailBodyDownloadedEvent>(e => Refresh());
            Subscribe<SettingsChangedEvent>(e => Refresh());
            Subscribe<EveIDToNameUpdatedEvent>(e => Refresh());
        }

        protected override IEnumerable<EveMailMessage> GetSourceItems()
        {
            if (Character is not CCPCharacter ccp)
                return Array.Empty<EveMailMessage>();

            return ccp.EVEMailMessages;
        }

        /// <summary>
        /// Gets all mail items for flat display (no grouping, for Gmail-style view).
        /// </summary>
        public IEnumerable<EveMailMessage> GetSourceItemsForDisplay() => GetSourceItems();

        protected override bool MatchesFilter(EveMailMessage x, string filter)
        {
            return x.SenderName.Contains(filter, ignoreCase: true) ||
                   x.Title.Contains(filter, ignoreCase: true) ||
                   x.ToCorpOrAlliance.Contains(filter, ignoreCase: true) ||
                   x.ToCharacters.Any(y => y.Contains(filter, ignoreCase: true)) ||
                   x.EVEMailBody.BodyText.Contains(filter, ignoreCase: true);
        }

        protected override int CompareItems(EveMailMessage x, EveMailMessage y, EveMailMessageColumn column)
        {
            return column switch
            {
                EveMailMessageColumn.SentDate => x.SentDate.CompareTo(y.SentDate),
                EveMailMessageColumn.SenderName => string.Compare(x.SenderName, y.SenderName, StringComparison.OrdinalIgnoreCase),
                EveMailMessageColumn.Title => string.Compare(x.Title, y.Title, StringComparison.OrdinalIgnoreCase),
                EveMailMessageColumn.ToCorpOrAlliance => string.Compare(x.ToCorpOrAlliance, y.ToCorpOrAlliance, StringComparison.OrdinalIgnoreCase),
                _ => 0
            };
        }

        protected override string GetGroupKey(EveMailMessage item, EVEMailMessagesGrouping grouping)
        {
            return grouping switch
            {
                EVEMailMessagesGrouping.State or EVEMailMessagesGrouping.StateDesc => item.State.ToString(),
                EVEMailMessagesGrouping.SentDate or EVEMailMessagesGrouping.SentDateDesc => item.SentDate.ToShortDateString(),
                EVEMailMessagesGrouping.Sender or EVEMailMessagesGrouping.SenderDesc => item.SenderName,
                EVEMailMessagesGrouping.Subject or EVEMailMessagesGrouping.SubjectDesc => item.Title,
                EVEMailMessagesGrouping.Recipient or EVEMailMessagesGrouping.RecipientDesc => string.Join(", ", item.ToCharacters),
                EVEMailMessagesGrouping.CorpOrAlliance or EVEMailMessagesGrouping.CorpOrAllianceDesc => item.ToCorpOrAlliance,
                _ => string.Empty
            };
        }

        protected override DateTime GetItemTimestamp(EveMailMessage item) => item.SentDate;
    }
}
