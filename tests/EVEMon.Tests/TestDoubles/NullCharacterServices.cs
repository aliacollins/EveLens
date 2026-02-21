// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Collections.Global;
using EVEMon.Common.Interfaces;
using EVEMon.Common.Models;

namespace EVEMon.Tests.TestDoubles
{
    /// <summary>
    /// No-op implementation of <see cref="ICharacterServices"/> for tests.
    /// Events are subscribed to but never fire. Event-firing methods are no-ops.
    /// Allows 100+ CCPCharacter construction without EveMonClient initialization.
    /// </summary>
    internal sealed class NullCharacterServices : ICharacterServices
    {
        // Events that are subscribed to but never fire
        public event EventHandler SecondTick { add { } remove { } }
        public event EventHandler FiveSecondTick { add { } remove { } }
        public event EventHandler ESIKeyInfoUpdated { add { } remove { } }
        public event EventHandler EveIDToNameUpdated { add { } remove { } }

        // Capture counts for test assertions
        public int CharacterUpdatedCount { get; private set; }

        public void OnCharacterUpdated(Character c) => CharacterUpdatedCount++;
        public void OnMarketOrdersUpdated(Character c) { }
        public void OnContractsUpdated(Character c) { }
        public void OnIndustryJobsUpdated(Character c) { }
        public void OnCharacterInfoUpdated(Character c) { }
        public void OnCharacterSkillQueueUpdated(Character c) { }
        public void OnCharacterQueuedSkillsCompleted(Character c, IEnumerable<QueuedSkill> skills) { }

        public bool AnyESIKeyUnprocessed() => false;

        public GlobalNotificationCollection Notifications => null!;
    }
}
