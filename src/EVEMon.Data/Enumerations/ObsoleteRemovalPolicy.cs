namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The policy to apply when removing obsolete entries from a plan.
    /// </summary>
    public enum ObsoleteRemovalPolicy
    {
        None = 0,
        RemoveAll = 1,
        ConfirmedOnly = 2
    }
}