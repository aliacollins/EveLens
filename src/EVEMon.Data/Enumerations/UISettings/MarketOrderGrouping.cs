using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the market orders to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum MarketOrderGrouping
    {
        [Header("Group by order status")]
        State = 0,

        [Header("Group by order status (Desc)")]
        StateDesc = 1,

        [Header("Group by buying/selling")]
        OrderType = 2,

        [Header("Group by buying/selling (Desc)")]
        OrderTypeDesc = 3,

        [Header("Group by issue day")]
        Issued = 4,

        [Header("Group by issue day (Desc)")]
        IssuedDesc = 5,

        [Header("Group by item type")]
        ItemType = 6,

        [Header("Group by item type (Desc)")]
        ItemTypeDesc = 7,

        [Header("Group by station")]
        Location = 8,

        [Header("Group by station (Desc)")]
        LocationDesc = 9
    }
}