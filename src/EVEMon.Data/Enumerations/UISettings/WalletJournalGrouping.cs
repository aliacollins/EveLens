using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the wallet journal to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum WalletJournalGrouping
    {
        [Header("No group")]
        None = 0,

        [Header("Group by date")]
        Date = 1,

        [Header("Group by date (Desc)")]
        DateDesc = 2,

        [Header("Group by type")]
        Type = 3,

        [Header("Group by type (Desc)")]
        TypeDesc = 4,

        [Header("Group by issuer")]
        Issuer = 5,

        [Header("Group by issuer (Desc)")]
        IssuerDesc = 6,

        [Header("Group by recipient")]
        Recipient = 7,

        [Header("Group by recipient (Desc)")]
        RecipientDesc = 8
    }
}