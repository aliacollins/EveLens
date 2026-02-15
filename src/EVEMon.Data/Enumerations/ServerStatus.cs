namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents a server status.
    /// </summary>
    public enum ServerStatus
    {
        /// <summary>
        /// The server is offline
        /// </summary>
        Offline,

        /// <summary>
        /// The server is online
        /// </summary>
        Online,

        /// <summary>
        /// The API couldn't be queried or has not been queried yet.
        /// </summary>
        Unknown,

        /// <summary>
        /// The server's status checks have been disabled.
        /// </summary>
        CheckDisabled
    }
}