using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the metagroup of an item.
    /// </summary>
    [Flags]
    public enum ItemMetaGroup
    {
        T1 = 2,
        T2 = 4,
        T3 = 8,
        Faction = 16,
        Officer = 32,
        Deadspace = 64,
        Storyline = 128,

        None = 0,
        AllTechLevel = T1 | T2 | T3,
        AllNonTechLevel = Faction | Officer | Deadspace | Storyline,
        All = AllTechLevel | AllNonTechLevel
    }
}