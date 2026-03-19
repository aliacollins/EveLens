// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Models;
using EveLens.Common.Notifications;
using EveLens.Core.Events;
using EveLens.Core.Interfaces;

namespace EveLens.Common.Services
{
    /// <summary>
    /// Bridges <see cref="HealthStateChangedEvent"/> transitions to the activity log.
    /// Only user-actionable state changes produce notifications:
    /// <list type="bullet">
    ///   <item><b>Failing:</b> "ESI data for {Name} is not updating"</item>
    ///   <item><b>Suspended:</b> "{Name}: re-authentication required"</item>
    ///   <item><b>Healthy (from Failing/Suspended):</b> auto-clears the notification</item>
    ///   <item><b>Degraded:</b> no notification (transient, auto-resolves)</item>
    /// </list>
    /// </summary>
    internal sealed class HealthNotificationSubscriber : IDisposable
    {
        private readonly IDisposable? _subscription;

        public HealthNotificationSubscriber(IEventAggregator? aggregator)
        {
            _subscription = aggregator?.Subscribe<HealthStateChangedEvent>(OnHealthChanged);
        }

        private void OnHealthChanged(HealthStateChangedEvent e)
        {
            try
            {
                var character = FindCharacter(e.CharacterId);
                if (character == null)
                    return;

                if (e.NewState == HealthStateChangedEvent.StateFailing)
                {
                    var notification = new NotificationEventArgs(character,
                        NotificationCategory.QueryingError)
                    {
                        Description = $"ESI data for {character.Name} is not updating",
                        Behaviour = NotificationBehaviour.Overwrite,
                        Priority = NotificationPriority.Warning
                    };
                    AppServices.Notifications?.Notify(notification);
                }
                else if (e.NewState == HealthStateChangedEvent.StateSuspended)
                {
                    var notification = new NotificationEventArgs(character,
                        NotificationCategory.QueryingError)
                    {
                        Description = $"{character.Name}: re-authentication required",
                        Behaviour = NotificationBehaviour.Overwrite,
                        Priority = NotificationPriority.Error
                    };
                    AppServices.Notifications?.Notify(notification);
                }
                else if (e.NewState == HealthStateChangedEvent.StateHealthy &&
                         (e.OldState == HealthStateChangedEvent.StateFailing ||
                          e.OldState == HealthStateChangedEvent.StateSuspended))
                {
                    // Auto-clear the previous error notification
                    AppServices.Notifications?.InvalidateCharacterAPIError(character);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"HealthNotificationSubscriber error: {ex}");
            }
        }

        private static CCPCharacter? FindCharacter(long characterId)
        {
            if (AppServices.Characters == null)
                return null;

            foreach (var character in AppServices.Characters)
            {
                if (character is CCPCharacter ccp && ccp.CharacterID == characterId)
                    return ccp;
            }
            return null;
        }

        public void Dispose()
        {
            _subscription?.Dispose();
        }
    }
}
