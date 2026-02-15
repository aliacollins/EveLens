namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the type of a plan operation.
    /// </summary>
    public enum PlanOperations
    {
        /// <summary>
        /// None, there is nothing to do.
        /// </summary>
        None,

        /// <summary>
        /// The operation is an addition.
        /// </summary>
        Addition,

        /// <summary>
        /// The operation is a suppression.
        /// </summary>
        Suppression
    }
}