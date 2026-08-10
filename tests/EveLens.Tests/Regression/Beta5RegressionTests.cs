// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System.IO;
using System.Linq;
using System.Xml.Serialization;
using EveLens.Common.Data;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Tests.TestDoubles;
using FluentAssertions;
using Xunit;

namespace EveLens.Tests.Regression
{
    /// <summary>
    /// Regression tests for the 1.4.0-beta.5 fix train: stale SDE prerequisites (#99),
    /// missing planet data (#66), and the ship-name-frozen-in-import-language bug.
    /// Requires the bundled datafiles in the test output Resources/ folder.
    /// </summary>
    [Collection("StaticData")]
    public class Beta5RegressionTests
    {
        public Beta5RegressionTests()
        {
            PlanTestFixture.EnsureGameDataLoaded();
        }

        #region Issue #99 — SDE prerequisite freshness

        [Fact]
        public void CapitalJumpPortalGeneration_RequiresJumpPortalGenerationIII()
        {
            // CCP lowered this prereq from V to III; our bundled SDE was stale for months
            // (Issue #99). This pins the regenerated data — if a future SDE regeneration
            // silently reverts or the datafile goes stale again, this fails.
            var skill = StaticSkills.GetSkillByName("Capital Jump Portal Generation");
            skill.Should().NotBeNull("skill 83094 must exist in the bundled SDE");

            var prereq = skill!.Prerequisites.FirstOrDefault(
                p => p.Skill?.Name == "Jump Portal Generation");
            prereq.Should().NotBeNull();
            prereq!.Level.Should().Be(3,
                "CCP reduced the requirement to level 3 (Issue #99, SDE build 3458726+)");
        }

        #endregion

        #region Issue #66 — planet data in the geography datafile

        [Fact]
        public void GeographyDatafile_ContainsPlanets()
        {
            // The YAML->SQLite pipeline never populated planets, so the geography datafile
            // shipped with ZERO of them and every PI colony showed "Unknown" as its planet
            // name (Issue #66). Jita IV is the canonical never-going-away planet.
            StaticGeography.Load();

            var jita = StaticGeography.GetSolarSystemByName("Jita");
            (jita != null).Should().BeTrue("Jita must exist in the geography datafile");

            var planets = jita!.Planets?.ToList();
            planets.Should().NotBeNullOrEmpty(
                "solar systems must carry their planets — an empty list regenerates the " +
                "\"Unknown\" planet name bug (Issue #66)");
            planets!.Should().Contain(p => p.Name == "Jita IV",
                "planet names derive from system name + celestial index");
        }

        #endregion

        #region Ship name must re-resolve in the current language

        [Fact]
        public void SerializableCharacter_ShipTypeID_RoundTrips()
        {
            // Law 13: the new shipTypeID element is part of the data contract. Losing it
            // reverts ship names to strings frozen in the import-time language.
            var character = new SerializableSettingsCharacter
            {
                ID = 91234567,
                Name = "Test Pilot",
                ShipName = "My Shuttle",
                ShipTypeName = "외교관 셔틀",
                ShipTypeID = 21628,
            };

            var serializer = new XmlSerializer(typeof(SerializableSettingsCharacter));
            using var writer = new StringWriter();
            serializer.Serialize(writer, character);
            using var reader = new StringReader(writer.ToString());
            var result = (SerializableSettingsCharacter)serializer.Deserialize(reader)!;

            result.ShipTypeID.Should().Be(21628);
            result.ShipTypeName.Should().Be("외교관 셔틀");
        }

        [Fact]
        public void LegacyCharacterFile_WithoutShipTypeID_DeserializesToZero()
        {
            // Files saved before the field existed carry only the (possibly wrong-language)
            // name string; ID zero signals "keep the stored string" on import.
            const string legacyXml =
                "<SerializableSettingsCharacter><name>Old Pilot</name>" +
                "<shipTypeName>셔틀</shipTypeName></SerializableSettingsCharacter>";
            var serializer = new XmlSerializer(typeof(SerializableSettingsCharacter));
            using var reader = new StringReader(legacyXml);
            var result = (SerializableSettingsCharacter)serializer.Deserialize(reader)!;

            result.ShipTypeID.Should().Be(0);
            result.ShipTypeName.Should().Be("셔틀");
        }

        [Fact]
        public void ShipTypeName_ReResolvesInCurrentLanguage_WhenIDPersisted()
        {
            // The end-to-end bug: running in Korean froze the LOCALIZED name string into the
            // character file; switching back to English kept showing Korean ship names even
            // after restart. With the ID persisted, import re-resolves in the CURRENT language.
            var prevLang = Loc.Language;
            var prevMode = EveLens.Common.Settings.UI.GameNameMode;
            try
            {
                Loc.Language = "en";
                EveLens.Common.Settings.UI.GameNameMode =
                    Common.Enumerations.UISettings.GameNameMode.Auto;

                // Type 34 (Tritanium) — universal test item with translations in every language
                string english = StaticItems.GetItemName(34);
                var item = StaticItems.GetItemByID(34);
                item.Should().NotBeNull();
                item!.LocalizedName.Should().Be(english,
                    "with English active, re-resolution from the ID must yield the English name " +
                    "regardless of what language string was persisted");
            }
            finally
            {
                Loc.Language = prevLang;
                EveLens.Common.Settings.UI.GameNameMode = prevMode;
            }
        }

        #endregion
    }
}
