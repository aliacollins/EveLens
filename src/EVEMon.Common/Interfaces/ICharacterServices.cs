// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Models;

namespace EVEMon.Common.Interfaces
{
    /// <summary>
    /// Abstracts the EveMonClient services that CCPCharacter and its sub-collections
    /// depend on: timer events, event firing, notifications, and ESI key state.
    /// Production implementation wraps EveMonClient statics.
    /// Test implementation is a no-op for creating characters without EveMonClient.
    /// </summary>
    public interface ICharacterServices
    {
        // Timer subscriptions
        event EventHandler SecondTick;
        event EventHandler FiveSecondTick;

        // Global event subscriptions
        event EventHandler ESIKeyInfoUpdated;
        event EventHandler EveIDToNameUpdated;

        // Event firing (CCPCharacter and sub-collections fire these)
        void OnCharacterUpdated(Character character);
        void OnMarketOrdersUpdated(Character character);
        void OnContractsUpdated(Character character);
        void OnIndustryJobsUpdated(Character character);
        void OnCharacterInfoUpdated(Character character);
        void OnCharacterSkillQueueUpdated(Character character);
        void OnCharacterQueuedSkillsCompleted(Character character, IEnumerable<QueuedSkill> skillsCompleted);

        // ESI key state
        bool AnyESIKeyUnprocessed();

        // Notification facade
        GlobalNotificationCollection Notifications { get; }
    }
}
