namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Abstracts the notification system for decoupled alerting.
    /// Replaces direct dependency on <c>EveMonClient.Notifications</c>.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Raises a notification with the specified category and priority.
        /// </summary>
        /// <param name="category">The notification category (mapped to NotificationCategory enum).</param>
        /// <param name="priority">The notification priority (mapped to NotificationPriority enum).</param>
        /// <param name="title">The notification title text.</param>
        /// <param name="context">Optional context object associated with the notification.</param>
        void Notify(int category, int priority, string title, object? context = null);

        /// <summary>
        /// Invalidates (removes) notifications matching the specified category.
        /// </summary>
        /// <param name="category">The notification category to invalidate.</param>
        /// <param name="context">Optional context to match when invalidating.</param>
        void Invalidate(int category, object? context = null);

        /// <summary>
        /// Invalidates all API error notifications.
        /// </summary>
        void InvalidateAPIErrors();
    }
}
