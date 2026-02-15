namespace EVEMon.Common.Enumerations
{
    /// <summary>
    /// Represents the status of the Internet connection.
    /// </summary>
    public enum ConnectionStatus
    {
        /// <summary>
        /// Everything normal, we're online
        /// </summary>
        Online,

        /// <summary>
        /// The user requested to stay offline after connection failures
        /// </summary>
        Offline,

        /// <summary>
        /// The connection has not been tested yet
        /// </summary>
        Unknown
    }
}