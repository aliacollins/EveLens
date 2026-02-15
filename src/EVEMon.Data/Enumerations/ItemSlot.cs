using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Flags for the items slots.
    /// </summary>
    [Flags]
    public enum ItemSlot
    {
        None = 0,
        NoSlot = 1,
        Low = 2,
        Medium = 4,
        High = 8,

        All = Low | Medium | High | NoSlot
    }
}