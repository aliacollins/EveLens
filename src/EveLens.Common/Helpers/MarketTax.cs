// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using EveLens.Common.Constants;

namespace EveLens.Common.Helpers
{
    /// <summary>
    /// Market sales tax as a function of the seller's Accounting skill level.
    /// </summary>
    public static class MarketTax
    {
        private const long MaxSkillLevel = 5;

        /// <summary>
        /// The sales tax in percent of the sale price. Each Accounting level lowers the
        /// base tax by <see cref="EveConstants.SalesTaxReductionPerAccountingLevel"/> of
        /// the base. Levels outside 0-5 are clamped.
        /// </summary>
        /// <param name="accountingLevel">The seller's trained Accounting level.</param>
        public static double SalesTaxPercent(long accountingLevel)
        {
            long level = Math.Clamp(accountingLevel, 0, MaxSkillLevel);
            return EveConstants.SalesTaxBasePercent
                * (1.0 - EveConstants.SalesTaxReductionPerAccountingLevel * level);
        }

        /// <summary>The sales tax as a fraction of the sale price.</summary>
        /// <param name="accountingLevel">The seller's trained Accounting level.</param>
        public static double SalesTaxRate(long accountingLevel) => SalesTaxPercent(accountingLevel) / 100.0;
    }
}
