using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of medal group.
    /// </summary>
    public enum MedalGroup
    {
        [Description("Corporation")]
        Corporation,

        [Description("Current Corporation")]
        CurrentCorporation,

        [Description("Other Corporation")]
        OtherCorporation,
    }
}