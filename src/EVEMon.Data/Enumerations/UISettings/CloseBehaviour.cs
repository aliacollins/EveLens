namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Represents the behaviour when closing the main form
    /// </summary>
    public enum CloseBehaviour
    {
        /// <summary>
        /// Exit the application
        /// </summary>
        Exit = 0,

        /// <summary>
        /// Minimize to the system tray
        /// </summary>
        MinimizeToTray = 1,

        /// <summary>
        /// Minimize to the task bar
        /// </summary>
        MinimizeToTaskbar = 2
    }
}