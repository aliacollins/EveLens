using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of skill browser sorter.
    /// </summary>
    public enum SkillSort
    {
        [Description("No Sorting")]
        None = 0,

        [Description("Time to Next Level")]
        TimeToNextLevel = 1,

        [Description("Time to Max Level")]
        TimeToLevel5 = 2,

        [Description("Skill Rank")]
        Rank = 3,

        [Description("Skill Points per Hour")]
        SPPerHour = 4
    }
}