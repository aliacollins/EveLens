// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Constants
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
        /// Maximum effective attribute value a character can have (MaxBaseAttributePoints + MaxImplantPoints).
        /// No attribute should ever exceed this in training time calculations.
        /// </summary>
        public const int MaxEffectiveAttributePoints = 32;

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
