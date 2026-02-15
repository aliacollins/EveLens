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
    /// </summary>
    internal sealed class EveMonClientCharacterServices : ICharacterServices
    {
        internal static readonly EveMonClientCharacterServices Instance = new();

        public event EventHandler SecondTick
        {
            add => EveMonClient.SecondTick += value;
            remove => EveMonClient.SecondTick -= value;
        }

        public event EventHandler FiveSecondTick
        {
            add => EveMonClient.FiveSecondTick += value;
            remove => EveMonClient.FiveSecondTick -= value;
        }

        public event EventHandler ESIKeyInfoUpdated
        {
            add => EveMonClient.ESIKeyInfoUpdated += value;
            remove => EveMonClient.ESIKeyInfoUpdated -= value;
        }

        public event EventHandler EveIDToNameUpdated
        {
            add => EveMonClient.EveIDToNameUpdated += value;
            remove => EveMonClient.EveIDToNameUpdated -= value;
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
    }
}
