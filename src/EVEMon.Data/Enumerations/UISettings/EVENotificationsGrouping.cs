using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the EVE notifications to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum EVENotificationsGrouping
    {
        [Header("Group by type")]
        Type = 0,

        [Header("Group by type (Desc)")]
        TypeDesc = 1,

        [Header("Group by received date")]
        SentDate = 2,

        [Header("Group by received date (Desc)")]
        SentDateDesc = 3,

        [Header("Group by sender")]
        Sender = 4,

        [Header("Group by sender (Desc)")]
        SenderDesc = 5,
    }
}