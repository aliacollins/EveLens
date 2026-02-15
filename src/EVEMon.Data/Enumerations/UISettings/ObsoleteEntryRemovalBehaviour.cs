namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Describes the behaviour employed to remove Obsolete Entries from plans.
    /// </summary>
    public enum ObsoleteEntryRemovalBehaviour
    {
        /// <summary>
        /// Never remove entries from the plan, always ask the user.
        /// </summary>
        AlwaysAsk = 0,

        /// <summary>
        /// Only remove confirmed completed (by API) entries from the plan, ask about unconfirmed entries.
        /// </summary>
        RemoveConfirmed = 1,

        /// <summary>
        /// Always remove all entries automatically.
        /// </summary>
        RemoveAll = 2
    }
}