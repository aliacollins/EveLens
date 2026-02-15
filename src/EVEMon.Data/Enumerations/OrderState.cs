using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of a market order.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum OrderState
    {
        [Header("Active orders")]
        Active = 0,

        [Header("Canceled orders")]
        Canceled = 1,

        [Header("Expired orders")]
        Expired = 2,

        [Header("Fulfilled orders")]
        Fulfilled = 3,

        [Header("Modified orders")]
        Modified = 4
    }
}