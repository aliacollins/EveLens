using System.ComponentModel;
using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the standing columns.
    /// </summary>
    public enum StandingColumn
    {
        None = -1,

        [Header("Entity Name")]
        [Description("Entity Name")]
        EntityName = 0,

        [Header("Standing")]
        [Description("Standing Value")]
        StandingValue = 1,

        [Header("Effective Standing")]
        [Description("Effective Standing")]
        EffectiveStanding = 2,

        [Header("Group")]
        [Description("Standing Group")]
        Group = 3
    }
}
