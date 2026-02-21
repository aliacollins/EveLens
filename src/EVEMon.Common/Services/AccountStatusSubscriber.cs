// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Models;
using EVEMon.Core.Interfaces;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Subscribes to collection events and handles account status updates.
    /// Extracted from EveMonClient.OnESIKeyCollectionChanged() and OnCharacterSkillQueueUpdated()
    /// to decouple business logic from event publishing.
    /// </summary>
    public sealed class AccountStatusSubscriber : IDisposable
    {
        private readonly IDisposable[] _subscriptions;

        public AccountStatusSubscriber(IEventAggregator aggregator)
        {
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

            _subscriptions = new[]
            {
                // When ESI key collection changes, update all character account statuses
                aggregator.Subscribe<CommonEvents.ESIKeyCollectionChangedEvent>(_ =>
                    AppServices.Characters?.UpdateAccountStatuses()),

                // When a character's skill queue is updated, update that character's account status
                aggregator.Subscribe<CommonEvents.CharacterSkillQueueUpdatedEvent>(e =>
                    e.Character?.UpdateAccountStatus()),
            };
        }

        private readonly IEventAggregator _aggregator;

        private static AccountStatusSubscriber s_instance;

        /// <summary>
        /// Initializes the singleton instance. Call from EveMonClient.Initialize().
        /// </summary>
        public static void Initialize()
        {
            s_instance?.Dispose();
            s_instance = new AccountStatusSubscriber(AppServices.EventAggregator);
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
        }
    }
}
