// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Data;
using EveLens.Common.Events;
using EveLens.Common.Models;
using EveLens.Common.Services.Planetary;
using EveLens.Core.Interfaces;

namespace EveLens.Common.ViewModels
{
    public sealed class PlanetaryDashboardViewModel : CharacterViewModelBase
    {
        private List<ColonyCardData> _colonies = new();
        private string _textFilter = string.Empty;
        private double _totalNetIskPerDay;
        private int _totalColonies;
        private int _idleColonies;
        private int _expiringColonies;
        private bool _hasAnyEconomicsData;
        private int _customsCodeExpertiseLevel;

        public PlanetaryDashboardViewModel(IEventAggregator eventAggregator, IDispatcher? dispatcher = null)
            : base(eventAggregator, dispatcher)
        {
            SubscribeForCharacter<CharacterPlanetaryColoniesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterPlanetaryLayoutUpdatedEvent>(e => Refresh());
            // ISK/day depends on market prices, which load asynchronously after the first
            // request — repaint when they land, else economics stay 0 until the next
            // colony ESI refresh (Issue #66).
            Subscribe<ItemPricesUpdatedEvent>(e => Refresh());
        }

        public PlanetaryDashboardViewModel() : base()
        {
            SubscribeForCharacter<CharacterPlanetaryColoniesUpdatedEvent>(e => Refresh());
            SubscribeForCharacter<CharacterPlanetaryLayoutUpdatedEvent>(e => Refresh());
            Subscribe<ItemPricesUpdatedEvent>(e => Refresh());
        }

        public IReadOnlyList<ColonyCardData> Colonies => _colonies;

        public string TextFilter
        {
            get => _textFilter;
            set
            {
                if (SetProperty(ref _textFilter, value))
                    Refresh();
            }
        }

        public double TotalNetIskPerDay
        {
            get => _totalNetIskPerDay;
            private set => SetProperty(ref _totalNetIskPerDay, value);
        }

        public int TotalColonies
        {
            get => _totalColonies;
            private set => SetProperty(ref _totalColonies, value);
        }

        public int IdleColonies
        {
            get => _idleColonies;
            private set => SetProperty(ref _idleColonies, value);
        }

        public int ExpiringColonies
        {
            get => _expiringColonies;
            private set => SetProperty(ref _expiringColonies, value);
        }

        public bool HasAnyEconomicsData
        {
            get => _hasAnyEconomicsData;
            private set => SetProperty(ref _hasAnyEconomicsData, value);
        }

        protected override void OnCharacterChanged()
        {
            base.OnCharacterChanged();
            UpdateSkillLevel();
            Refresh();
        }

        public void Refresh()
        {
            if (Character is not CCPCharacter ccp)
            {
                _colonies = new List<ColonyCardData>();
                OnPropertyChanged(nameof(Colonies));
                return;
            }

            var economicsService = new ColonyEconomicsService(_customsCodeExpertiseLevel);
            var cards = new List<ColonyCardData>();

            foreach (var colony in ccp.PlanetaryColonies)
            {
                var analysis = ProductionChainAnalyzer.Analyze(colony);
                var economics = economicsService.Calculate(analysis);

                var card = new ColonyCardData(colony, analysis, economics);

                if (!string.IsNullOrWhiteSpace(_textFilter))
                {
                    bool match = card.PlanetName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase) ||
                                 card.PlanetTypeName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase) ||
                                 card.SystemName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase) ||
                                 card.OutputName.Contains(_textFilter, StringComparison.OrdinalIgnoreCase);
                    if (!match) continue;
                }

                cards.Add(card);
            }

            cards.Sort((a, b) =>
            {
                int healthCompare = GetHealthSortOrder(a.Health).CompareTo(GetHealthSortOrder(b.Health));
                if (healthCompare != 0) return healthCompare;
                return a.TimeUntilIdle.CompareTo(b.TimeUntilIdle);
            });

            _colonies = cards;
            TotalColonies = cards.Count;
            IdleColonies = cards.Count(c => c.Health == ColonyHealthStatus.Idle);
            ExpiringColonies = cards.Count(c => c.Health == ColonyHealthStatus.Expiring);
            TotalNetIskPerDay = cards.Where(c => c.HasYieldData).Sum(c => c.NetIskPerDay);
            HasAnyEconomicsData = cards.Any(c => c.HasYieldData);
            OnPropertyChanged(nameof(Colonies));
        }

        private void UpdateSkillLevel()
        {
            _customsCodeExpertiseLevel = 0;

            if (Character == null) return;

            var skill = StaticSkills.GetSkillByName("Customs Code Expertise");
            if (skill != null)
                _customsCodeExpertiseLevel = (int)Character.GetSkillLevel(skill);
        }

        private static int GetHealthSortOrder(ColonyHealthStatus status)
        {
            return status switch
            {
                ColonyHealthStatus.Idle => 0,
                ColonyHealthStatus.Expiring => 1,
                ColonyHealthStatus.Optimal => 2,
                ColonyHealthStatus.NoExtractors => 3,
                _ => 99
            };
        }
    }

    public sealed class ColonyCardData
    {
        public PlanetaryColony Colony { get; }
        public ColonyAnalysis Analysis { get; }
        public ColonyEconomics Economics { get; }

        public string PlanetName => Colony.PlanetName;
        public string PlanetTypeName => Colony.PlanetTypeName;
        public string SystemName => Colony.SolarSystem?.Name ?? "Unknown";
        public float SecurityStatus => Colony.SolarSystem?.SecurityLevel ?? 0f;
        public string SecurityDisplay => PlanetaryTaxCalculator.RoundSecurityForDisplay(SecurityStatus).ToString("0.0");
        public string SecurityColor => PlanetaryTaxCalculator.GetSecurityColor(SecurityStatus);
        public ColonyHealthStatus Health => Analysis.Health;
        public TimeSpan TimeUntilIdle => Analysis.TimeUntilFirstIdle;
        public int ActiveExtractors => Analysis.ActiveExtractorCount;
        public int TotalExtractors => Analysis.TotalExtractorCount;
        public int FactoryCount => Analysis.FactoryCount;

        public double ExtractionProgress
        {
            get
            {
                if (Analysis.Extractors.Count == 0) return 0;
                return Analysis.Extractors.Average(e => e.PercentComplete);
            }
        }

        public double GrossIskPerDay => Economics.GrossIskPerDay;
        public double TaxCostPerDay => Economics.TaxCostPerDay;
        public double NetIskPerDay => Economics.NetIskPerDay;
        public double TaxRate => Economics.TaxRate;

        public string OutputName
        {
            get
            {
                if (Economics.Outputs.Count == 0) return "No output";
                return Economics.Outputs[0].TypeName;
            }
        }

        public ProductionTier OutputTier
        {
            get
            {
                if (Economics.Outputs.Count == 0) return ProductionTier.Basic;
                return Economics.Outputs[0].Tier;
            }
        }

        public string OutputTierLabel => OutputTier switch
        {
            ProductionTier.Basic => "P1",
            ProductionTier.Advanced => "P2",
            ProductionTier.AdvancedP3 => "P3",
            ProductionTier.HighTech => "P4",
            _ => "P0"
        };

        public bool HasYieldData => Analysis.HasYieldData;

        public string TimeRemainingDisplay
        {
            get
            {
                if (Health == ColonyHealthStatus.Idle)
                    return "IDLE";
                if (Health == ColonyHealthStatus.NoExtractors)
                    return "Factories only";
                if (TimeUntilIdle <= TimeSpan.Zero)
                    return "Expired";

                if (TimeUntilIdle.TotalDays >= 1)
                    return $"{(int)TimeUntilIdle.TotalDays}d {TimeUntilIdle.Hours}h";
                if (TimeUntilIdle.TotalHours >= 1)
                    return $"{(int)TimeUntilIdle.TotalHours}h {TimeUntilIdle.Minutes}m";
                return $"{TimeUntilIdle.Minutes}m";
            }
        }

        public string HealthLabel => Health switch
        {
            ColonyHealthStatus.Optimal => "Running",
            ColonyHealthStatus.Expiring => "Expiring Soon",
            ColonyHealthStatus.Idle => "Idle",
            ColonyHealthStatus.NoExtractors => "Factories Only",
            _ => ""
        };

        public IReadOnlyList<Bottleneck> Bottlenecks => Analysis.Bottlenecks;

        internal ColonyCardData(PlanetaryColony colony, ColonyAnalysis analysis, ColonyEconomics economics)
        {
            Colony = colony;
            Analysis = analysis;
            Economics = economics;
        }
    }
}
