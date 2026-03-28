// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using EveLens.Common.Constants;
using EveLens.Common.Data;
using EveLens.Common.MarketPricer;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the Skill Farm Dashboard. Shows extraction readiness,
    /// revenue calculations, and alerts for designated farm characters.
    /// </summary>
    public sealed class SkillFarmDashboardViewModel : IDisposable
    {
        private const long SpPerLargeExtraction = 500_000;
        private const long SpPerSmallExtraction = 100_000;
        private const long MinimumSPForExtraction = 5_000_000;

        // Accounting skill: reduces sales tax from 7.5% by 1.1% per level
        private const double BaseSalesTaxPercent = 7.5;
        private const double SalesTaxReductionPerLevel = 1.1;

        private List<FarmCharacterEntry> _entries = new();
        private List<FarmAlert> _alerts = new();

        // Market prices (auto-fetched from ItemPricer)
        public double InjectorPrice { get; private set; }
        public double SmallInjectorPrice { get; private set; }
        public double ExtractorPrice { get; private set; }
        public double PlexPrice { get; private set; }

        // Summary
        public IReadOnlyList<FarmCharacterEntry> Entries => _entries;
        public IReadOnlyList<FarmAlert> Alerts => _alerts;
        public int ReadyCount => _entries.Count(e => e.ExtractionsAvailable > 0);
        public int TrainingCount => _entries.Count(e => e.ExtractionsAvailable == 0 && e.IsTraining);
        public int PausedCount => _entries.Count(e => !e.IsTraining);
        public int TotalCharacters => _entries.Count;

        public double TotalRevenueToday => _entries.Sum(e => e.ExtractionsAvailable * e.NetProfitPerExtraction);
        public double TotalExtractionsToday => _entries.Sum(e => e.ExtractionsAvailable);

        private static readonly HttpClient s_httpClient = new();
        private const int TheForgeRegionId = 10000002;
        private const int PlexRegionId = 19000001;   // PLEX trades in its own region
        private const long JitaStationId = 60003760;

        /// <summary>
        /// Fetches Jita sell prices directly from ESI for the 4 key items.
        /// </summary>
        public async Task RefreshPricesAsync()
        {
            try
            {
                var tasks = new[]
                {
                    FetchSellPrice(TheForgeRegionId, DBConstants.LargeSkillInjectorID),
                    FetchSellPrice(TheForgeRegionId, DBConstants.SmallSkillInjectorID),
                    FetchSellPrice(TheForgeRegionId, DBConstants.SkillExtractorID),
                    FetchSellPrice(PlexRegionId, DBConstants.PlexTypeID)
                };

                var prices = await Task.WhenAll(tasks);

                InjectorPrice = prices[0];
                SmallInjectorPrice = prices[1];
                ExtractorPrice = prices[2];
                PlexPrice = prices[3];
            }
            catch
            {
                // Price fetch is best-effort
            }
        }

        /// <summary>
        /// Fetches the lowest Jita sell price for a type ID from ESI.
        /// </summary>
        private static async Task<double> FetchSellPrice(int regionId, int typeId)
        {
            try
            {
                var url = $"https://esi.evetech.net/latest/markets/{regionId}/orders/" +
                          $"?order_type=sell&type_id={typeId}&datasource=tranquility";

                var response = await s_httpClient.GetStringAsync(url);
                var orders = JsonSerializer.Deserialize<List<EsiMarketOrder>>(response);

                if (orders == null || orders.Count == 0) return 0;

                // Find lowest sell price at Jita 4-4, or lowest in region
                var jitaOrders = orders.Where(o => o.location_id == JitaStationId && !o.is_buy_order);
                var lowestJita = jitaOrders.Any() ? jitaOrders.Min(o => o.price) : 0;

                if (lowestJita > 0) return lowestJita;

                // Fallback: lowest sell in The Forge
                var sellOrders = orders.Where(o => !o.is_buy_order);
                return sellOrders.Any() ? sellOrders.Min(o => o.price) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>True if prices have loaded (non-zero).</summary>
        public bool PricesLoaded => InjectorPrice > 0 && ExtractorPrice > 0;

        // Minimal ESI market order DTO
        private sealed class EsiMarketOrder
        {
            public double price { get; set; }
            public bool is_buy_order { get; set; }
            public long location_id { get; set; }
            public int volume_remain { get; set; }
        }

        /// <summary>
        /// Rebuilds the farm character list from settings and live character data.
        /// </summary>
        public void Refresh()
        {

            var settings = Settings.UI.SkillFarm;
            var allChars = AppServices.Characters.Where(c => c.Monitored).ToList();
            var farmGuids = new HashSet<Guid>(settings.FarmCharacters.Select(f => f.CharacterGuid));

            var entries = new List<FarmCharacterEntry>();
            var alerts = new List<FarmAlert>();

            foreach (var farmSetting in settings.FarmCharacters)
            {
                var character = allChars.FirstOrDefault(c => c.Guid == farmSetting.CharacterGuid);
                if (character == null) continue;

                var entry = BuildEntry(character, farmSetting);
                entries.Add(entry);

                // Generate alerts
                if (entry.ExtractionsAvailable > 0 && !entry.IsTraining)
                    alerts.Add(new FarmAlert(character.Name, FarmAlertType.ReadyButPaused,
                        "Ready to extract but queue is paused. Extract now before SP is wasted."));

                if (entry.ImplantLevel == 0 && entry.IsTraining)
                    alerts.Add(new FarmAlert(character.Name, FarmAlertType.NoImplants,
                        $"No learning implants. Training at {entry.SpPerHour:N0} SP/hr instead of ~2,700 SP/hr."));

                if (entry.ImplantLevel > 0 && entry.ImplantLevel < 5 && entry.IsTraining)
                    alerts.Add(new FarmAlert(character.Name, FarmAlertType.SuboptimalImplants,
                        $"+{entry.ImplantLevel} implants. Upgrade to +5 for faster extraction cycles."));
            }

            // Sort: ready first, then by time to extract
            entries.Sort((a, b) =>
            {
                // Ready characters first (descending by extractions available)
                if (a.ExtractionsAvailable > 0 && b.ExtractionsAvailable == 0) return -1;
                if (b.ExtractionsAvailable > 0 && a.ExtractionsAvailable == 0) return 1;

                // Paused at the bottom
                if (!a.IsTraining && b.IsTraining) return 1;
                if (a.IsTraining && !b.IsTraining) return -1;

                // Then by time to next extraction
                return a.TimeToNextExtraction.CompareTo(b.TimeToNextExtraction);
            });

            _entries = entries;
            _alerts = alerts;
        }

        private FarmCharacterEntry BuildEntry(Character character, SkillFarmCharacterSettings settings)
        {
            long currentSP = character.SkillPoints;
            long threshold = Math.Max(settings.ExtractionThreshold, MinimumSPForExtraction);
            long extractableSP = Math.Max(0, currentSP - MinimumSPForExtraction);
            int largeExtractions = (int)(extractableSP / SpPerLargeExtraction);

            // SP/hour from current training
            double spPerHour = 0;
            bool isTraining = false;
            if (character is CCPCharacter ccp && ccp.IsTraining)
            {
                isTraining = true;
                var currentSkill = ccp.CurrentlyTrainingSkill;
                if (currentSkill != null)
                    spPerHour = currentSkill.SkillPointsPerHour;
            }

            // Time to next extraction
            TimeSpan timeToNext = TimeSpan.Zero;
            if (largeExtractions == 0 && spPerHour > 0)
            {
                long spNeeded = MinimumSPForExtraction + SpPerLargeExtraction - currentSP;
                if (spNeeded > 0)
                    timeToNext = TimeSpan.FromHours(spNeeded / spPerHour);
            }

            // Implant level (check for attribute implants)
            int implantLevel = GetLearningImplantLevel(character);

            // Tax calculation from Accounting skill
            long accountingLevel = 0;
            var accountingSkill = character.Skills?[DBConstants.AccountingSkillID];
            if (accountingSkill != null && accountingSkill.IsKnown)
                accountingLevel = accountingSkill.LastConfirmedLvl;

            double salesTaxPercent = BaseSalesTaxPercent - (SalesTaxReductionPerLevel * accountingLevel);
            double salesTaxRate = salesTaxPercent / 100.0;

            // Revenue per extraction
            double grossRevenue = InjectorPrice;
            double salesTax = grossRevenue * salesTaxRate;
            double netRevenue = grossRevenue - salesTax;
            double netProfit = netRevenue - ExtractorPrice;

            return new FarmCharacterEntry
            {
                Character = character,
                CurrentSP = currentSP,
                SpPerHour = spPerHour,
                IsTraining = isTraining,
                ImplantLevel = implantLevel,
                ExtractionsAvailable = largeExtractions,
                TimeToNextExtraction = timeToNext,
                AccountingLevel = accountingLevel,
                SalesTaxPercent = salesTaxPercent,
                GrossRevenuePerExtraction = grossRevenue,
                NetProfitPerExtraction = netProfit,
                ExtractorCost = ExtractorPrice,
                Notes = settings.Notes
            };
        }

        private static int GetLearningImplantLevel(Character character)
        {
            try
            {
                var implantSet = character.CurrentImplants;
                if (implantSet == null) return 0;

                // Find the highest attribute bonus among all implants
                int maxBonus = 0;
                foreach (var implant in implantSet)
                {
                    if (implant != null && implant.Bonus > maxBonus)
                        maxBonus = (int)implant.Bonus;
                }
                return maxBonus;
            }
            catch
            {
                return 0;
            }
        }

        public void AddFarmCharacter(Character character)
        {
            var settings = Settings.UI.SkillFarm;
            if (settings.FarmCharacters.Any(f => f.CharacterGuid == character.Guid))
                return;

            settings.FarmCharacters.Add(new SkillFarmCharacterSettings
            {
                CharacterGuid = character.Guid,
                ExtractionThreshold = settings.DefaultExtractionThreshold
            });
            Settings.Save();
        }

        public void RemoveFarmCharacter(Character character)
        {
            var settings = Settings.UI.SkillFarm;
            var toRemove = settings.FarmCharacters.FirstOrDefault(f => f.CharacterGuid == character.Guid);
            if (toRemove != null)
            {
                settings.FarmCharacters.Remove(toRemove);
                Settings.Save();
            }
        }

        public void Dispose() { }
    }

    /// <summary>A single farm character's status and economics.</summary>
    public sealed class FarmCharacterEntry
    {
        public Character Character { get; init; } = null!;
        public long CurrentSP { get; init; }
        public double SpPerHour { get; init; }
        public bool IsTraining { get; init; }
        public int ImplantLevel { get; init; }
        public int ExtractionsAvailable { get; init; }
        public TimeSpan TimeToNextExtraction { get; init; }
        public long AccountingLevel { get; init; }
        public double SalesTaxPercent { get; init; }
        public double GrossRevenuePerExtraction { get; init; }
        public double NetProfitPerExtraction { get; init; }
        public double ExtractorCost { get; init; }
        public string Notes { get; init; } = string.Empty;

        public string StatusText => ExtractionsAvailable > 0
            ? $"NOW ({ExtractionsAvailable}×)"
            : !IsTraining ? "PAUSED"
            : FormatTimeSpan(TimeToNextExtraction);

        public string SpPerHourText => SpPerHour > 0 ? $"{SpPerHour:N0}/hr" : "0";
        public string ImplantText => ImplantLevel > 0 ? $"+{ImplantLevel}" : "none";

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts <= TimeSpan.Zero) return "NOW";
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            return $"{ts.Minutes}m";
        }
    }

    /// <summary>An alert about a farm character needing attention.</summary>
    public sealed class FarmAlert
    {
        public string CharacterName { get; }
        public FarmAlertType Type { get; }
        public string Message { get; }

        public FarmAlert(string characterName, FarmAlertType type, string message)
        {
            CharacterName = characterName;
            Type = type;
            Message = message;
        }
    }

    public enum FarmAlertType
    {
        ReadyButPaused,
        NoImplants,
        SuboptimalImplants,
        SuboptimalRemap
    }
}
