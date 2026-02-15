namespace EVEMon.SkillPlanner
{
    /// <summary>
    /// Remapping strategy.
    /// </summary>
    public enum AttributeOptimizationStrategy
    {
        /// <summary>
        /// Stratagy based on remapping points.
        /// </summary>
        RemappingPoints,

        /// <summary>
        /// Strategy based on the first year from a plan.
        /// </summary>
        OneYearPlan,

        /// <summary>
        /// Strategy based on already trained skills.
        /// </summary>
        Character,

        /// <summary>
        /// Used when the user double-click a remapping point to manually edit it.
        /// </summary>
        ManualRemappingPointEdition
    }
}