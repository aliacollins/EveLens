namespace EVEMon.Common.Services
{
    /// <summary>
    /// Switchover flags for enabling new service implementations.
    /// Defaults OFF (old code runs). Flip to true in ServiceRegistration.Configure() to enable.
    /// This is a developer toggle, not a user setting — no persistence needed.
    /// </summary>
    public static class FeatureFlags
    {
        /// <summary>
        /// When true, Settings.Save() delegates to SmartSettingsManager for save coalescing
        /// instead of using the built-in s_savePending flag with ThirtySecondTick.
        /// </summary>
        public static bool UseSmartSettings { get; set; } = false;

        /// <summary>
        /// When true, SmartQueryScheduler replaces CentralQueryScheduler as the tick driver
        /// for all character/corporation querying and ESI key token refresh.
        /// </summary>
        public static bool UseSmartScheduler { get; set; } = false;

        /// <summary>
        /// When true, CharacterQueryOrchestrator replaces CharacterDataQuerying as the
        /// character data querying implementation, and CorporationQueryOrchestrator replaces
        /// CorporationDataQuerying for corporation data querying.
        /// </summary>
        public static bool UseCharacterOrchestrator { get; set; } = false;
    }
}
