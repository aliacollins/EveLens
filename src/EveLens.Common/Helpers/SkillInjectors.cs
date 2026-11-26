// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Skill extraction and injection amounts.
    /// </summary>
    public static class SkillInjectors
    {
        /// <summary>Skill points removed by one large skill extractor.</summary>
        public const long SkillPointsPerLargeExtraction = 500_000;

        /// <summary>Skill points held by one small skill injector.</summary>
        public const long SkillPointsPerSmallExtraction = 100_000;

        /// <summary>Total skill points a character must keep to be able to extract.</summary>
        public const long MinimumSkillPointsToExtract = 5_000_000;

        /// <summary>
        /// Skill points a character gains from one large skill injector, which shrinks
        /// as the character's total skill points grow.
        /// </summary>
        /// <param name="characterSkillPoints">The character's total skill points.</param>
        public static int SkillPointsPerLargeInjector(long characterSkillPoints) => characterSkillPoints switch
        {
            < 5_000_000 => 500_000,
            < 50_000_000 => 400_000,
            < 80_000_000 => 300_000,
            _ => 150_000
        };
    }
}
