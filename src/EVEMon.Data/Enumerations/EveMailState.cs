using EVEMon.Common.Attributes;

namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// The status of an EVE mail message.
    /// </summary>
    /// <remarks>The integer value determines the sort order in "Group by...".</remarks>
    public enum EveMailState
    {
        [Header("Inbox")]
        Inbox = 0,

        [Header("Sent Items")]
        SentItem = 1,
    }
}