// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Abstracts the application notification system for decoupled alerting.
    /// Replaces direct dependency on <c>EveLensClient.Notifications</c>
    /// (<c>GlobalNotificationCollection</c>).
    /// </summary>
    /// <remarks>
    /// Notifications are displayed in the UI notification panel and optionally as system
    /// tray balloon tips. Each notification has a category (what happened) and a priority
    /// (how urgently the user should be informed).
    ///
    /// Category and priority are passed as <c>int</c> rather than enum types because the
    /// Core assembly cannot reference the <c>NotificationCategory</c> and
    /// <c>NotificationPriority</c> enums defined in <c>EveLens.Common</c>. Callers cast
    /// from the appropriate enum.
    ///
    /// <see cref="Invalidate"/> removes notifications matching a category (and optional context),
    /// used when the condition that triggered the notification is resolved. For example,
    /// an API error notification is invalidated when the next API call succeeds.
    ///
    /// Production: Implement by delegating to <c>GlobalNotificationCollection</c>.
    /// Testing: Provide a stub that tracks <c>Notify</c>/<c>Invalidate</c> calls for assertion.
    /// </remarks>
    public interface INotificationService
    {
        /// <summary>
        /// Raises a notification visible in the notification panel and optionally as a tray balloon.
        /// </summary>
        /// <param name="category">The notification category (cast from <c>NotificationCategory</c> enum).</param>
        /// <param name="priority">The notification priority (cast from <c>NotificationPriority</c> enum).</param>
        /// <param name="title">The notification title text displayed to the user.</param>
        /// <param name="context">Optional context object associated with the notification (e.g., character).</param>
        void Notify(int category, int priority, string title, object? context = null);

        /// <summary>
        /// Removes all notifications matching the specified category and optional context.
        /// Used when the underlying condition is resolved (e.g., API error clears on success).
        /// </summary>
        /// <param name="category">The notification category to remove (cast from <c>NotificationCategory</c>).</param>
        /// <param name="context">Optional context to match; if null, removes all notifications of the category.</param>
        void Invalidate(int category, object? context = null);

        /// <summary>
        /// Removes all API-related error notifications at once.
        /// Called when a bulk operation succeeds and all outstanding API errors can be dismissed.
        /// </summary>
        void InvalidateAPIErrors();
    }
}
