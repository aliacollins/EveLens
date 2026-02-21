// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EVEMon.Common.Notifications
{
    /// <summary>
    /// Represents the priority of a <see cref="NotificationEventArgs"/>.
    /// </summary>
    public enum NotificationPriority
    {
        /// <summary>
        /// This notification is a mere information.
        /// </summary>
        Information = 0,

        /// <summary>
        /// The notification is a warnining.
        /// </summary>
        Warning = 1,

        /// <summary>
        /// The notification is an error.
        /// </summary>
        Error = 2
    }
}