namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Describes whether this entry is a prerequisite of another entry.
    /// </summary>
    public enum PlanEntryType
    {
        /// <summary>
        /// This entry is a top-level one, no entries depend on it.
        /// </summary>
        Planned,

        /// <summary>
        /// This entry is required by another entry
        /// </summary>
        Prerequisite
    }
}