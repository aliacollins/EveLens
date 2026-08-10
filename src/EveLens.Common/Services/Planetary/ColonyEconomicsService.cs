// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.MarketPricer;
using EveLens.Common.Models;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Calculates economic metrics for PI colonies by combining extraction yields,
    /// market prices, and tax rates into actionable ISK/hr figures.
    /// </summary>
    public sealed class ColonyEconomicsService
    {
        private readonly Func<int, double> _priceProvider;
        private readonly int _customsCodeExpertiseLevel;
        private readonly double? _pocoTaxOverride;

        /// <summary>
        /// Creates a new economics service for a specific character context.
        /// </summary>
        /// <param name="customsCodeExpertiseLevel">Character's CCE skill level (0-5)</param>
        /// <param name="pocoTaxOverride">User-configured POCO tax rate, null for NPC default</param>
        /// <param name="priceProvider">Optional price lookup override (for testing)</param>
        public ColonyEconomicsService(int customsCodeExpertiseLevel, double? pocoTaxOverride = null, Func<int, double> priceProvider = null)
        {
            _customsCodeExpertiseLevel = Math.Clamp(customsCodeExpertiseLevel, 0, 5);
            _pocoTaxOverride = pocoTaxOverride;
            _priceProvider = priceProvider ?? DefaultPriceProvider;
        }

        /// <summary>
        /// Calculates full economics for a colony analysis.
        /// </summary>
        public ColonyEconomics Calculate(ColonyAnalysis analysis)
        {
            if (analysis == null || analysis.Colony == null)
                return ColonyEconomics.Empty;

            float securityStatus = analysis.Colony.SolarSystem?.SecurityLevel ?? 1.0f;
            double taxRate = PlanetaryTaxCalculator.GetTotalExportTaxRate(
                securityStatus, _customsCodeExpertiseLevel, _pocoTaxOverride);

            var outputItems = GetFinalOutputs(analysis);
            double grossIskPerHour = 0;
            double taxCostPerHour = 0;

            foreach (var output in outputItems)
            {
                double price = _priceProvider(output.TypeID);
                double iskPerHour = output.UnitsPerHour * price;
                grossIskPerHour += iskPerHour;

                // Tax is based on CCP base value, not market price
                // For simplicity, use market price as proxy (CCP base values aren't in our data)
                double taxPerUnit = price * taxRate;
                taxCostPerHour += output.UnitsPerHour * taxPerUnit;
            }

            double netIskPerHour = grossIskPerHour - taxCostPerHour;

            return new ColonyEconomics(
                grossIskPerHour: grossIskPerHour,
                taxCostPerHour: taxCostPerHour,
                netIskPerHour: netIskPerHour,
                taxRate: taxRate,
                securityStatus: securityStatus,
                securityClass: PlanetaryTaxCalculator.GetSecurityClass(securityStatus),
                outputs: outputItems,
                timeUntilIdle: analysis.TimeUntilFirstIdle,
                health: analysis.Health);
        }

        /// <summary>
        /// Calculates a quick ISK estimate for an extractor based on its output type and current rate.
        /// </summary>
        public double EstimateExtractorIskPerHour(ExtractorInfo extractor)
        {
            if (extractor == null || extractor.OutputTypeID <= 0)
                return 0;

            double price = _priceProvider(extractor.OutputTypeID);
            return extractor.CurrentYieldPerHour * price;
        }

        /// <summary>
        /// Determines the "final" outputs of the colony — the highest-tier products being made.
        /// If there are factories, the final output is the highest-tier factory output.
        /// If only extractors, the output is raw materials.
        /// </summary>
        private List<OutputItem> GetFinalOutputs(ColonyAnalysis analysis)
        {
            var outputs = new List<OutputItem>();

            if (analysis.Factories.Count > 0)
            {
                // Find highest-tier factories — their outputs are what gets exported
                var maxTier = analysis.Factories.Max(f => f.Tier);
                var topFactories = analysis.Factories.Where(f => f.Tier == maxTier);

                foreach (var factory in topFactories)
                {
                    outputs.Add(new OutputItem(
                        factory.Schematic.Output.TypeID,
                        factory.Schematic.Output.TypeName,
                        factory.OutputPerHour,
                        factory.Tier));
                }
            }
            else
            {
                // No factories — raw extractor output
                foreach (var extractor in analysis.Extractors)
                {
                    if (extractor.OutputTypeID > 0)
                    {
                        outputs.Add(new OutputItem(
                            extractor.OutputTypeID,
                            extractor.OutputTypeName,
                            extractor.CurrentYieldPerHour,
                            ProductionTier.Basic));
                    }
                }
            }

            return outputs;
        }

        private static double DefaultPriceProvider(int typeId)
        {
            // Use first available pricer
            var providers = ItemPricer.Providers;
            foreach (var provider in providers)
            {
                if (provider.Queried)
                {
                    double price = provider.GetPriceByTypeID(typeId);
                    if (price > 0) return price;
                }
            }
            return 0;
        }
    }

    #region Economics Result Types

    public sealed class ColonyEconomics
    {
        public static readonly ColonyEconomics Empty = new(0, 0, 0, 0, 0, PlanetaryTaxCalculator.SecurityClass.HighSec, new(), TimeSpan.Zero, ColonyHealthStatus.NoExtractors);

        public double GrossIskPerHour { get; }
        public double TaxCostPerHour { get; }
        public double NetIskPerHour { get; }
        public double GrossIskPerDay => GrossIskPerHour * 24;
        public double TaxCostPerDay => TaxCostPerHour * 24;
        public double NetIskPerDay => NetIskPerHour * 24;
        public double TaxRate { get; }
        public float SecurityStatus { get; }
        public PlanetaryTaxCalculator.SecurityClass SecurityClass { get; }
        public IReadOnlyList<OutputItem> Outputs { get; }
        public TimeSpan TimeUntilIdle { get; }
        public ColonyHealthStatus Health { get; }

        internal ColonyEconomics(double grossIskPerHour, double taxCostPerHour, double netIskPerHour,
            double taxRate, float securityStatus, PlanetaryTaxCalculator.SecurityClass securityClass,
            List<OutputItem> outputs, TimeSpan timeUntilIdle, ColonyHealthStatus health)
        {
            GrossIskPerHour = grossIskPerHour;
            TaxCostPerHour = taxCostPerHour;
            NetIskPerHour = netIskPerHour;
            TaxRate = taxRate;
            SecurityStatus = securityStatus;
            SecurityClass = securityClass;
            Outputs = outputs;
            TimeUntilIdle = timeUntilIdle;
            Health = health;
        }
    }

    public sealed class OutputItem
    {
        public int TypeID { get; }
        public string TypeName { get; }
        public double UnitsPerHour { get; }
        public ProductionTier Tier { get; }

        internal OutputItem(int typeId, string typeName, double unitsPerHour, ProductionTier tier)
        {
            TypeID = typeId;
            TypeName = typeName;
            UnitsPerHour = unitsPerHour;
            Tier = tier;
        }
    }

    #endregion
}
