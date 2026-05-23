// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Linq;

namespace EveLens.Common.Services.Planetary
{
    /// <summary>
    /// Provides static lookup for PI factory schematics.
    /// Data sourced from SDE planetSchematics.yaml (68 schematics across 4 production tiers).
    /// </summary>
    public static class PlanetarySchematicsProvider
    {
        private static readonly Dictionary<int, PlanetarySchematic> s_schematics = new();
        private static readonly Dictionary<int, PlanetarySchematic> s_schematicsByOutputType = new();
        private static bool s_loaded;

        /// <summary>
        /// Gets a schematic by its ID (as referenced in ESI pin data).
        /// </summary>
        public static PlanetarySchematic GetSchematic(int schematicId)
        {
            EnsureLoaded();
            s_schematics.TryGetValue(schematicId, out var schematic);
            return schematic;
        }

        /// <summary>
        /// Gets the schematic that produces a given output type.
        /// </summary>
        public static PlanetarySchematic GetSchematicByOutputType(int outputTypeId)
        {
            EnsureLoaded();
            s_schematicsByOutputType.TryGetValue(outputTypeId, out var schematic);
            return schematic;
        }

        /// <summary>
        /// Gets all schematics of a specific production tier.
        /// </summary>
        public static IEnumerable<PlanetarySchematic> GetSchematicsByTier(ProductionTier tier)
        {
            EnsureLoaded();
            return s_schematics.Values.Where(s => s.Tier == tier);
        }

        /// <summary>
        /// Gets all loaded schematics.
        /// </summary>
        public static IReadOnlyDictionary<int, PlanetarySchematic> All
        {
            get
            {
                EnsureLoaded();
                return s_schematics;
            }
        }

        /// <summary>
        /// Determines the production tier from schematic characteristics.
        /// </summary>
        internal static ProductionTier InferTier(int cycleTime, int totalInputQuantity, int outputQuantity)
        {
            if (cycleTime == 1800 && totalInputQuantity == 3000 && outputQuantity == 20)
                return ProductionTier.Basic;

            if (outputQuantity == 5)
                return ProductionTier.Advanced;

            if (outputQuantity == 3)
                return ProductionTier.AdvancedP3;

            if (outputQuantity == 1)
                return ProductionTier.HighTech;

            // Fallback based on cycle time and quantities
            if (cycleTime == 1800)
                return ProductionTier.Basic;

            return totalInputQuantity <= 20 ? ProductionTier.AdvancedP3 : ProductionTier.Advanced;
        }

        private static void EnsureLoaded()
        {
            if (s_loaded) return;
            s_loaded = true;
            RegisterAll();
        }

        private static void Register(int id, string name, int cycleTime, SchematicMaterial[] inputs, SchematicMaterial output, ProductionTier tier)
        {
            var schematic = new PlanetarySchematic(id, name, cycleTime, inputs, output, tier);
            s_schematics[id] = schematic;
            s_schematicsByOutputType[output.TypeID] = schematic;
        }

        private static void RegisterAll()
        {
            // ═══════════════════════════════════════════════════════════════
            // P0 → P1: Basic Industry Facility (1800s cycle, 3000 in → 20 out)
            // ═══════════════════════════════════════════════════════════════
            Register(121, "Water", 1800,
                new[] { new SchematicMaterial(2268, "Aqueous Liquids", 3000) },
                new SchematicMaterial(3645, "Water", 20), ProductionTier.Basic);

            Register(122, "Plasmoids", 1800,
                new[] { new SchematicMaterial(2308, "Suspended Plasma", 3000) },
                new SchematicMaterial(2389, "Plasmoids", 20), ProductionTier.Basic);

            Register(123, "Electrolytes", 1800,
                new[] { new SchematicMaterial(2309, "Ionic Solutions", 3000) },
                new SchematicMaterial(2390, "Electrolytes", 20), ProductionTier.Basic);

            Register(124, "Oxygen", 1800,
                new[] { new SchematicMaterial(2310, "Noble Gas", 3000) },
                new SchematicMaterial(3683, "Oxygen", 20), ProductionTier.Basic);

            Register(125, "Oxidizing Compound", 1800,
                new[] { new SchematicMaterial(2311, "Reactive Gas", 3000) },
                new SchematicMaterial(2392, "Oxidizing Compound", 20), ProductionTier.Basic);

            Register(126, "Reactive Metals", 1800,
                new[] { new SchematicMaterial(2267, "Base Metals", 3000) },
                new SchematicMaterial(2398, "Reactive Metals", 20), ProductionTier.Basic);

            Register(127, "Precious Metals", 1800,
                new[] { new SchematicMaterial(2270, "Noble Metals", 3000) },
                new SchematicMaterial(2399, "Precious Metals", 20), ProductionTier.Basic);

            Register(128, "Toxic Metals", 1800,
                new[] { new SchematicMaterial(2272, "Heavy Metals", 3000) },
                new SchematicMaterial(2400, "Toxic Metals", 20), ProductionTier.Basic);

            Register(129, "Chiral Structures", 1800,
                new[] { new SchematicMaterial(2306, "Non-CS Crystals", 3000) },
                new SchematicMaterial(2401, "Chiral Structures", 20), ProductionTier.Basic);

            Register(130, "Silicon", 1800,
                new[] { new SchematicMaterial(2307, "Felsic Magma", 3000) },
                new SchematicMaterial(9828, "Silicon", 20), ProductionTier.Basic);

            Register(131, "Bacteria", 1800,
                new[] { new SchematicMaterial(2073, "Micro Organisms", 3000) },
                new SchematicMaterial(2393, "Bacteria", 20), ProductionTier.Basic);

            Register(132, "Biomass", 1800,
                new[] { new SchematicMaterial(2286, "Planktic Colonies", 3000) },
                new SchematicMaterial(3779, "Biomass", 20), ProductionTier.Basic);

            Register(133, "Proteins", 1800,
                new[] { new SchematicMaterial(2287, "Complex Organisms", 3000) },
                new SchematicMaterial(2395, "Proteins", 20), ProductionTier.Basic);

            Register(134, "Biofuels", 1800,
                new[] { new SchematicMaterial(2288, "Carbon Compounds", 3000) },
                new SchematicMaterial(2396, "Biofuels", 20), ProductionTier.Basic);

            Register(135, "Industrial Fibers", 1800,
                new[] { new SchematicMaterial(2305, "Autotrophs", 3000) },
                new SchematicMaterial(2397, "Industrial Fibers", 20), ProductionTier.Basic);

            // ═══════════════════════════════════════════════════════════════
            // P1 → P2: Advanced Industry Facility (3600s cycle, 40+40 in → 5 out)
            // ═══════════════════════════════════════════════════════════════
            Register(65, "Superconductors", 3600,
                new[] { new SchematicMaterial(2389, "Plasmoids", 40), new SchematicMaterial(3645, "Water", 40) },
                new SchematicMaterial(9838, "Superconductors", 5), ProductionTier.Advanced);

            Register(66, "Coolant", 3600,
                new[] { new SchematicMaterial(2390, "Electrolytes", 40), new SchematicMaterial(3645, "Water", 40) },
                new SchematicMaterial(9832, "Coolant", 5), ProductionTier.Advanced);

            Register(67, "Rocket Fuel", 3600,
                new[] { new SchematicMaterial(2389, "Plasmoids", 40), new SchematicMaterial(2390, "Electrolytes", 40) },
                new SchematicMaterial(9830, "Rocket Fuel", 5), ProductionTier.Advanced);

            Register(68, "Synthetic Oil", 3600,
                new[] { new SchematicMaterial(2390, "Electrolytes", 40), new SchematicMaterial(3683, "Oxygen", 40) },
                new SchematicMaterial(3691, "Synthetic Oil", 5), ProductionTier.Advanced);

            Register(69, "Oxides", 3600,
                new[] { new SchematicMaterial(2392, "Oxidizing Compound", 40), new SchematicMaterial(3683, "Oxygen", 40) },
                new SchematicMaterial(2317, "Oxides", 5), ProductionTier.Advanced);

            Register(70, "Silicate Glass", 3600,
                new[] { new SchematicMaterial(2392, "Oxidizing Compound", 40), new SchematicMaterial(9828, "Silicon", 40) },
                new SchematicMaterial(3697, "Silicate Glass", 5), ProductionTier.Advanced);

            Register(71, "Transmitter", 3600,
                new[] { new SchematicMaterial(2389, "Plasmoids", 40), new SchematicMaterial(2401, "Chiral Structures", 40) },
                new SchematicMaterial(9840, "Transmitter", 5), ProductionTier.Advanced);

            Register(72, "Water-Cooled CPU", 3600,
                new[] { new SchematicMaterial(2398, "Reactive Metals", 40), new SchematicMaterial(3645, "Water", 40) },
                new SchematicMaterial(2328, "Water-Cooled CPU", 5), ProductionTier.Advanced);

            Register(73, "Mechanical Parts", 3600,
                new[] { new SchematicMaterial(2398, "Reactive Metals", 40), new SchematicMaterial(2399, "Precious Metals", 40) },
                new SchematicMaterial(3689, "Mechanical Parts", 5), ProductionTier.Advanced);

            Register(74, "Construction Blocks", 3600,
                new[] { new SchematicMaterial(2398, "Reactive Metals", 40), new SchematicMaterial(2400, "Toxic Metals", 40) },
                new SchematicMaterial(3828, "Construction Blocks", 5), ProductionTier.Advanced);

            Register(75, "Enriched Uranium", 3600,
                new[] { new SchematicMaterial(2399, "Precious Metals", 40), new SchematicMaterial(2400, "Toxic Metals", 40) },
                new SchematicMaterial(44, "Enriched Uranium", 5), ProductionTier.Advanced);

            Register(76, "Consumer Electronics", 3600,
                new[] { new SchematicMaterial(2400, "Toxic Metals", 40), new SchematicMaterial(2401, "Chiral Structures", 40) },
                new SchematicMaterial(9836, "Consumer Electronics", 5), ProductionTier.Advanced);

            Register(77, "Miniature Electronics", 3600,
                new[] { new SchematicMaterial(2401, "Chiral Structures", 40), new SchematicMaterial(9828, "Silicon", 40) },
                new SchematicMaterial(9842, "Miniature Electronics", 5), ProductionTier.Advanced);

            Register(78, "Nanites", 3600,
                new[] { new SchematicMaterial(2393, "Bacteria", 40), new SchematicMaterial(2398, "Reactive Metals", 40) },
                new SchematicMaterial(2463, "Nanites", 5), ProductionTier.Advanced);

            Register(79, "Biocells", 3600,
                new[] { new SchematicMaterial(2396, "Biofuels", 40), new SchematicMaterial(2399, "Precious Metals", 40) },
                new SchematicMaterial(2329, "Biocells", 5), ProductionTier.Advanced);

            Register(80, "Microfiber Shielding", 3600,
                new[] { new SchematicMaterial(2397, "Industrial Fibers", 40), new SchematicMaterial(9828, "Silicon", 40) },
                new SchematicMaterial(2327, "Microfiber Shielding", 5), ProductionTier.Advanced);

            Register(81, "Viral Agent", 3600,
                new[] { new SchematicMaterial(2393, "Bacteria", 40), new SchematicMaterial(3779, "Biomass", 40) },
                new SchematicMaterial(3775, "Viral Agent", 5), ProductionTier.Advanced);

            Register(82, "Fertilizer", 3600,
                new[] { new SchematicMaterial(2393, "Bacteria", 40), new SchematicMaterial(2395, "Proteins", 40) },
                new SchematicMaterial(3693, "Fertilizer", 5), ProductionTier.Advanced);

            Register(83, "Genetically Enhanced Livestock", 3600,
                new[] { new SchematicMaterial(2395, "Proteins", 40), new SchematicMaterial(3779, "Biomass", 40) },
                new SchematicMaterial(15317, "Genetically Enhanced Livestock", 5), ProductionTier.Advanced);

            Register(84, "Livestock", 3600,
                new[] { new SchematicMaterial(2395, "Proteins", 40), new SchematicMaterial(2396, "Biofuels", 40) },
                new SchematicMaterial(3725, "Livestock", 5), ProductionTier.Advanced);

            Register(85, "Polytextiles", 3600,
                new[] { new SchematicMaterial(2396, "Biofuels", 40), new SchematicMaterial(2397, "Industrial Fibers", 40) },
                new SchematicMaterial(3695, "Polytextiles", 5), ProductionTier.Advanced);

            Register(86, "Test Cultures", 3600,
                new[] { new SchematicMaterial(2393, "Bacteria", 40), new SchematicMaterial(3645, "Water", 40) },
                new SchematicMaterial(2319, "Test Cultures", 5), ProductionTier.Advanced);

            Register(87, "Supertensile Plastics", 3600,
                new[] { new SchematicMaterial(3683, "Oxygen", 40), new SchematicMaterial(3779, "Biomass", 40) },
                new SchematicMaterial(2312, "Supertensile Plastics", 5), ProductionTier.Advanced);

            Register(88, "Polyaramids", 3600,
                new[] { new SchematicMaterial(2392, "Oxidizing Compound", 40), new SchematicMaterial(2397, "Industrial Fibers", 40) },
                new SchematicMaterial(2321, "Polyaramids", 5), ProductionTier.Advanced);

            // ═══════════════════════════════════════════════════════════════
            // P2 → P3: Advanced Industry Facility (3600s cycle, 10+10(+10) in → 3 out)
            // ═══════════════════════════════════════════════════════════════
            Register(89, "Ukomi Superconductor", 3600,
                new[] { new SchematicMaterial(3691, "Synthetic Oil", 10), new SchematicMaterial(9838, "Superconductors", 10) },
                new SchematicMaterial(17136, "Ukomi Superconductor", 3), ProductionTier.AdvancedP3);

            Register(90, "Condensates", 3600,
                new[] { new SchematicMaterial(2317, "Oxides", 10), new SchematicMaterial(9832, "Coolant", 10) },
                new SchematicMaterial(2344, "Condensates", 3), ProductionTier.AdvancedP3);

            Register(91, "Camera Drones", 3600,
                new[] { new SchematicMaterial(3697, "Silicate Glass", 10), new SchematicMaterial(9830, "Rocket Fuel", 10) },
                new SchematicMaterial(2345, "Camera Drones", 3), ProductionTier.AdvancedP3);

            Register(92, "Synthetic Synapses", 3600,
                new[] { new SchematicMaterial(2312, "Supertensile Plastics", 10), new SchematicMaterial(2319, "Test Cultures", 10) },
                new SchematicMaterial(2346, "Synthetic Synapses", 3), ProductionTier.AdvancedP3);

            Register(94, "High-Tech Transmitter", 3600,
                new[] { new SchematicMaterial(2321, "Polyaramids", 10), new SchematicMaterial(9840, "Transmitter", 10) },
                new SchematicMaterial(17898, "High-Tech Transmitter", 3), ProductionTier.AdvancedP3);

            Register(95, "Gel-Matrix Biopaste", 3600,
                new[] { new SchematicMaterial(2317, "Oxides", 10), new SchematicMaterial(2329, "Biocells", 10), new SchematicMaterial(9838, "Superconductors", 10) },
                new SchematicMaterial(2348, "Gel-Matrix Biopaste", 3), ProductionTier.AdvancedP3);

            Register(96, "Supercomputers", 3600,
                new[] { new SchematicMaterial(2328, "Water-Cooled CPU", 10), new SchematicMaterial(9832, "Coolant", 10), new SchematicMaterial(9836, "Consumer Electronics", 10) },
                new SchematicMaterial(2349, "Supercomputers", 3), ProductionTier.AdvancedP3);

            Register(97, "Robotics", 3600,
                new[] { new SchematicMaterial(3689, "Mechanical Parts", 10), new SchematicMaterial(9836, "Consumer Electronics", 10) },
                new SchematicMaterial(9848, "Robotics", 3), ProductionTier.AdvancedP3);

            Register(98, "Smartfab Units", 3600,
                new[] { new SchematicMaterial(3828, "Construction Blocks", 10), new SchematicMaterial(9842, "Miniature Electronics", 10) },
                new SchematicMaterial(2351, "Smartfab Units", 3), ProductionTier.AdvancedP3);

            Register(99, "Nuclear Reactors", 3600,
                new[] { new SchematicMaterial(44, "Enriched Uranium", 10), new SchematicMaterial(2327, "Microfiber Shielding", 10) },
                new SchematicMaterial(2352, "Nuclear Reactors", 3), ProductionTier.AdvancedP3);

            Register(100, "Guidance Systems", 3600,
                new[] { new SchematicMaterial(2328, "Water-Cooled CPU", 10), new SchematicMaterial(9840, "Transmitter", 10) },
                new SchematicMaterial(9834, "Guidance Systems", 3), ProductionTier.AdvancedP3);

            Register(102, "Neocoms", 3600,
                new[] { new SchematicMaterial(2329, "Biocells", 10), new SchematicMaterial(3697, "Silicate Glass", 10) },
                new SchematicMaterial(2354, "Neocoms", 3), ProductionTier.AdvancedP3);

            Register(103, "Planetary Vehicles", 3600,
                new[] { new SchematicMaterial(2312, "Supertensile Plastics", 10), new SchematicMaterial(3689, "Mechanical Parts", 10), new SchematicMaterial(9842, "Miniature Electronics", 10) },
                new SchematicMaterial(9846, "Planetary Vehicles", 3), ProductionTier.AdvancedP3);

            Register(104, "Biotech Research Reports", 3600,
                new[] { new SchematicMaterial(2463, "Nanites", 10), new SchematicMaterial(3725, "Livestock", 10), new SchematicMaterial(3828, "Construction Blocks", 10) },
                new SchematicMaterial(2358, "Biotech Research Reports", 3), ProductionTier.AdvancedP3);

            Register(105, "Vaccines", 3600,
                new[] { new SchematicMaterial(3725, "Livestock", 10), new SchematicMaterial(3775, "Viral Agent", 10) },
                new SchematicMaterial(28974, "Vaccines", 3), ProductionTier.AdvancedP3);

            Register(106, "Industrial Explosives", 3600,
                new[] { new SchematicMaterial(3693, "Fertilizer", 10), new SchematicMaterial(3695, "Polytextiles", 10) },
                new SchematicMaterial(2360, "Industrial Explosives", 3), ProductionTier.AdvancedP3);

            Register(107, "Hermetic Membranes", 3600,
                new[] { new SchematicMaterial(2321, "Polyaramids", 10), new SchematicMaterial(15317, "Genetically Enhanced Livestock", 10) },
                new SchematicMaterial(2361, "Hermetic Membranes", 3), ProductionTier.AdvancedP3);

            Register(108, "Transcranial Microcontroller", 3600,
                new[] { new SchematicMaterial(2329, "Biocells", 10), new SchematicMaterial(2463, "Nanites", 10) },
                new SchematicMaterial(12836, "Transcranial Microcontroller", 3), ProductionTier.AdvancedP3);

            Register(109, "Data Chips", 3600,
                new[] { new SchematicMaterial(2312, "Supertensile Plastics", 10), new SchematicMaterial(2327, "Microfiber Shielding", 10) },
                new SchematicMaterial(17392, "Data Chips", 3), ProductionTier.AdvancedP3);

            Register(110, "Hazmat Detection Systems", 3600,
                new[] { new SchematicMaterial(3695, "Polytextiles", 10), new SchematicMaterial(3775, "Viral Agent", 10), new SchematicMaterial(9840, "Transmitter", 10) },
                new SchematicMaterial(2366, "Hazmat Detection Systems", 3), ProductionTier.AdvancedP3);

            Register(111, "Cryoprotectant Solution", 3600,
                new[] { new SchematicMaterial(2319, "Test Cultures", 10), new SchematicMaterial(3691, "Synthetic Oil", 10), new SchematicMaterial(3693, "Fertilizer", 10) },
                new SchematicMaterial(2367, "Cryoprotectant Solution", 3), ProductionTier.AdvancedP3);

            // ═══════════════════════════════════════════════════════════════
            // P3 → P4: High-Tech Production Plant (3600s cycle, 6+6+6(+40) in → 1 out)
            // ═══════════════════════════════════════════════════════════════
            Register(112, "Organic Mortar Applicators", 3600,
                new[] { new SchematicMaterial(2344, "Condensates", 6), new SchematicMaterial(2393, "Bacteria", 40), new SchematicMaterial(9848, "Robotics", 6) },
                new SchematicMaterial(2870, "Organic Mortar Applicators", 1), ProductionTier.HighTech);

            Register(113, "Sterile Conduits", 3600,
                new[] { new SchematicMaterial(2351, "Smartfab Units", 6), new SchematicMaterial(3645, "Water", 40), new SchematicMaterial(28974, "Vaccines", 6) },
                new SchematicMaterial(2875, "Sterile Conduits", 1), ProductionTier.HighTech);

            Register(114, "Nano-Factory", 3600,
                new[] { new SchematicMaterial(2360, "Industrial Explosives", 6), new SchematicMaterial(2398, "Reactive Metals", 40), new SchematicMaterial(17136, "Ukomi Superconductor", 6) },
                new SchematicMaterial(2869, "Nano-Factory", 1), ProductionTier.HighTech);

            Register(115, "Self-Harmonizing Power Core", 3600,
                new[] { new SchematicMaterial(2345, "Camera Drones", 6), new SchematicMaterial(2352, "Nuclear Reactors", 6), new SchematicMaterial(2361, "Hermetic Membranes", 6) },
                new SchematicMaterial(2872, "Self-Harmonizing Power Core", 1), ProductionTier.HighTech);

            Register(116, "Recursive Computing Module", 3600,
                new[] { new SchematicMaterial(2346, "Synthetic Synapses", 6), new SchematicMaterial(9834, "Guidance Systems", 6), new SchematicMaterial(12836, "Transcranial Microcontroller", 6) },
                new SchematicMaterial(2871, "Recursive Computing Module", 1), ProductionTier.HighTech);

            Register(117, "Broadcast Node", 3600,
                new[] { new SchematicMaterial(2354, "Neocoms", 6), new SchematicMaterial(17392, "Data Chips", 6), new SchematicMaterial(17898, "High-Tech Transmitter", 6) },
                new SchematicMaterial(2867, "Broadcast Node", 1), ProductionTier.HighTech);

            Register(118, "Integrity Response Drones", 3600,
                new[] { new SchematicMaterial(2348, "Gel-Matrix Biopaste", 6), new SchematicMaterial(2366, "Hazmat Detection Systems", 6), new SchematicMaterial(9846, "Planetary Vehicles", 6) },
                new SchematicMaterial(2868, "Integrity Response Drones", 1), ProductionTier.HighTech);

            Register(119, "Wetware Mainframe", 3600,
                new[] { new SchematicMaterial(2349, "Supercomputers", 6), new SchematicMaterial(2358, "Biotech Research Reports", 6), new SchematicMaterial(2367, "Cryoprotectant Solution", 6) },
                new SchematicMaterial(2876, "Wetware Mainframe", 1), ProductionTier.HighTech);
        }
    }
}
