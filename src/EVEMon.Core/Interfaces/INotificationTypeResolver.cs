namespace EVEMon.Core.Interfaces
{
    /// <summary>
    /// Resolves EVE notification type codes to IDs, names, and layout templates.
    /// Breaks Model -> EveNotificationType Service dependency (5 call sites, 2 files).
    /// </summary>
    public interface INotificationTypeResolver
    {
        /// <summary>
        /// Gets the notification type ID from its code string.
        /// Creates a template entry if the code is unknown.
        /// </summary>
        /// <param name="typeCode">The notification type code string.</param>
        /// <returns>The notification type ID.</returns>
        int GetID(string typeCode);

        /// <summary>
        /// Gets the display name for a notification type ID.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The display name.</returns>
        string GetName(int typeId);

        /// <summary>
        /// Gets the subject layout template for a notification type.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The subject layout template string.</returns>
        string GetSubjectLayout(int typeId);

        /// <summary>
        /// Gets the text layout template for a notification type.
        /// </summary>
        /// <param name="typeId">The notification type ID.</param>
        /// <returns>The text layout template string.</returns>
        string GetTextLayout(int typeId);
    }
}
