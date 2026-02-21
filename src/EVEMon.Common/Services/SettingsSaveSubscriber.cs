// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EVEMon.Common.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Subscribes to domain events and persists settings when needed.
    /// Extracted from EveMonClient.OnXxx() methods to decouple Models from Settings.Save().
    /// </summary>
    /// <remarks>
    /// Previously, Model files had to call EveMonClient.OnXxx() which bundled:
    ///   1. Trace() logging
    ///   2. Settings.Save()
    ///   3. EventAggregator.Publish()
    ///
    /// With this subscriber, Models can publish events directly and Settings.Save()
    /// is handled here automatically. The OnXxx() methods in EveMonClient.Events.cs
    /// still exist for backward compatibility with UI code that calls them.
    /// </remarks>
    public sealed class SettingsSaveSubscriber : IDisposable
    {
        private readonly IEventAggregator _aggregator;
        private readonly IDisposable[] _subscriptions;

        public SettingsSaveSubscriber(IEventAggregator aggregator)
        {
            _aggregator = aggregator ?? throw new ArgumentNullException(nameof(aggregator));

            // Subscribe to all CommonEvents that previously triggered Settings.Save()
            // in EveMonClient.OnXxx() methods.
            _subscriptions = new[]
            {
                // Parameterless singleton events
                _aggregator.Subscribe<SettingsChangedEvent>(OnSave),
                _aggregator.Subscribe<SchedulerChangedEvent>(OnSave),
                _aggregator.Subscribe<ESIKeyCollectionChangedEvent>(OnSave),
                _aggregator.Subscribe<ESIKeyMonitoredChangedEvent>(OnSave),
                _aggregator.Subscribe<MonitoredCharacterCollectionChangedEvent>(OnSave),
                _aggregator.Subscribe<CharacterCollectionChangedEvent>(OnSave),
                _aggregator.Subscribe<ESIKeyInfoUpdatedEvent>(OnSave),
                _aggregator.Subscribe<AccountStatusUpdatedEvent>(OnSave),

                // Character-scoped events
                _aggregator.Subscribe<CharacterListUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterImplantSetCollectionChangedEvent>(OnSave),
                _aggregator.Subscribe<CharacterUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterInfoUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterSkillQueueUpdatedEvent>(OnSave),
                _aggregator.Subscribe<MarketOrdersUpdatedEvent>(OnSave),
                _aggregator.Subscribe<ContractsUpdatedEvent>(OnSave),
                _aggregator.Subscribe<IndustryJobsUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterEVEMailMessagesUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterEVENotificationsUpdatedEvent>(OnSave),
                _aggregator.Subscribe<CharacterPlanCollectionChangedEvent>(OnSave),

                // Plan events
                _aggregator.Subscribe<PlanChangedEvent>(OnSave),
                _aggregator.Subscribe<PlanNameChangedEvent>(OnSave),
            };
        }

        private void OnSave<T>(T _) where T : class
        {
            Settings.Save();
        }

        public void Dispose()
        {
            foreach (var sub in _subscriptions)
                sub?.Dispose();
        }
    }
}
