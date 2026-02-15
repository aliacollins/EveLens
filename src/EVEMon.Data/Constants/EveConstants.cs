namespace EVEMon.Common.Constants
{
    public static class EveConstants
    {
        public const int SpareAttributePointsOnRemap = 14;
        public const int CharacterBaseAttributePoints = 17;
        public const int MaxRemappablePointsPerAttribute = 10;
        public const int MaxImplantPoints = 5;

        /// <summary>
        /// Maximum base attribute points (CharacterBaseAttributePoints + MaxRemappablePointsPerAttribute).
        /// </summary>
        public const int MaxBaseAttributePoints = 27;

        /// <summary>
        /// Maximum total attribute value without a booster (MaxBaseAttributePoints + MaxImplantPoints).
        /// Any attribute value above this indicates a cerebral accelerator is active.
        /// </summary>
        public const int MaxAttributeWithoutBooster = 32;

        /// <summary>
        /// Maximum bonus from cerebral accelerators (Expert Cerebral Accelerator).
        /// </summary>
        public const int MaxBoosterBonus = 12;

        /// <summary>
        /// Base duration of cerebral accelerators in hours before Biology skill bonus.
        /// Standard accelerators last 24 hours base.
        /// </summary>
        public const int BaseBoosterDurationHours = 24;

        public const int DowntimeHour = 11;
        public const int DowntimeDuration = 30;
        public const float TransactionTaxBase = 0.05f;
        public const float BrokerFeeBase = 0.05f;
        public const int MaxSkillsInQueue = 50;
        public const int MaxAlphaSkillTraining = 5000000;

        /// <summary>
        /// Represents a "region" range.
        /// </summary>
        public const int RegionRange = 32767;

    }
}
