// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml;
using EveLens.Common;
using EveLens.Common.Enumerations;
using EveLens.Common.Helpers;
using EveLens.Common.Serialization.Exportation;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Core.Enumerations;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// Tests for PlanIOHelper import/export — covers the #51 fix (plan import was
    /// creating empty plans) and format detection for .emp files.
    /// </summary>
    [Collection("AppServices")]
    public class PlanIOHelperImportTests : IDisposable
    {
        private readonly string _tempDir;

        public PlanIOHelperImportTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "evelens-planioimport-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Mock DialogService so import error paths don't throw
            var dialogService = Substitute.For<IDialogService>();
            dialogService.ShowMessage(Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<DialogButtons>(), Arg.Any<DialogIcon>())
                .Returns(DialogChoice.OK);
            AppServices.SetDialogService(dialogService);
        }

        public void Dispose()
        {
            AppServices.Reset();

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch { }
        }

        #region Helpers

        /// <summary>
        /// Builds an OutputPlan with the specified number of skill entries.
        /// Each entry gets a unique skill ID, name, and level (cycling 1-5).
        /// </summary>
        private static OutputPlan BuildOutputPlan(string name, int entryCount, int revision = 5)
        {
            var plan = new OutputPlan
            {
                Name = name,
                Owner = Guid.NewGuid(),
                Revision = revision
            };

            for (int i = 0; i < entryCount; i++)
            {
                plan.Entries.Add(new SerializablePlanEntry
                {
                    ID = 3300 + i,
                    SkillName = $"TestSkill{i}",
                    Level = (i % 5) + 1,
                    Priority = 3,
                    Type = PlanEntryType.Planned
                });
            }

            return plan;
        }

        /// <summary>
        /// Serializes an OutputPlan to XML and writes it to a file.
        /// Returns the file path.
        /// </summary>
        private string WriteOutputPlanToFile(OutputPlan plan, string filename)
        {
            var doc = (XmlDocument)Util.SerializeToXmlDocument(plan);
            string xml = Util.GetXmlStringRepresentation(doc);
            string path = Path.Combine(_tempDir, filename);
            File.WriteAllText(path, xml, Encoding.UTF8);
            return path;
        }

        /// <summary>
        /// Serializes an OutputPlan to XML, gzip-compresses it, and writes to a file.
        /// Returns the file path.
        /// </summary>
        private string WriteGzippedOutputPlanToFile(OutputPlan plan, string filename)
        {
            var doc = (XmlDocument)Util.SerializeToXmlDocument(plan);
            string xml = Util.GetXmlStringRepresentation(doc);
            byte[] xmlBytes = Encoding.UTF8.GetBytes(xml);

            string path = Path.Combine(_tempDir, filename);
            using (var fileStream = File.Create(path))
            using (var gzipStream = new GZipStream(fileStream, CompressionMode.Compress))
            {
                gzipStream.Write(xmlBytes, 0, xmlBytes.Length);
            }
            return path;
        }

        #endregion

        // =====================================================================
        // SCENARIO 1: XML round-trip — the exact bug TinkeringGoblin hit (#51)
        // =====================================================================

        #region Round-Trip Tests

        [Fact]
        public void ImportFromXML_RoundTrip_PreservesAllEntries()
        {
            // Arrange — 510 skills, matching the reporter's scenario
            var original = BuildOutputPlan("Baseline Skills", 510);
            string path = WriteOutputPlanToFile(original, "baseline.xml");

            // Act
            var imported = PlanIOHelper.ImportFromXML(path);

            // Assert
            imported.Should().NotBeNull("import should succeed for valid XML");
            imported!.Entries.Should().HaveCount(510,
                "all 510 skill entries must survive the round-trip — this was the #51 bug");
        }

        [Fact]
        public void ImportFromXML_RoundTrip_PreservesPlanName()
        {
            var original = BuildOutputPlan("Cruiser Training V", 5);
            string path = WriteOutputPlanToFile(original, "named.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull();
            imported!.Name.Should().Be("Cruiser Training V");
        }

        [Fact]
        public void ImportFromXML_RoundTrip_PreservesEntryDetails()
        {
            var original = BuildOutputPlan("Detail Check", 3);
            // Set distinctive values on the first entry
            original.Entries[0].ID = 11584;
            original.Entries[0].SkillName = "Caldari Frigate";
            original.Entries[0].Level = 4;
            original.Entries[0].Priority = 1;
            original.Entries[0].Type = PlanEntryType.Prerequisite;
            original.Entries[0].Notes = "Need for Buzzard";

            string path = WriteOutputPlanToFile(original, "details.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull();
            var entry = imported!.Entries[0];
            entry.ID.Should().Be(11584);
            entry.SkillName.Should().Be("Caldari Frigate");
            entry.Level.Should().Be(4);
            entry.Priority.Should().Be(1);
            entry.Type.Should().Be(PlanEntryType.Prerequisite);
            entry.Notes.Should().Be("Need for Buzzard");
        }

        [Fact]
        public void ImportFromXML_RoundTrip_PreservesOwnerGuid()
        {
            var owner = Guid.NewGuid();
            var original = BuildOutputPlan("GUID Test", 1);
            original.Owner = owner;
            string path = WriteOutputPlanToFile(original, "guid.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull();
            imported!.Owner.Should().Be(owner);
        }

        [Fact]
        public void ImportFromXML_RoundTrip_EmptyPlan_PreservesName()
        {
            // A plan with zero entries is valid — should import cleanly
            var original = BuildOutputPlan("Empty Plan", 0);
            string path = WriteOutputPlanToFile(original, "empty-plan.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull();
            imported!.Name.Should().Be("Empty Plan");
            imported.Entries.Should().BeEmpty();
        }

        #endregion

        // =====================================================================
        // SCENARIO 2: .emp format detection (plain XML vs gzip)
        // =====================================================================

        #region .emp Format Detection

        [Fact]
        public void ImportFromXML_PlainXmlEmp_ImportsSuccessfully()
        {
            // EveLens exports .emp files as plain XML (not gzipped)
            var original = BuildOutputPlan("Plain EMP", 25);
            string path = WriteOutputPlanToFile(original, "plan.emp");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull("plain XML .emp files should be importable");
            imported!.Entries.Should().HaveCount(25);
            imported.Name.Should().Be("Plain EMP");
        }

        [Fact]
        public void ImportFromXML_GzipEmp_ImportsSuccessfully()
        {
            // Legacy EVEMon exported .emp files as gzip-compressed XML
            var original = BuildOutputPlan("Legacy Gzip", 50);
            string path = WriteGzippedOutputPlanToFile(original, "legacy.emp");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull("gzip-compressed .emp files should be importable");
            imported!.Entries.Should().HaveCount(50);
            imported.Name.Should().Be("Legacy Gzip");
        }

        [Fact]
        public void ImportFromXML_PlainXmlEmp_LargeEntryCount_Survives()
        {
            // Stress test with many entries — TinkeringGoblin had 510
            var original = BuildOutputPlan("Big Plan", 600);
            string path = WriteOutputPlanToFile(original, "big.emp");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull();
            imported!.Entries.Should().HaveCount(600);
        }

        #endregion

        // =====================================================================
        // SCENARIO 3: Edge cases — BOM, empty file, corrupt gzip, no revision
        // =====================================================================

        #region Edge Cases

        [Fact]
        public void ImportFromXML_BomPrefixedXml_ImportsSuccessfully()
        {
            // Some editors save XML with a UTF-8 BOM (EF BB BF).
            // The <?xml sniff reads chars, so the BOM is transparent to StreamReader.
            var original = BuildOutputPlan("BOM Plan", 10);
            var doc = (XmlDocument)Util.SerializeToXmlDocument(original);
            string xml = Util.GetXmlStringRepresentation(doc);

            string path = Path.Combine(_tempDir, "bom.emp");
            // Write with UTF-8 BOM explicitly
            File.WriteAllText(path, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull("BOM-prefixed XML should import correctly");
            imported!.Entries.Should().HaveCount(10);
        }

        [Fact]
        public void ImportFromXML_EmptyFile_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "empty.xml");
            File.WriteAllText(path, string.Empty);

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().BeNull("an empty file has no revision attribute and no valid XML");
        }

        [Fact]
        public void ImportFromXML_EmptyEmpFile_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "empty.emp");
            File.WriteAllText(path, string.Empty);

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().BeNull("an empty .emp file cannot be parsed");
        }

        [Fact]
        public void ImportFromXML_CorruptGzipEmp_ReturnsNull()
        {
            // Write random bytes that aren't valid gzip
            string path = Path.Combine(_tempDir, "corrupt.emp");
            File.WriteAllBytes(path, new byte[] { 0x00, 0x01, 0x02, 0x03, 0xFF, 0xFE });

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().BeNull("corrupt gzip data should fail gracefully");
        }

        [Fact]
        public void ImportFromXML_MissingRevisionAttribute_ReturnsNull()
        {
            // XML that is well-formed but has no revision attribute (pre-1.3.0 format)
            string xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<plan name=""Old Plan"">
  <entry skillID=""3300"" skill=""TestSkill"" level=""1"" priority=""3"" type=""Planned"" />
</plan>";

            string path = Path.Combine(_tempDir, "norevision.xml");
            File.WriteAllText(path, xml);

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().BeNull(
                "files without a revision attribute are pre-1.3.0 and not supported");
        }

        [Fact]
        public void ImportFromXML_RevisionZero_ImportsSuccessfully()
        {
            // revision="0" is valid — distinct from missing revision (which returns -1)
            var original = BuildOutputPlan("Rev Zero", 5, revision: 0);
            string path = WriteOutputPlanToFile(original, "revzero.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            imported.Should().NotBeNull("revision=0 is a valid modern format");
            imported!.Entries.Should().HaveCount(5);
        }

        [Fact]
        public void ImportFromXML_NonExistentFile_ReturnsNull()
        {
            string path = Path.Combine(_tempDir, "does-not-exist.xml");

            var imported = PlanIOHelper.ImportFromXML(path);

            // GetRevisionNumber reads the filename as content when file doesn't exist,
            // so it won't find a revision attribute → returns null
            imported.Should().BeNull("a non-existent file should fail gracefully");
        }

        #endregion

        // =====================================================================
        // SCENARIO 4: Multi-plan import (OutputPlans / .epb)
        // =====================================================================

        #region Multi-Plan Import

        [Fact]
        public void ImportPlansFromXML_RoundTrip_PreservesAllPlans()
        {
            // Build an OutputPlans with multiple plans
            var output = new OutputPlans { Revision = 5 };

            var plan1 = new SerializablePlan { Name = "Plan Alpha" };
            plan1.Entries.Add(new SerializablePlanEntry { ID = 3300, SkillName = "Skill A", Level = 3 });
            plan1.Entries.Add(new SerializablePlanEntry { ID = 3301, SkillName = "Skill B", Level = 5 });

            var plan2 = new SerializablePlan { Name = "Plan Beta" };
            plan2.Entries.Add(new SerializablePlanEntry { ID = 3400, SkillName = "Skill C", Level = 1 });

            output.Plans.Add(plan1);
            output.Plans.Add(plan2);

            var doc = (XmlDocument)Util.SerializeToXmlDocument(output);
            string xml = Util.GetXmlStringRepresentation(doc);
            string path = Path.Combine(_tempDir, "multi.xml");
            File.WriteAllText(path, xml, Encoding.UTF8);

            // Act
            var imported = PlanIOHelper.ImportPlansFromXML(path);

            // Assert
            imported.Should().NotBeNull();
            var plans = imported!.ToList();
            plans.Should().HaveCount(2);
            plans[0].Name.Should().Be("Plan Alpha");
            plans[0].Entries.Should().HaveCount(2);
            plans[1].Name.Should().Be("Plan Beta");
            plans[1].Entries.Should().HaveCount(1);
        }

        #endregion

        // =====================================================================
        // SCENARIO 5: GetRevisionNumber edge cases
        // =====================================================================

        #region Revision Number Parsing

        [Fact]
        public void GetRevisionNumber_ValidFile_ReturnsRevision()
        {
            var plan = BuildOutputPlan("Rev Test", 1, revision: 42);
            string path = WriteOutputPlanToFile(plan, "rev42.xml");

            int revision = Util.GetRevisionNumber(path);

            revision.Should().Be(42);
        }

        [Fact]
        public void GetRevisionNumber_NoRevisionAttribute_ReturnsNegativeOne()
        {
            string xml = @"<?xml version=""1.0""?><plan name=""test""><entry/></plan>";
            string path = Path.Combine(_tempDir, "norev.xml");
            File.WriteAllText(path, xml);

            int revision = Util.GetRevisionNumber(path);

            revision.Should().Be(-1, "missing revision attribute means old format");
        }

        [Fact]
        public void GetRevisionNumber_RevisionZero_ReturnsZero()
        {
            var plan = BuildOutputPlan("Rev 0", 1, revision: 0);
            string path = WriteOutputPlanToFile(plan, "rev0.xml");

            int revision = Util.GetRevisionNumber(path);

            revision.Should().Be(0, "revision=0 is distinct from missing revision (-1)");
        }

        #endregion
    }
}
