using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of a contract.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum ContractState
    {
        [Header("Assigned contracts")]
        Assigned = 0,

        [Header("Created contracts")]
        Created = 1,

        [Header("Canceled contracts")]
        Canceled = 2,

        [Header("Deleted contracts")]
        Deleted = 3,

        [Header("Expired contracts")]
        Expired = 4,

        [Header("Rejected contracts")]
        Rejected = 5,

        [Header("Finished contracts")]
        Finished = 6,

        [Header("Failed contracts")]
        Failed = 7,

        [Header("Unknown contracts")]
        Unknown = 8
    }
}