// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE notification type codes to numeric IDs, display names, and layout templates.
    /// Notification types define how in-game notifications (war declarations, sovereignty changes,
    /// structure alerts, etc.) are displayed in EveLens.
    /// Breaks the Model to <c>EveNotificationType</c> static service dependency
    /// (5 call sites, 2 files).
    /// </summary>
    /// <remarks>
    /// Notification type data is loaded from static datafiles at startup. If an unknown type code
    /// is encountered (CCP adds new notification types periodically), <see cref="GetID"/> creates
    /// a placeholder entry so the application does not crash on unknown types.
    ///
    /// Layout templates use a simple token replacement syntax for rendering notification
    /// subjects and body text from structured notification data.
    ///
    /// Production: <c>NotificationTypeResolverAdapter</c> in
    /// <c>EveLens.Common/Services/NotificationTypeResolverAdapter.cs</c>
    /// (delegates to static <c>EveNotificationType</c>).
    /// Testing: Provide a stub with a small set of known type codes and templates.
    /// </remarks>
    public interface INotificationTypeResolver
    {
        /// <summary>
        /// Gets the numeric notification type ID from its ESI type code string.
        /// Creates a placeholder template entry if the code is unknown (new/unrecognized type).
        /// </summary>
        /// <param name="typeCode">The ESI notification type code string (e.g., "WarDeclaredMsg").</param>
        /// <returns>The internal notification type ID.</returns>
        int GetID(string typeCode);

        /// <summary>
        /// Gets the human-readable display name for a notification type ID.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The display name (e.g., "War Declared").</returns>
        string GetName(int typeId);

        /// <summary>
        /// Gets the subject layout template for rendering a notification's subject line.
        /// Uses token replacement syntax with notification data fields.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The subject layout template string.</returns>
        string GetSubjectLayout(int typeId);

        /// <summary>
        /// Gets the body text layout template for rendering a notification's full text.
        /// Uses token replacement syntax with notification data fields.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The text layout template string.</returns>
        string GetTextLayout(int typeId);
    }
}
