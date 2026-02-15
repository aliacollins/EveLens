using System.ComponentModel;

namespace EVEMon.Common.Enumerations.CCPAPI
{
    public enum CCPContractStatus
    {
        [Description("None")]
        None,

        [Description("Outstanding")]
        Outstanding,

        [Description("In Progress")]
        InProgress,

        [Description("Deleted")]
        Deleted,

        [Description("Finished")]
        Completed,

        [Description("Failed")]
        Failed,

        [Description("Completed By Issuer")]
        CompletedByIssuer,

        [Description("Completed By Contractor")]
        CompletedByContractor,

        [Description("Canceled")]
        Canceled,

        [Description("Rejected")]
        Rejected,

        [Description("Overdue")]
        Overdue,

        [Description("Reversed")]
        Reversed
    }
}
