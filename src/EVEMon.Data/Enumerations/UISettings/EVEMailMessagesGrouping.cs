using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Enumeration for the EVE mail messages to be group by.
    /// </summary>
    /// <remarks>The integer value determines the sort order.</remarks>
    public enum EVEMailMessagesGrouping
    {
        [Header("Group by mail state")]
        State = 0,

        [Header("Group by mail state (Desc)")]
        StateDesc = 1,

        [Header("Group by received date")]
        SentDate = 2,

        [Header("Group by received date (Desc)")]
        SentDateDesc = 3,

        [Header("Group by sender")]
        Sender = 4,

        [Header("Group by sender (Desc)")]
        SenderDesc = 5,

        [Header("Group by subject")]
        Subject = 6,

        [Header("Group by subject (Desc)")]
        SubjectDesc = 7,

        [Header("Group by recipient")]
        Recipient = 8,

        [Header("Group by recipient (Desc)")]
        RecipientDesc = 9,

        [Header("Group by Corp or Alliance")]
        CorpOrAlliance = 10,

        [Header("Group by Corp or Alliance (Desc)")]
        CorpOrAllianceDesc = 11,

        [Header("Group by mailing list")]
        MailingList = 12,

        [Header("Group by mailing list (Desc)")]
        MailingListDesc = 13
    }
}