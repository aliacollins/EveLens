using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the contacts to be grouped by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContactGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by contact group")]
        ContactGroup = 1,

        [Header("Group by standing bracket")]
        StandingBracket = 2
    }
}
