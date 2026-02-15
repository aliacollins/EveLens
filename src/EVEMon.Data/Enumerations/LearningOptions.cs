using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the options one can use with CharacterScratchpad.SetSkillLevel. Those are only optimizations.
    /// </summary>
    [Flags]
    public enum LearningOptions
    {
        /// <summary>
        /// None, regular learning.
        /// </summary>
        None = 0,

        /// <summary>
        /// Do not update the total SP count.
        /// </summary>
        FreezeSP = 1,

        /// <summary>
        /// Do not update the training time and the trained skills enumeration.
        /// </summary>
        IgnoreTraining = 2,

        /// <summary>
        /// Assume the prerequisites are already known.
        /// </summary>
        IgnorePrereqs = 4,

        /// <summary>
        /// Ignore the changes when the given target level is lower than the current one
        /// </summary>
        UpgradeOnly = 8
    }
}