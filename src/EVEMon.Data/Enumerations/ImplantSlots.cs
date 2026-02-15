using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumerations of the implants slots. None is -1, other range from 0 to 4, matching the order of the implants ingame.
    /// </summary>
    public enum ImplantSlots
    {
        [Description("None")]
        None = -1,

        [Description("Slot  1 (Perception)")]
        Perception = 0,

        [Description("Slot  2 (Memory)")]
        Memory = 1,

        [Description("Slot  3 (Willpower)")]
        Willpower = 2,

        [Description("Slot  4 (Intelligence)")]
        Intelligence = 3,

        [Description("Slot  5 (Charisma)")]
        Charisma = 4,

        [Description("Slot  6")]
        Slot6 = 5,

        [Description("Slot  7")]
        Slot7 = 6,

        [Description("Slot  8")]
        Slot8 = 7,

        [Description("Slot  9")]
        Slot9 = 8,

        [Description("Slot 10")]
        Slot10 = 9
    }
}