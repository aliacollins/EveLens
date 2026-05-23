// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.Linq;
using EveLens.Common.Services.Planetary;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Services.Planetary
{
    public class PlanetarySchematicsProviderTests
    {
        // ═══════════════════════════════════════════════════════════
        // Data Integrity (all 68 schematics loaded correctly)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void All_Contains68Schematics()
        {
            PlanetarySchematicsProvider.All.Should().HaveCount(68);
        }

        [Fact]
        public void BasicTier_Has15Schematics()
        {
            PlanetarySchematicsProvider.GetSchematicsByTier(ProductionTier.Basic)
                .Should().HaveCount(15, "there are 15 P0→P1 schematics in EVE");
        }

        [Fact]
        public void AdvancedTier_Has24Schematics()
        {
            PlanetarySchematicsProvider.GetSchematicsByTier(ProductionTier.Advanced)
                .Should().HaveCount(24, "there are 24 P1→P2 schematics in EVE");
        }

        [Fact]
        public void AdvancedP3Tier_Has21Schematics()
        {
            PlanetarySchematicsProvider.GetSchematicsByTier(ProductionTier.AdvancedP3)
                .Should().HaveCount(21, "there are 21 P2→P3 schematics in EVE");
        }

        [Fact]
        public void HighTechTier_Has8Schematics()
        {
            PlanetarySchematicsProvider.GetSchematicsByTier(ProductionTier.HighTech)
                .Should().HaveCount(8, "there are 8 P3→P4 schematics in EVE");
        }

        // ═══════════════════════════════════════════════════════════
        // Specific schematic validation (cross-reference with SDE)
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void Schematic121_Water_CorrectData()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(121);
            s.Should().NotBeNull();
            s.Name.Should().Be("Water");
            s.CycleTimeSeconds.Should().Be(1800);
            s.Tier.Should().Be(ProductionTier.Basic);
            s.Inputs.Should().HaveCount(1);
            s.Inputs[0].TypeID.Should().Be(2268);  // Aqueous Liquids
            s.Inputs[0].Quantity.Should().Be(3000);
            s.Output.TypeID.Should().Be(3645);     // Water
            s.Output.Quantity.Should().Be(20);
        }

        [Fact]
        public void Schematic65_Superconductors_CorrectData()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(65);
            s.Should().NotBeNull();
            s.Name.Should().Be("Superconductors");
            s.CycleTimeSeconds.Should().Be(3600);
            s.Tier.Should().Be(ProductionTier.Advanced);
            s.Inputs.Should().HaveCount(2);
            s.Inputs.Should().Contain(m => m.TypeID == 2389 && m.Quantity == 40); // Plasmoids
            s.Inputs.Should().Contain(m => m.TypeID == 3645 && m.Quantity == 40); // Water
            s.Output.TypeID.Should().Be(9838);    // Superconductors
            s.Output.Quantity.Should().Be(5);
        }

        [Fact]
        public void Schematic89_UkomiSuperconductor_P3Tier()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(89);
            s.Should().NotBeNull();
            s.Name.Should().Be("Ukomi Superconductor");
            s.Tier.Should().Be(ProductionTier.AdvancedP3);
            s.Inputs.Should().HaveCount(2);
            s.Inputs.Should().Contain(m => m.TypeID == 3691 && m.Quantity == 10); // Synthetic Oil
            s.Inputs.Should().Contain(m => m.TypeID == 9838 && m.Quantity == 10); // Superconductors
            s.Output.TypeID.Should().Be(17136);  // Ukomi Superconductor
            s.Output.Quantity.Should().Be(3);
        }

        [Fact]
        public void Schematic119_WetwareMainframe_P4Tier()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(119);
            s.Should().NotBeNull();
            s.Name.Should().Be("Wetware Mainframe");
            s.Tier.Should().Be(ProductionTier.HighTech);
            s.Inputs.Should().HaveCount(3);
            s.Output.TypeID.Should().Be(2876);  // Wetware Mainframe
            s.Output.Quantity.Should().Be(1);
        }

        // ═══════════════════════════════════════════════════════════
        // Lookup methods
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void GetSchematicByOutputType_FindsCorrectSchematic()
        {
            // Water (3645) is produced by schematic 121
            var s = PlanetarySchematicsProvider.GetSchematicByOutputType(3645);
            s.Should().NotBeNull();
            s.SchematicID.Should().Be(121);
            s.Name.Should().Be("Water");
        }

        [Fact]
        public void GetSchematicByOutputType_UnknownType_ReturnsNull()
        {
            PlanetarySchematicsProvider.GetSchematicByOutputType(99999)
                .Should().BeNull();
        }

        [Fact]
        public void GetSchematic_UnknownId_ReturnsNull()
        {
            PlanetarySchematicsProvider.GetSchematic(999).Should().BeNull();
        }

        // ═══════════════════════════════════════════════════════════
        // Production rate calculations
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void BasicFactory_OutputPerHour_Is40()
        {
            // P0→P1: 1800s cycle, 20 per cycle = 2 cycles/hr * 20 = 40/hr
            var s = PlanetarySchematicsProvider.GetSchematic(121); // Water
            s.GetOutputPerHour().Should().Be(40);
        }

        [Fact]
        public void BasicFactory_InputDemandPerHour_Is6000()
        {
            // P0→P1: 1800s cycle, 3000 per cycle = 2 cycles/hr * 3000 = 6000/hr
            var s = PlanetarySchematicsProvider.GetSchematic(121); // Water
            s.GetInputDemandPerHour(2268).Should().Be(6000); // Aqueous Liquids
        }

        [Fact]
        public void AdvancedFactory_OutputPerHour_Is5()
        {
            // P1→P2: 3600s cycle, 5 per cycle = 1 cycle/hr * 5 = 5/hr
            var s = PlanetarySchematicsProvider.GetSchematic(65); // Superconductors
            s.GetOutputPerHour().Should().Be(5);
        }

        [Fact]
        public void AdvancedFactory_InputDemandPerHour_Is40()
        {
            // P1→P2: 3600s cycle, 40 per cycle = 1 cycle/hr * 40 = 40/hr
            var s = PlanetarySchematicsProvider.GetSchematic(65); // Superconductors
            s.GetInputDemandPerHour(2389).Should().Be(40); // Plasmoids
            s.GetInputDemandPerHour(3645).Should().Be(40); // Water
        }

        [Fact]
        public void P3Factory_OutputPerHour_Is3()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(89); // Ukomi Superconductor
            s.GetOutputPerHour().Should().Be(3);
        }

        [Fact]
        public void P4Factory_OutputPerHour_Is1()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(119); // Wetware Mainframe
            s.GetOutputPerHour().Should().Be(1);
        }

        [Fact]
        public void GetInputDemandPerHour_UnknownTypeId_ReturnsZero()
        {
            var s = PlanetarySchematicsProvider.GetSchematic(121);
            s.GetInputDemandPerHour(99999).Should().Be(0);
        }

        // ═══════════════════════════════════════════════════════════
        // Production chain consistency
        // ═══════════════════════════════════════════════════════════

        [Fact]
        public void AllBasicSchematics_Have1800CycleTime()
        {
            foreach (var s in PlanetarySchematicsProvider.GetSchematicsByTier(ProductionTier.Basic))
            {
                s.CycleTimeSeconds.Should().Be(1800, $"Basic schematic {s.Name} should have 30min cycle");
            }
        }

        [Fact]
        public void AllAdvancedAndAbove_Have3600CycleTime()
        {
            foreach (var s in PlanetarySchematicsProvider.All.Values.Where(x => x.Tier != ProductionTier.Basic))
            {
                s.CycleTimeSeconds.Should().Be(3600, $"Schematic {s.Name} ({s.Tier}) should have 1hr cycle");
            }
        }

        [Fact]
        public void AllSchematics_HaveAtLeastOneInput()
        {
            foreach (var s in PlanetarySchematicsProvider.All.Values)
            {
                s.Inputs.Should().NotBeEmpty($"Schematic {s.Name} must have inputs");
            }
        }

        [Fact]
        public void AllSchematics_HavePositiveOutputQuantity()
        {
            foreach (var s in PlanetarySchematicsProvider.All.Values)
            {
                s.Output.Quantity.Should().BeGreaterThan(0, $"Schematic {s.Name} must produce something");
            }
        }

        [Fact]
        public void EachOutputType_IsUnique()
        {
            var outputTypes = PlanetarySchematicsProvider.All.Values
                .Select(s => s.Output.TypeID)
                .ToList();

            outputTypes.Should().OnlyHaveUniqueItems("each item is produced by exactly one schematic");
        }
    }
}
