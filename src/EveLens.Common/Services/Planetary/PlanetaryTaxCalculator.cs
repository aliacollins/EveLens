// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Calculates PI export/import taxes based on system security, character skills,
    /// and player-owned customs office (POCO) rates.
    /// </summary>
    public static class PlanetaryTaxCalculator
    {
        // NPC customs office base tax rates by security class
        private const double NpcTaxRateHighSec = 0.10;  // 10%
        private const double NpcTaxRateLowSec = 0.05;   // 5%
        private const double NpcTaxRateNullSec = 0.0;   // 0% (null/WH)

        // Customs Code Expertise reduces NPC tax by 10% per level (max 50% at level 5)
        private const double SkillReductionPerLevel = 0.10;

        // Default POCO tax rate when player hasn't configured one
        public const double DefaultPocoTaxRate = 0.10;  // 10%

        /// <summary>
        /// Security class for PI tax calculation purposes.
        /// Uses raw security value with 0.45 threshold (not the rounded display value).
        /// </summary>
        public enum SecurityClass
        {
            HighSec,
            LowSec,
            NullSec
        }

        /// <summary>
        /// Determines the security class from a raw security status value.
        /// PI mechanics use the raw value: >= 0.45 is highsec, > 0.0 is lowsec, <= 0.0 is nullsec.
        /// This differs from the display rounding (which shows 0.5+ as highsec).
        /// </summary>
        public static SecurityClass GetSecurityClass(float securityStatus)
        {
            if (securityStatus >= 0.45f)
                return SecurityClass.HighSec;
            if (securityStatus > 0.0f)
                return SecurityClass.LowSec;
            return SecurityClass.NullSec;
        }

        /// <summary>
        /// Gets the NPC base tax rate for a security class.
        /// Only applies to NPC customs offices — player-owned have their own rates.
        /// </summary>
        public static double GetNpcBaseTaxRate(SecurityClass secClass)
        {
            return secClass switch
            {
                SecurityClass.HighSec => NpcTaxRateHighSec,
                SecurityClass.LowSec => NpcTaxRateLowSec,
                SecurityClass.NullSec => NpcTaxRateNullSec,
                _ => NpcTaxRateHighSec
            };
        }

        /// <summary>
        /// Calculates the effective NPC tax rate after applying Customs Code Expertise skill.
        /// The skill reduces the NPC portion by 10% per level (multiplicative).
        /// </summary>
        /// <param name="securityStatus">Raw system security status (-1.0 to 1.0)</param>
        /// <param name="customsCodeExpertiseLevel">Skill level 0-5</param>
        /// <returns>Effective NPC tax rate (0.0 to 0.10)</returns>
        public static double GetEffectiveNpcTaxRate(float securityStatus, int customsCodeExpertiseLevel)
        {
            var secClass = GetSecurityClass(securityStatus);
            double baseRate = GetNpcBaseTaxRate(secClass);
            int skillLevel = Math.Clamp(customsCodeExpertiseLevel, 0, 5);
            double reduction = 1.0 - (skillLevel * SkillReductionPerLevel);
            return baseRate * reduction;
        }

        /// <summary>
        /// Calculates the total effective tax rate (NPC + POCO combined).
        /// In highsec, all COs are NPC-owned so POCO rate is zero.
        /// In low/null, player POCOs replace NPC COs — the NPC rate still applies as base,
        /// and the POCO owner sets an additional rate on top.
        /// </summary>
        /// <param name="securityStatus">Raw system security status</param>
        /// <param name="customsCodeExpertiseLevel">CCE skill level 0-5</param>
        /// <param name="pocoTaxRate">Player-configured POCO tax rate (0.0 to 1.0), null for NPC CO</param>
        /// <returns>Total tax rate applied to exports</returns>
        public static double GetTotalExportTaxRate(float securityStatus, int customsCodeExpertiseLevel, double? pocoTaxRate = null)
        {
            double npcRate = GetEffectiveNpcTaxRate(securityStatus, customsCodeExpertiseLevel);

            if (pocoTaxRate.HasValue)
                return npcRate + Math.Max(0, pocoTaxRate.Value);

            return npcRate;
        }

        /// <summary>
        /// Calculates the ISK cost to export a quantity of items from a planet.
        /// Tax = BaseValue × Volume × TaxRate
        /// BaseValue is the CCP-assigned base value for the item type (not market price).
        /// </summary>
        /// <param name="baseValue">CCP base value per unit (from SDE typeDogma/basePrice)</param>
        /// <param name="quantity">Number of units to export</param>
        /// <param name="taxRate">Total tax rate from GetTotalExportTaxRate()</param>
        /// <returns>ISK cost of the export</returns>
        public static double CalculateExportTax(double baseValue, int quantity, double taxRate)
        {
            if (baseValue <= 0 || quantity <= 0 || taxRate <= 0)
                return 0;

            return baseValue * quantity * taxRate;
        }

        /// <summary>
        /// Calculates the ISK cost to import items to a planet.
        /// Import tax uses the same formula as export but at half the rate.
        /// </summary>
        public static double CalculateImportTax(double baseValue, int quantity, double taxRate)
        {
            if (baseValue <= 0 || quantity <= 0 || taxRate <= 0)
                return 0;

            return baseValue * quantity * (taxRate * 0.5);
        }

        /// <summary>
        /// Rounds a raw security status for display, implementing EVE's special rounding rule:
        /// positive values in (0, 0.05] round UP to 0.1, never to 0.0.
        /// </summary>
        public static double RoundSecurityForDisplay(double securityStatus)
        {
            if (securityStatus == 0.0)
                return 0.0;

            if (securityStatus > 0.0 && securityStatus <= 0.05)
                return 0.1;

            return Math.Round(securityStatus, 1, MidpointRounding.AwayFromZero);
        }

        /// <summary>
        /// Gets the EVE-standard color hex code for a security status value (rounded display).
        /// </summary>
        public static string GetSecurityColor(double securityStatus)
        {
            double rounded = RoundSecurityForDisplay(securityStatus);

            if (rounded >= 1.0) return "#2C75E1";
            if (rounded >= 0.9) return "#399AEB";
            if (rounded >= 0.8) return "#4ECEF8";
            if (rounded >= 0.7) return "#60DBA3";
            if (rounded >= 0.6) return "#71E754";
            if (rounded >= 0.5) return "#F5FF83";
            if (rounded >= 0.4) return "#DC6C06";
            if (rounded >= 0.3) return "#CE440F";
            if (rounded >= 0.2) return "#BB1116";
            if (rounded >= 0.1) return "#731F1F";
            return "#8D3163";
        }
    }
}
