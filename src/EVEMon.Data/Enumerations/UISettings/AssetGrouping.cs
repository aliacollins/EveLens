using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the assets to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum AssetGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by group")]
        Group = 1,

        [Header("Group by group (Desc)")]
        GroupDesc = 2,

        [Header("Group by category")]
        Category = 3,

        [Header("Group by category (Desc)")]
        CategoryDesc = 4,

        [Header("Group by container")]
        Container = 5,

        [Header("Group by container (Desc)")]
        ContainerDesc = 6,

        [Header("Group by location")]
        Location = 7,

        [Header("Group by location (Desc)")]
        LocationDesc = 8,

        [Header("Group by region")]
        Region = 9,

        [Header("Group by region (Desc)")]
        RegionDesc = 10,

        [Header("Group by jumps")]
        Jumps = 11,

        [Header("Group by jumps (Desc)")]
        JumpsDesc = 12
    }
}