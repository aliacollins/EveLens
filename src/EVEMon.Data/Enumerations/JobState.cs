using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of a industry job.
    /// </summary>
    /// <remarks>The integer value determines the sort order in "Group by...".</remarks>
    public enum JobState
    {
        [Header("Active jobs")]
        Active = 0,

        [Header("Delivered jobs")]
        Delivered = 1,

        [Header("Canceled jobs")]
        Canceled = 2,

        [Header("Paused jobs")]
        Paused = 3,

        [Header("Failed jobs")]
        Failed = 4
    }
}