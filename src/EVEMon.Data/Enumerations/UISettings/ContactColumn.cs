using System.ComponentModel;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the contact columns.
    /// </summary>
    public enum ContactColumn
    {
        None = -1,

        [Header("Name")]
        [Description("Contact Name")]
        Name = 0,

        [Header("Standing")]
        [Description("Standing")]
        Standing = 1,

        [Header("Group")]
        [Description("Contact Group")]
        Group = 2,

        [Header("In Watchlist")]
        [Description("In Watchlist")]
        InWatchlist = 3
    }
}
