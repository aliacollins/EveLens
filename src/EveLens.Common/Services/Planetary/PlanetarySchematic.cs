// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Collections.Generic;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Represents a PI factory schematic — what inputs it needs and what it produces.
    /// </summary>
    public sealed class PlanetarySchematic
    {
        public int SchematicID { get; }
        public string Name { get; }
        public int CycleTimeSeconds { get; }
        public IReadOnlyList<SchematicMaterial> Inputs { get; }
        public SchematicMaterial Output { get; }
        public ProductionTier Tier { get; }

        public PlanetarySchematic(int id, string name, int cycleTime, SchematicMaterial[] inputs, SchematicMaterial output, ProductionTier tier)
        {
            SchematicID = id;
            Name = name;
            CycleTimeSeconds = cycleTime;
            Inputs = inputs;
            Output = output;
            Tier = tier;
        }

        /// <summary>
        /// Input demand per hour for a specific material type.
        /// </summary>
        public int GetInputDemandPerHour(int typeId)
        {
            int cyclesPerHour = 3600 / CycleTimeSeconds;
            foreach (var input in Inputs)
            {
                if (input.TypeID == typeId)
                    return input.Quantity * cyclesPerHour;
            }
            return 0;
        }

        /// <summary>
        /// Output production per hour.
        /// </summary>
        public int GetOutputPerHour()
        {
            int cyclesPerHour = 3600 / CycleTimeSeconds;
            return Output.Quantity * cyclesPerHour;
        }
    }

    /// <summary>
    /// A material input or output in a schematic.
    /// </summary>
    public sealed class SchematicMaterial
    {
        public int TypeID { get; }
        public string TypeName { get; }
        public int Quantity { get; }

        public SchematicMaterial(int typeId, string typeName, int quantity)
        {
            TypeID = typeId;
            TypeName = typeName;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// The production tier of a schematic (determines the factory type that runs it).
    /// </summary>
    public enum ProductionTier
    {
        /// <summary>P0 → P1: Basic Industry Facility (30min cycle, 3000 input → 20 output)</summary>
        Basic,

        /// <summary>P1 → P2: Advanced Industry Facility (1hr cycle, 40+40 input → 5 output)</summary>
        Advanced,

        /// <summary>P2 → P3: Advanced Industry Facility (1hr cycle, 10+10+... input → 3 output)</summary>
        AdvancedP3,

        /// <summary>P3 → P4: High-Tech Production Plant (1hr cycle, 6+6+40 input → 1 output)</summary>
        HighTech
    }
}
