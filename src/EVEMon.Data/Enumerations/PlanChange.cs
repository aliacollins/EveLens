using System;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Describes the kind of changes which occurred.
    /// </summary>
    [Flags]
    public enum PlanChange
    {
        None = 0,
        Notification = 1,
        Prerequisites = 2,
        All = Notification | Prerequisites
    }
}