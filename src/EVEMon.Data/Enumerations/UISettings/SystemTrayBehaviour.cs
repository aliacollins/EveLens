namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Represents the behaviour for the system tray icon
    /// </summary>
    public enum SystemTrayBehaviour
    {
        /// <summary>
        /// The tray icon is always hidden
        /// </summary>
        Disabled = 0,

        /// <summary>
        /// The tray icon is visible when the main window is minimized
        /// </summary>
        ShowWhenMinimized = 1,

        /// <summary>
        /// The tray icon is always visible
        /// </summary>
        AlwaysVisible = 2
    }
}