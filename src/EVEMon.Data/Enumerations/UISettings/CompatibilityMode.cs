namespace EVEMon.Common.Enumerations.UISettings
{
    /// <summary>
    /// Describes the target platform to allow EVEMon to apply different tweaks at runtime
    /// </summary>
    public enum CompatibilityMode
    {
        /// <summary>
        /// Windows and Linux + Wine
        /// </summary>
        Default = 0,

        /// <summary>
        /// Wine
        /// </summary>
        Wine = 1
    }
}