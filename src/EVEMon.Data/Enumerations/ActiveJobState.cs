using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of an active job.
    /// </summary>
    public enum ActiveJobState
    {
        None,

        [Description("Pending")]
        Pending,

        [Description("In progress")]
        InProgress,

        [Description("Ready")]
        Ready
    }
}