namespace EVEMon.Common.Enumerations.CCPAPI
{
    /// <summary>
    /// The status of a market order.
    /// </summary>
    public enum CCPOrderState
    {
        Opened = 0,
        Closed = 1,
        ExpiredOrFulfilled = 2,
        Canceled = 3,
        Pending = 4,
        CharacterDeleted = 5
    }
}