using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of Kill log fitting and content group.
    /// </summary>
    public enum KillLogFittingContentGroup
    {
        [Description("Unknown")]
        None = 0,

        [Description("High Power Slots")]
        HighSlot = 1,

        [Description("Medium Power Slots")]
        MediumSlot = 2,

        [Description("Low Power Slots")]
        LowSlot = 3,

        [Description("Subsystem Slots")]
        SubsystemSlot = 4,

        [Description("Rig Slots")]
        RigSlot = 5,

        [Description("Drone Bay")]
        DroneBay = 6,

        [Description("Cargo Bay")]
        Cargo = 7,

        [Description("Implants")]
        Implant = 8,

        [Description("Boosters")]
        Booster = 9,

        [Description("Other")]
        Other = 10
    }
}