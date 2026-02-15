using System.ComponentModel;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The contract type.
    /// </summary>
    public enum ContractType
    {
        None,

        [Description("Item Exchange")]
        ItemExchange,

        [Description("Courier")]
        Courier,

        [Description("Loan")]
        Loan,

        [Description("Auction")]
        Auction
    }
}