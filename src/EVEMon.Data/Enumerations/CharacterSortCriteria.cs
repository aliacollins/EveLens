namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Enumeration of character sort criteria.
    /// </summary>
    public enum CharacterSortCriteria
    {
        /// <summary>
        /// Characters are sorted by their names
        /// </summary>
        Name = 0,

        /// <summary>
        /// Characters are sorted by their training completion time or, when not in training, their names.
        /// </summary>
        TrainingCompletion = 1,
    };
}