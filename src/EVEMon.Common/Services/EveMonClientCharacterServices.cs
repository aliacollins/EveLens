using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;
using EVEMon.Core.Events;
using CommonEvents = EVEMon.Common.Events;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Production implementation that delegates to EveMonClient statics.
    /// Singleton -- all CCPCharacter instances share one.
    /// Subscribes to EventAggregator for timer and global events, then
    /// raises its own CLR events so that existing ICharacterServices
    /// consumers continue to work without change.
    /// </summary>
    internal sealed class EveMonClientCharacterServices : ICharacterServices, IDisposable
    {
        internal static readonly EveMonClientCharacterServices Instance = new();

        private EventHandler? _secondTick;
        private EventHandler? _fiveSecondTick;
        private EventHandler? _esiKeyInfoUpdated;
        private EventHandler? _eveIDToNameUpdated;

        private IDisposable? _subSecondTick;
        private IDisposable? _subFiveSecondTick;
        private IDisposable? _subESIKeyInfoUpdated;
        private IDisposable? _subEveIDToNameUpdated;

        private EveMonClientCharacterServices()
        {
            _subSecondTick = AppServices.EventAggregator?.Subscribe<SecondTickEvent>(
                e => _secondTick?.Invoke(this, EventArgs.Empty));
            _subFiveSecondTick = AppServices.EventAggregator?.Subscribe<FiveSecondTickEvent>(
                e => _fiveSecondTick?.Invoke(this, EventArgs.Empty));
            _subESIKeyInfoUpdated = AppServices.EventAggregator?.Subscribe<CommonEvents.ESIKeyInfoUpdatedEvent>(
                e => _esiKeyInfoUpdated?.Invoke(this, EventArgs.Empty));
            _subEveIDToNameUpdated = AppServices.EventAggregator?.Subscribe<CommonEvents.EveIDToNameUpdatedEvent>(
                e => _eveIDToNameUpdated?.Invoke(this, EventArgs.Empty));
        }

        public event EventHandler SecondTick
        {
            add => _secondTick += value;
            remove => _secondTick -= value;
        }

        public event EventHandler FiveSecondTick
        {
            add => _fiveSecondTick += value;
            remove => _fiveSecondTick -= value;
        }

        public event EventHandler ESIKeyInfoUpdated
        {
            add => _esiKeyInfoUpdated += value;
            remove => _esiKeyInfoUpdated -= value;
        }

        public event EventHandler EveIDToNameUpdated
        {
            add => _eveIDToNameUpdated += value;
            remove => _eveIDToNameUpdated -= value;
        }

        public void OnCharacterUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"CharacterUpdated: {c.Name}");
            AppServices.EventAggregator?.Publish(new CharacterUpdatedEvent(c.CharacterID, c.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterUpdatedEvent(c));
        }

        public void OnMarketOrdersUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"MarketOrdersUpdated: {c.Name}");
            AppServices.EventAggregator?.Publish(new CommonEvents.MarketOrdersUpdatedEvent(c));
        }

        public void OnContractsUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"ContractsUpdated: {c.Name}");
            AppServices.EventAggregator?.Publish(new CommonEvents.ContractsUpdatedEvent(c));
        }

        public void OnIndustryJobsUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"IndustryJobsUpdated: {c.Name}");
            AppServices.EventAggregator?.Publish(new CommonEvents.IndustryJobsUpdatedEvent(c));
        }

        public void OnCharacterInfoUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"CharacterInfoUpdated: {c.Name}");
            AppServices.EventAggregator?.Publish(new CharacterInfoUpdatedEvent(c.CharacterID, c.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterInfoUpdatedEvent(c));
        }

        public void OnCharacterSkillQueueUpdated(Character c)
        {
            AppServices.TraceService?.Trace($"CharacterSkillQueueUpdated: {c.Name}");
            c.UpdateAccountStatus();
            AppServices.EventAggregator?.Publish(new CharacterSkillQueueUpdatedEvent(c.CharacterID, c.Name));
            AppServices.EventAggregator?.Publish(new CommonEvents.CharacterSkillQueueUpdatedEvent(c));
        }

        public void OnCharacterQueuedSkillsCompleted(Character c, IEnumerable<QueuedSkill> skills)
        {
            AppServices.TraceService?.Trace($"CharacterQueuedSkillsCompleted: {c.Name}");
            AppServices.EventAggregator?.Publish(new CommonEvents.QueuedSkillsCompletedEvent(c, skills));
        }

        public bool AnyESIKeyUnprocessed() => AppServices.ESIKeys.Any(k => !k.IsProcessed);

        public GlobalNotificationCollection Notifications => AppServices.Notifications;

        public void Dispose()
        {
            _subSecondTick?.Dispose();
            _subSecondTick = null;
            _subFiveSecondTick?.Dispose();
            _subFiveSecondTick = null;
            _subESIKeyInfoUpdated?.Dispose();
            _subESIKeyInfoUpdated = null;
            _subEveIDToNameUpdated?.Dispose();
            _subEveIDToNameUpdated = null;
        }
    }
}
