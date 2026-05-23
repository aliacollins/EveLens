// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Models;
using EveLens.Common.Services;
using EveLens.Common.Services.Planetary;

namespace EveLens.Common.ViewModels
{
    /// <summary>
    /// ViewModel for the global Planetary Dashboard window — shows all characters' PI at a glance.
    /// </summary>
    public sealed class PlanetaryOverviewViewModel : ViewModelBase
    {
        private List<PlanetaryCharacterEntry> _entries = new();

        public IReadOnlyList<PlanetaryCharacterEntry> Entries => _entries;

        public int TotalCharacters => _entries.Count;
        public int TotalColonies => _entries.Sum(e => e.ColonyCount);
        public int IdleExtractors => _entries.Sum(e => e.IdleExtractorCount);
        public int ActiveExtractors => _entries.Sum(e => e.ActiveExtractorCount);
        public double TotalNetIskPerDay => _entries.Where(e => e.HasYieldData).Sum(e => e.NetIskPerDay);
        public bool HasAnyEconomicsData => _entries.Any(e => e.HasYieldData);
        public int CharactersNeedingAttention => _entries.Count(e => e.NeedsAttention);
        public int ExpiringCount => _entries.Sum(e => e.ExpiringExtractorCount);

        public void Refresh()
        {
            var entries = new List<PlanetaryCharacterEntry>();

            foreach (var character in AppServices.Characters.Where(c => c.Monitored))
            {
                if (character is not CCPCharacter ccp)
                    continue;

                var colonies = ccp.PlanetaryColonies.ToList();
                if (colonies.Count == 0)
                    continue;

                int cceLevel = 0;
                var cceSkill = StaticSkills.GetSkillByName("Customs Code Expertise");
                if (cceSkill != null)
                    cceLevel = (int)character.GetSkillLevel(cceSkill);

                var economicsService = new ColonyEconomicsService(cceLevel);
                var colonyAnalyses = new List<(ColonyAnalysis analysis, ColonyEconomics economics)>();

                foreach (var colony in colonies)
                {
                    var analysis = ProductionChainAnalyzer.Analyze(colony);
                    var economics = economicsService.Calculate(analysis);
                    colonyAnalyses.Add((analysis, economics));
                }

                var entry = new PlanetaryCharacterEntry(
                    character,
                    colonies.Count,
                    colonyAnalyses);

                entries.Add(entry);
            }

            entries.Sort((a, b) =>
            {
                int attentionCompare = b.NeedsAttention.CompareTo(a.NeedsAttention);
                if (attentionCompare != 0) return attentionCompare;
                int idleCompare = b.IdleExtractorCount.CompareTo(a.IdleExtractorCount);
                if (idleCompare != 0) return idleCompare;
                return a.TimeUntilFirstIdle.CompareTo(b.TimeUntilFirstIdle);
            });

            _entries = entries;
        }
    }

    public sealed class PlanetaryCharacterEntry
    {
        public Character Character { get; }
        public long CharacterID => Character.CharacterID;
        public string CharacterName => Character.Name;
        public int ColonyCount { get; }
        public int ActiveExtractorCount { get; }
        public int IdleExtractorCount { get; }
        public int ExpiringExtractorCount { get; }
        public int TotalExtractorCount { get; }
        public int FactoryCount { get; }
        public double GrossIskPerDay { get; }
        public double TaxCostPerDay { get; }
        public double NetIskPerDay { get; }
        public bool HasYieldData { get; }
        public TimeSpan TimeUntilFirstIdle { get; }
        public ColonyHealthStatus WorstHealth { get; }
        public bool NeedsAttention { get; }

        public string TimeDisplay
        {
            get
            {
                if (IdleExtractorCount == TotalExtractorCount && TotalExtractorCount > 0)
                    return "ALL IDLE";
                if (TimeUntilFirstIdle <= TimeSpan.Zero && TotalExtractorCount > 0)
                    return "Expired";
                if (TimeUntilFirstIdle.TotalDays >= 1)
                    return $"{(int)TimeUntilFirstIdle.TotalDays}d {TimeUntilFirstIdle.Hours}h";
                if (TimeUntilFirstIdle.TotalHours >= 1)
                    return $"{(int)TimeUntilFirstIdle.TotalHours}h {TimeUntilFirstIdle.Minutes}m";
                if (TimeUntilFirstIdle > TimeSpan.Zero)
                    return $"{TimeUntilFirstIdle.Minutes}m";
                return "--";
            }
        }

        public string HealthDisplay => WorstHealth switch
        {
            ColonyHealthStatus.Optimal => "Running",
            ColonyHealthStatus.Expiring => "Expiring",
            ColonyHealthStatus.Idle => "IDLE",
            ColonyHealthStatus.NoExtractors => "Factories",
            _ => ""
        };

        internal PlanetaryCharacterEntry(Character character, int colonyCount,
            List<(ColonyAnalysis analysis, ColonyEconomics economics)> analyses)
        {
            Character = character;
            ColonyCount = colonyCount;

            int active = 0, idle = 0, expiring = 0, total = 0, factories = 0;
            double grossIsk = 0, taxIsk = 0;
            bool hasYield = false;
            var earliestIdle = TimeSpan.MaxValue;
            var worstHealth = ColonyHealthStatus.Optimal;

            var alertMinutes = Settings.UI?.MainWindow?.Planetary?.AlertLeadTimeMinutes ?? 120;
            var leadTime = TimeSpan.FromMinutes(alertMinutes);
            var now = DateTime.UtcNow;

            foreach (var (analysis, economics) in analyses)
            {
                active += analysis.ActiveExtractorCount;
                idle += analysis.TotalExtractorCount - analysis.ActiveExtractorCount;
                total += analysis.TotalExtractorCount;
                factories += analysis.FactoryCount;

                if (analysis.HasYieldData)
                {
                    grossIsk += economics.GrossIskPerDay;
                    taxIsk += economics.TaxCostPerDay;
                    hasYield = true;
                }

                // Count extractors expiring within lead time
                foreach (var ext in analysis.Extractors)
                {
                    if (ext.Pin.ExpiryTime > now && (ext.Pin.ExpiryTime - now) <= leadTime)
                        expiring++;
                }

                if (analysis.TimeUntilFirstIdle > TimeSpan.Zero && analysis.TimeUntilFirstIdle < earliestIdle)
                    earliestIdle = analysis.TimeUntilFirstIdle;

                if (GetHealthSeverity(analysis.Health) > GetHealthSeverity(worstHealth))
                    worstHealth = analysis.Health;
            }

            ActiveExtractorCount = active;
            IdleExtractorCount = idle;
            ExpiringExtractorCount = expiring;
            TotalExtractorCount = total;
            FactoryCount = factories;
            GrossIskPerDay = grossIsk;
            TaxCostPerDay = taxIsk;
            NetIskPerDay = grossIsk - taxIsk;
            HasYieldData = hasYield;
            TimeUntilFirstIdle = earliestIdle == TimeSpan.MaxValue ? TimeSpan.Zero : earliestIdle;
            WorstHealth = worstHealth;
            NeedsAttention = idle > 0 || worstHealth == ColonyHealthStatus.Idle || worstHealth == ColonyHealthStatus.Expiring;
        }

        private static int GetHealthSeverity(ColonyHealthStatus health) => health switch
        {
            ColonyHealthStatus.Optimal => 0,
            ColonyHealthStatus.NoExtractors => 1,
            ColonyHealthStatus.Expiring => 2,
            ColonyHealthStatus.Idle => 3,
            _ => 0
        };
    }
}
