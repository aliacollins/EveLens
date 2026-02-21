// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Linq;
using EVEMon.Common.Models;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;

namespace EVEMon.Common.Services
{
    /// <summary>
    /// Subscribes to <see cref="ActiveCharacterChangedEvent"/> and toggles
    /// Tier 1 (Detail) monitors on the corresponding <see cref="CharacterQueryOrchestrator"/>.
    /// This bridges the UI tab selection (Phase 1) to the query tier system (Phase 2).
    /// </summary>
    internal sealed class ActiveCharacterTierSubscriber : IDisposable
    {
        private readonly IDisposable? _subscription;
        private long _previousActiveId;

        internal ActiveCharacterTierSubscriber()
        {
            _subscription = AppServices.EventAggregator?
                .Subscribe<ActiveCharacterChangedEvent>(OnActiveChanged);
        }

        private void OnActiveChanged(ActiveCharacterChangedEvent e)
        {
            // Toggle Tier 1 monitors: deactivate previous, activate new
            if (_previousActiveId != 0)
                SetActive(_previousActiveId, false);
            if (e.CharacterId != 0)
                SetActive(e.CharacterId, true);
            _previousActiveId = e.CharacterId;

            // Wire priority scheduling via EsiScheduler
            AppServices.EsiScheduler?.SetVisibleCharacter(e.CharacterId);
        }

        private static void SetActive(long characterId, bool active)
        {
            var ccp = AppServices.MonitoredCharacters
                .OfType<CCPCharacter>()
                .FirstOrDefault(c => c.CharacterID == characterId);
            ccp?.QueryOrchestrator?.SetActiveCharacter(active);
        }

        public void Dispose() => _subscription?.Dispose();
    }
}
