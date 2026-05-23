// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;
using EveLens.Common.Constants;
using EveLens.Common.Models;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Analyzes a colony's production chain to identify structure, throughput, and bottlenecks.
    /// Takes raw pin/route/link data and produces an actionable analysis.
    /// </summary>
    public static class ProductionChainAnalyzer
    {
        /// <summary>
        /// Performs a full analysis of a colony's production chain.
        /// </summary>
        public static ColonyAnalysis Analyze(PlanetaryColony colony)
        {
            if (colony == null)
                return ColonyAnalysis.Empty;

            var pins = colony.Pins.ToList();
            var routes = colony.Routes.ToList();
            var links = colony.Links.ToList();

            if (pins.Count == 0)
                return ColonyAnalysis.Empty;

            var extractors = ClassifyExtractors(pins);
            var factories = ClassifyFactories(pins);
            var storage = ClassifyStorage(pins);
            var bottlenecks = DetectBottlenecks(extractors, factories, routes);

            return new ColonyAnalysis(
                colony,
                extractors,
                factories,
                storage,
                bottlenecks,
                CalculateOverallHealth(extractors, bottlenecks));
        }

        private static List<ExtractorInfo> ClassifyExtractors(List<PlanetaryPin> pins)
        {
            var result = new List<ExtractorInfo>();

            foreach (var pin in pins)
            {
                if (!DBConstants.EcuTypeIDs.Contains(pin.TypeID))
                    continue;

                int durationSeconds = 0;
                if (pin.ExpiryTime > pin.InstallTime)
                    durationSeconds = (int)(pin.ExpiryTime - pin.InstallTime).TotalSeconds;

                var yields = pin.CycleTime > 0 && durationSeconds > 0
                    ? ExtractionCalculator.CalculateCycleYields(pin.QuantityPerCycle, pin.CycleTime, durationSeconds)
                    : Array.Empty<int>();

                int currentCycleIndex = 0;
                if (pin.InstallTime != DateTime.MinValue && pin.CycleTime > 0)
                {
                    var elapsed = (DateTime.UtcNow - pin.InstallTime).TotalSeconds;
                    currentCycleIndex = Math.Min((int)(elapsed / pin.CycleTime), yields.Length - 1);
                    if (currentCycleIndex < 0) currentCycleIndex = 0;
                }

                double currentYieldPerHour = yields.Length > 0 && currentCycleIndex < yields.Length
                    ? ExtractionCalculator.CalculateYieldPerHourAtCycle(pin.QuantityPerCycle, pin.CycleTime, currentCycleIndex)
                    : 0;

                long totalYield = 0;
                for (int i = 0; i < yields.Length; i++)
                    totalYield += yields[i];

                long remainingYield = 0;
                for (int i = currentCycleIndex; i < yields.Length; i++)
                    remainingYield += yields[i];

                result.Add(new ExtractorInfo(
                    pin,
                    pin.ContentTypeID,
                    pin.ContentTypeName,
                    currentYieldPerHour,
                    totalYield,
                    remainingYield,
                    durationSeconds > 0 ? durationSeconds : 0,
                    currentCycleIndex,
                    yields.Length));
            }

            return result;
        }

        private static List<FactoryInfo> ClassifyFactories(List<PlanetaryPin> pins)
        {
            var result = new List<FactoryInfo>();

            foreach (var pin in pins)
            {
                if (pin.SchematicID <= 0)
                    continue;
                if (DBConstants.EcuTypeIDs.Contains(pin.TypeID))
                    continue;

                var schematic = PlanetarySchematicsProvider.GetSchematic((int)pin.SchematicID);
                if (schematic == null)
                    continue;

                result.Add(new FactoryInfo(pin, schematic));
            }

            return result;
        }

        private static List<StorageInfo> ClassifyStorage(List<PlanetaryPin> pins)
        {
            var result = new List<StorageInfo>();
            var storageGroupNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "Command Centers", "Spaceports", "Storage Facilities"
            };

            foreach (var pin in pins)
            {
                if (DBConstants.EcuTypeIDs.Contains(pin.TypeID))
                    continue;
                if (pin.SchematicID > 0)
                    continue;
                if (storageGroupNames.Contains(pin.GroupName ?? ""))
                    result.Add(new StorageInfo(pin));
            }

            return result;
        }

        private static List<Bottleneck> DetectBottlenecks(
            List<ExtractorInfo> extractors,
            List<FactoryInfo> factories,
            List<PlanetaryRoute> routes)
        {
            var bottlenecks = new List<Bottleneck>();

            // Group factories by their input material type to calculate total demand
            var demandByType = new Dictionary<int, double>();
            foreach (var factory in factories)
            {
                foreach (var input in factory.Schematic.Inputs)
                {
                    int demandPerHour = factory.Schematic.GetInputDemandPerHour(input.TypeID);
                    if (!demandByType.ContainsKey(input.TypeID))
                        demandByType[input.TypeID] = 0;
                    demandByType[input.TypeID] += demandPerHour;
                }
            }

            // Check if extractors can meet factory demand for P0 materials
            var supplyByType = new Dictionary<int, double>();
            foreach (var extractor in extractors)
            {
                if (extractor.OutputTypeID <= 0) continue;
                if (!supplyByType.ContainsKey(extractor.OutputTypeID))
                    supplyByType[extractor.OutputTypeID] = 0;
                supplyByType[extractor.OutputTypeID] += extractor.CurrentYieldPerHour;
            }

            // Also check factory outputs as supply for downstream factories
            foreach (var factory in factories)
            {
                int outputPerHour = factory.Schematic.GetOutputPerHour();
                int outputType = factory.Schematic.Output.TypeID;
                if (!supplyByType.ContainsKey(outputType))
                    supplyByType[outputType] = 0;
                supplyByType[outputType] += outputPerHour;
            }

            // Detect mismatches
            foreach (var kvp in demandByType)
            {
                int typeId = kvp.Key;
                double demand = kvp.Value;
                supplyByType.TryGetValue(typeId, out double supply);

                if (supply <= 0 && demand > 0)
                {
                    bottlenecks.Add(new Bottleneck(
                        typeId,
                        GetTypeName(typeId, factories, extractors),
                        supply,
                        demand,
                        BottleneckSeverity.Critical));
                }
                else if (supply < demand * 0.8)
                {
                    bottlenecks.Add(new Bottleneck(
                        typeId,
                        GetTypeName(typeId, factories, extractors),
                        supply,
                        demand,
                        BottleneckSeverity.Starving));
                }
                else if (supply > demand * 1.5)
                {
                    bottlenecks.Add(new Bottleneck(
                        typeId,
                        GetTypeName(typeId, factories, extractors),
                        supply,
                        demand,
                        BottleneckSeverity.Overflow));
                }
            }

            return bottlenecks;
        }

        private static string GetTypeName(int typeId, List<FactoryInfo> factories, List<ExtractorInfo> extractors)
        {
            foreach (var ext in extractors)
                if (ext.OutputTypeID == typeId)
                    return ext.OutputTypeName;

            foreach (var fac in factories)
            {
                if (fac.Schematic.Output.TypeID == typeId)
                    return fac.Schematic.Output.TypeName;
                foreach (var input in fac.Schematic.Inputs)
                    if (input.TypeID == typeId)
                        return input.TypeName;
            }

            return $"Type {typeId}";
        }

        private static ColonyHealthStatus CalculateOverallHealth(List<ExtractorInfo> extractors, List<Bottleneck> bottlenecks)
        {
            if (extractors.Count == 0)
                return ColonyHealthStatus.NoExtractors;

            bool allIdle = extractors.All(e => e.Pin.State == Enumerations.PlanetaryPinState.Idle);
            if (allIdle)
                return ColonyHealthStatus.Idle;

            bool anyIdle = extractors.Any(e => e.Pin.State == Enumerations.PlanetaryPinState.Idle);
            if (anyIdle)
                return ColonyHealthStatus.Idle;

            var alertMinutes = Settings.UI?.MainWindow?.Planetary?.AlertLeadTimeMinutes ?? 120;
            var leadTime = TimeSpan.FromMinutes(alertMinutes);
            var now = DateTime.UtcNow;
            bool anyExpiringSoon = extractors.Any(e =>
                e.Pin.ExpiryTime > now && (e.Pin.ExpiryTime - now) <= leadTime);

            if (anyExpiringSoon)
                return ColonyHealthStatus.Expiring;

            return ColonyHealthStatus.Optimal;
        }
    }

    #region Analysis Result Types

    public sealed class ColonyAnalysis
    {
        public static readonly ColonyAnalysis Empty = new(null, new(), new(), new(), new(), ColonyHealthStatus.NoExtractors);

        public PlanetaryColony Colony { get; }
        public IReadOnlyList<ExtractorInfo> Extractors { get; }
        public IReadOnlyList<FactoryInfo> Factories { get; }
        public IReadOnlyList<StorageInfo> Storage { get; }
        public IReadOnlyList<Bottleneck> Bottlenecks { get; }
        public ColonyHealthStatus Health { get; }

        public TimeSpan TimeUntilFirstIdle
        {
            get
            {
                var earliest = DateTime.MaxValue;
                foreach (var ext in Extractors)
                {
                    if (ext.Pin.ExpiryTime > DateTime.UtcNow && ext.Pin.ExpiryTime < earliest)
                        earliest = ext.Pin.ExpiryTime;
                }
                return earliest == DateTime.MaxValue ? TimeSpan.Zero : earliest - DateTime.UtcNow;
            }
        }

        public int TotalExtractorCount => Extractors.Count;
        public int ActiveExtractorCount => Extractors.Count(e => e.Pin.State == Enumerations.PlanetaryPinState.Extracting);
        public int FactoryCount => Factories.Count;
        public bool HasYieldData => Extractors.Any(e => e.CurrentYieldPerHour > 0);

        internal ColonyAnalysis(PlanetaryColony colony, List<ExtractorInfo> extractors,
            List<FactoryInfo> factories, List<StorageInfo> storage,
            List<Bottleneck> bottlenecks, ColonyHealthStatus health)
        {
            Colony = colony;
            Extractors = extractors;
            Factories = factories;
            Storage = storage;
            Bottlenecks = bottlenecks;
            Health = health;
        }
    }

    public sealed class ExtractorInfo
    {
        public PlanetaryPin Pin { get; }
        public int OutputTypeID { get; }
        public string OutputTypeName { get; }
        public double CurrentYieldPerHour { get; }
        public long TotalProgramYield { get; }
        public long RemainingYield { get; }
        public int TotalDurationSeconds { get; }
        public int CurrentCycleIndex { get; }
        public int TotalCycles { get; }
        public double PercentComplete => TotalCycles > 0 ? (double)CurrentCycleIndex / TotalCycles : 0;

        internal ExtractorInfo(PlanetaryPin pin, int outputTypeId, string outputTypeName,
            double currentYieldPerHour, long totalYield, long remainingYield,
            int totalDuration, int currentCycle, int totalCycles)
        {
            Pin = pin;
            OutputTypeID = outputTypeId;
            OutputTypeName = outputTypeName;
            CurrentYieldPerHour = currentYieldPerHour;
            TotalProgramYield = totalYield;
            RemainingYield = remainingYield;
            TotalDurationSeconds = totalDuration;
            CurrentCycleIndex = currentCycle;
            TotalCycles = totalCycles;
        }
    }

    public sealed class FactoryInfo
    {
        public PlanetaryPin Pin { get; }
        public PlanetarySchematic Schematic { get; }
        public int OutputPerHour => Schematic.GetOutputPerHour();
        public string OutputName => Schematic.Output.TypeName;
        public ProductionTier Tier => Schematic.Tier;

        internal FactoryInfo(PlanetaryPin pin, PlanetarySchematic schematic)
        {
            Pin = pin;
            Schematic = schematic;
        }
    }

    public sealed class StorageInfo
    {
        public PlanetaryPin Pin { get; }
        public string TypeName => Pin.TypeName;
        public int ContentQuantity => Pin.ContentQuantity;
        public double ContentVolume => Pin.ContentVolume;

        internal StorageInfo(PlanetaryPin pin)
        {
            Pin = pin;
        }
    }

    public sealed class Bottleneck
    {
        public int MaterialTypeID { get; }
        public string MaterialName { get; }
        public double SupplyPerHour { get; }
        public double DemandPerHour { get; }
        public BottleneckSeverity Severity { get; }
        public double Efficiency => DemandPerHour > 0 ? Math.Min(SupplyPerHour / DemandPerHour, 2.0) : 0;
        public double DeficitPerHour => Math.Max(0, DemandPerHour - SupplyPerHour);
        public double SurplusPerHour => Math.Max(0, SupplyPerHour - DemandPerHour);

        internal Bottleneck(int typeId, string name, double supply, double demand, BottleneckSeverity severity)
        {
            MaterialTypeID = typeId;
            MaterialName = name;
            SupplyPerHour = supply;
            DemandPerHour = demand;
            Severity = severity;
        }
    }

    public enum BottleneckSeverity
    {
        Optimal,
        Overflow,
        Starving,
        Critical
    }

    public enum ColonyHealthStatus
    {
        Optimal,
        Expiring,
        Idle,
        NoExtractors
    }

    #endregion
}
