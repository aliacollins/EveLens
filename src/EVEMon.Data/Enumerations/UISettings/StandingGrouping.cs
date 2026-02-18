using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the standings to be grouped by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum StandingGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by group")]
        Group = 1,

        [Header("Group by status")]
        Status = 2
    }
}
