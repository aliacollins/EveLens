// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using EVEMon.Common.Events;
using EVEMon.Common.Models;
using EVEMon.Common.Services;
using EVEMon.Core.Events;

namespace EVEMon.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the notification center / activity log.
    /// Subscribes to NotificationSentEvent and persists entries to disk.
    /// </summary>
    public sealed class NotificationCenterViewModel : ViewModelBase
    {
        private const int MaxEntries = 200;
        private readonly List<ActivityEntry> _entries;
        private readonly object _lock = new();
        private bool _isDirty;

        public NotificationCenterViewModel()
        {
            _entries = AppServices.ActivityLog.Load();

            Subscribe<NotificationSentEvent>(OnNotificationSent);
            Subscribe<NotificationInvalidatedEvent>(OnNotificationInvalidated);
            Subscribe<ThirtySecondTickEvent>(OnThirtySecondTick);
        }

        public IReadOnlyList<ActivityEntry> Entries
        {
            get { lock (_lock) { return new List<ActivityEntry>(_entries); } }
        }

        public int UnreadCount
        {
            get
            {
                lock (_lock)
                {
                    int count = 0;
                    foreach (var e in _entries)
                        if (!e.IsRead) count++;
                    return count;
                }
            }
        }

        public bool HasUnread => UnreadCount > 0;

        public void MarkAllRead()
        {
            lock (_lock)
            {
                foreach (var e in _entries)
                    e.IsRead = true;
                _isDirty = true;
            }

            RaiseAllChanged();
        }

        public void ClearAll()
        {
            lock (_lock)
            {
                _entries.Clear();
                _isDirty = true;
            }

            RaiseAllChanged();
        }

        public void Save()
        {
            List<ActivityEntry> snapshot;
            lock (_lock)
            {
                snapshot = new List<ActivityEntry>(_entries);
                _isDirty = false;
            }
            AppServices.ActivityLog.Save(snapshot);
        }

        private void OnNotificationSent(NotificationSentEvent evt)
        {
            var n = evt.Args;
            var entry = new ActivityEntry
            {
                Timestamp = DateTime.UtcNow,
                CharacterName = n.SenderCharacter?.Name ?? "",
                Category = n.Category.ToString(),
                Description = n.Description,
                IsRead = false
            };

            lock (_lock)
            {
                _entries.Insert(0, entry);
                if (_entries.Count > MaxEntries)
                    _entries.RemoveAt(_entries.Count - 1);
                _isDirty = true;
            }

            Dispatcher?.Post(RaiseAllChanged);
        }

        private void OnNotificationInvalidated(NotificationInvalidatedEvent evt)
        {
            // Invalidation removes stale notifications — just refresh the UI
            Dispatcher?.Post(RaiseAllChanged);
        }

        private void OnThirtySecondTick(ThirtySecondTickEvent _)
        {
            if (_isDirty)
                Save();
        }

        private void RaiseAllChanged()
        {
            OnPropertyChanged(nameof(Entries));
            OnPropertyChanged(nameof(UnreadCount));
            OnPropertyChanged(nameof(HasUnread));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && _isDirty)
                Save();

            base.Dispose(disposing);
        }
    }
}
