using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

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

        public void OnCharacterUpdated(Character c) => EveMonClient.OnCharacterUpdated(c);
        public void OnMarketOrdersUpdated(Character c) => EveMonClient.OnMarketOrdersUpdated(c);
        public void OnContractsUpdated(Character c) => EveMonClient.OnContractsUpdated(c);
        public void OnIndustryJobsUpdated(Character c) => EveMonClient.OnIndustryJobsUpdated(c);
        public void OnCharacterInfoUpdated(Character c) => EveMonClient.OnCharacterInfoUpdated(c);
        public void OnCharacterSkillQueueUpdated(Character c) => EveMonClient.OnCharacterSkillQueueUpdated(c);
        public void OnCharacterQueuedSkillsCompleted(Character c, IEnumerable<QueuedSkill> skills)
            => EveMonClient.OnCharacterQueuedSkillsCompleted(c, skills);

        public bool AnyESIKeyUnprocessed() => EveMonClient.ESIKeys.Any(k => !k.IsProcessed);

        public GlobalNotificationCollection Notifications => EveMonClient.Notifications;
    }
}
