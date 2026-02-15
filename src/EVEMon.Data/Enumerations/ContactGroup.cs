using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of contact group.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContactGroup
    {
        [Description("Personal")]
        Personal = 0,

        [Description("Corporation")]
        Corporate = 1,

        [Description("Alliance")]
        Alliance = 2,

        [Description("Agents")]
        Agent = 3,
    }
}