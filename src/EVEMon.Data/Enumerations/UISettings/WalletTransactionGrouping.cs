using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the wallet transactions to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum WalletTransactionGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by date")]
        Date = 1,

        [Header("Group by date (Desc)")]
        DateDesc = 2,

        [Header("Group by item type")]
        ItemType = 3,

        [Header("Group by item type (Desc)")]
        ItemTypeDesc = 4,

        [Header("Group by client")]
        Client = 5,

        [Header("Group by client (Desc)")]
        ClientDesc = 6,

        [Header("Group by station")]
        Location = 7,

        [Header("Group by station (Desc)")]
        LocationDesc = 8
    }
}