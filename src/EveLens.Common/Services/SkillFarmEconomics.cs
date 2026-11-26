// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Helpers;

namespace EveLens.Common.Services
{
    /// <summary>What a skill farm's monthly figures are worked out from.</summary>
    public sealed record SkillFarmMonthlyInput
    {
        /// <summary>The number of farm characters.</summary>
        public int FarmCharacters { get; init; }

        /// <summary>
        /// One account label per farm character; empty for a character without a label.
        /// Characters sharing a label share an account.
        /// </summary>
        public IReadOnlyList<string> AccountLabels { get; init; } = Array.Empty<string>();

        /// <summary>How many unlabeled farm characters share one account.</summary>
        public int CharactersPerAccount { get; init; } = 1;

        /// <summary>The skill points per hour all farm characters train together.</summary>
        public double SkillPointsPerHour { get; init; }

        /// <summary>The ISK price of one PLEX.</summary>
        public double PlexPrice { get; init; }

        /// <summary>The ISK price of one large skill injector.</summary>
        public double InjectorPrice { get; init; }

        /// <summary>The ISK price of one skill extractor.</summary>
        public double ExtractorPrice { get; init; }

        /// <summary>The sales tax paid on injector sales, as a fraction of the price.</summary>
        public double SalesTaxRate { get; init; }
    }

    /// <summary>A skill farm's figures for one month.</summary>
    public sealed record SkillFarmMonthly
    {
        /// <summary>The number of accounts the farm characters sit on.</summary>
        public int FarmAccounts { get; init; }

        /// <summary>The number of characters training on a multiple character training slot.</summary>
        public int MctSlots { get; init; }

        /// <summary>
        /// True when no character is training and the figures assume every character
        /// trains at <see cref="SkillFarmEconomics.MaxSkillPointsPerHour"/>.
        /// </summary>
        public bool IsEstimated { get; init; }

        /// <summary>The skill points per hour the figures are based on.</summary>
        public double SkillPointsPerHour { get; init; }

        /// <summary>The number of large extractions the month's training fills.</summary>
        public double Extractions { get; init; }

        /// <summary>The ISK cost of Omega time and multiple character training.</summary>
        public double SubscriptionCost { get; init; }

        /// <summary>The ISK from selling the month's injectors, after sales tax.</summary>
        public double Revenue { get; init; }

        /// <summary>The ISK cost of the month's extractors.</summary>
        public double ExtractorCost { get; init; }

        /// <summary>Revenue less extractors and subscription.</summary>
        public double Net => Revenue - ExtractorCost - SubscriptionCost;

        /// <summary>The ISK the farm earns towards its subscription; zero when the subscription costs nothing.</summary>
        public double SubscriptionSavings => SubscriptionCost > 0 ? Revenue - ExtractorCost : 0;
    }

    /// <summary>
    /// Monthly cost and revenue of a skill farm.
    /// </summary>
    public static class SkillFarmEconomics
    {
        /// <summary>PLEX for one month of Omega time on one account.</summary>
        public const double OmegaPlexPerMonth = 500.0;

        /// <summary>PLEX for one month of multiple character training on one character.</summary>
        public const double MctPlexPerMonth = 485.0;

        /// <summary>
        /// Skill points per hour of a character with a full remap into a skill's two
        /// attributes and +5 implants in both.
        /// </summary>
        public const double MaxSkillPointsPerHour = 2700.0;

        /// <summary>The most farm characters one account can hold.</summary>
        public const int MaxCharactersPerAccount = 3;

        /// <summary>The hours a month is counted as.</summary>
        public const double HoursPerMonth = 24 * 30;

        /// <summary>
        /// The number of accounts needed. Characters sharing a label share one account;
        /// unlabeled characters fill accounts of <paramref name="charactersPerAccount"/> each.
        /// </summary>
        public static int AccountsFor(IEnumerable<string> accountLabels, int charactersPerAccount)
        {
            int perAccount = Math.Clamp(charactersPerAccount, 1, MaxCharactersPerAccount);
            List<string> labels = accountLabels.Select(label => (label ?? string.Empty).Trim()).ToList();
            int labeled = labels.Where(label => label.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase).Count();
            int unlabeled = labels.Count(label => label.Length == 0);
            return labeled + (unlabeled == 0 ? 0 : (unlabeled + perAccount - 1) / perAccount);
        }

        /// <summary>
        /// Works out the month's figures.
        /// </summary>
        /// <exception cref="ArgumentNullException">input</exception>
        public static SkillFarmMonthly Monthly(SkillFarmMonthlyInput input)
        {
            if (input == null) throw new ArgumentNullException(nameof(input));

            int accounts = AccountsFor(input.AccountLabels, input.CharactersPerAccount);
            int mctSlots = Math.Max(0, input.FarmCharacters - accounts);

            bool estimated = input.SkillPointsPerHour == 0 && input.FarmCharacters > 0;
            double spPerHour = estimated
                ? input.FarmCharacters * MaxSkillPointsPerHour
                : input.SkillPointsPerHour;
            double extractions = spPerHour * HoursPerMonth / SkillInjectors.SkillPointsPerLargeExtraction;

            return new SkillFarmMonthly
            {
                FarmAccounts = accounts,
                MctSlots = mctSlots,
                IsEstimated = estimated,
                SkillPointsPerHour = spPerHour,
                Extractions = extractions,
                SubscriptionCost = (accounts * OmegaPlexPerMonth + mctSlots * MctPlexPerMonth) * input.PlexPrice,
                Revenue = extractions * input.InjectorPrice * (1 - input.SalesTaxRate),
                ExtractorCost = extractions * input.ExtractorPrice,
            };
        }
    }
}
