// EveLens — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EveLens.Common.Helpers;
using EveLens.Common.Serialization.Eve;
using EveLens.Common.Serialization.Settings;
using EveLens.Common.Services;
using EveLens.Common.SettingsObjects;
using EveLens.Core;
using EveLens.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EveLens.Tests.Helpers
{
    /// <summary>
    /// Tests for the multi-file decomposition and round-trip persistence.
    /// Verifies that DecomposeSettings → SaveMultiFileSync → LoadFromMultiFileFormatAsync
    /// preserves all data faithfully.
    /// </summary>
    [Collection("AppServices")]
    public class MultiFileRoundTripTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly IApplicationPaths _originalPaths;
        private readonly ITraceService _originalTrace;

        public MultiFileRoundTripTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "evelens-multifile-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            _originalPaths = ServiceLocator.ApplicationPaths;
            _originalTrace = ServiceLocator.TraceService;

            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(_tempDir);
            AppServices.SetApplicationPaths(mockPaths);
            AppServices.SyncToServiceLocator();

            SettingsFileManager.EnsureDirectoriesExist();
        }

        public void Dispose()
        {
            AppServices.Reset();
            if (_originalPaths != null)
                ServiceLocator.ApplicationPaths = _originalPaths;
            if (_originalTrace != null)
                ServiceLocator.TraceService = _originalTrace;

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch { }
        }

        #region Helpers

        private static SerializableSettings BuildRealisticSettings(int charCount = 3)
        {
            var settings = new SerializableSettings
            {
                ForkId = "aliacollins",
                ForkVersion = "1.0.0-alpha.25",
                Revision = 5,
                SSOClientID = "test-sso-id",
                SSOClientSecret = "test-sso-secret",
                EsiScopePreset = "Custom"
            };

            settings.EsiCustomScopes.Add("esi-skills.read_skills.v1");
            settings.EsiCustomScopes.Add("esi-wallet.read_character_wallet.v1");

            // Character groups
            var group = new CharacterGroupSettings { Name = "Main Characters" };

            for (int i = 0; i < charCount; i++)
            {
                long charId = 2100000001L + i;
                var guid = Guid.NewGuid();

                var character = new SerializableCCPCharacter
                {
                    Guid = guid,
                    ID = charId,
                    Name = $"Test Pilot {i + 1}",
                    Race = "Caldari",
                    BloodLine = "Deteis",
                    Gender = "Female",
                    CorporationName = "Test Corp",
                    CorporationID = 98000001L,
                    AllianceName = "Test Alliance",
                    AllianceID = 99000001L,
                    Balance = 1000000m * (i + 1),
                    SecurityStatus = 1.5,
                    FreeSkillPoints = i * 50000,
                    FreeRespecs = 2,
                    ShipName = "Raven",
                    ShipTypeName = "Raven Navy Issue",
                    CloneState = "Omega",
                    Label = i == 0 ? "Main" : null,
                    Attributes = new SerializableCharacterAttributes
                    {
                        Intelligence = 25,
                        Memory = 22,
                        Perception = 20,
                        Willpower = 20,
                        Charisma = 17
                    }
                };

                // Add skills
                for (int s = 0; s < 10; s++)
                {
                    character.Skills.Add(new SerializableCharacterSkill
                    {
                        ID = 3300 + s,
                        Name = $"Skill_{s}",
                        Level = (s % 5) + 1,
                        ActiveLevel = (s % 5) + 1,
                        Skillpoints = 256000L * ((s % 5) + 1),
                        IsKnown = true,
                        OwnsBook = s % 2 == 0
                    });
                }

                // Add skill queue
                for (int q = 0; q < 3; q++)
                {
                    character.SkillQueue.Add(new SerializableQueuedSkill
                    {
                        ID = 3300 + q,
                        Level = (q % 5) + 1,
                        StartTime = DateTime.UtcNow.AddHours(q),
                        EndTime = DateTime.UtcNow.AddHours(q + 1),
                        StartSP = q * 25000,
                        EndSP = (q + 1) * 25000
                    });
                }

                // Add employment
                character.EmploymentHistory.Add(new SerializableEmploymentHistory
                {
                    CorporationID = 98000001L,
                    CorporationName = "Test Corp",
                    StartDate = DateTime.UtcNow.AddDays(-365)
                });

                settings.Characters.Add(character);

                // ESI key
                settings.ESIKeys.Add(new SerializableESIKey
                {
                    ID = charId,
                    RefreshToken = $"rt-test-{charId}",
                    Monitored = true,
                    AuthorizedScopes = new List<string>
                    {
                        "esi-skills.read_skills.v1",
                        "esi-wallet.read_character_wallet.v1"
                    }
                });

                // Monitored character
                settings.MonitoredCharacters.Add(new MonitoredCharacterSettings
                {
                    CharacterGuid = guid,
                    Name = character.Name
                });

                // Plan
                var plan = new SerializablePlan
                {
                    Name = $"Plan for Pilot {i + 1}",
                    Description = "Test plan",
                    Owner = guid
                };
                plan.Entries.Add(new SerializablePlanEntry
                {
                    ID = 3305,
                    SkillName = "Caldari Cruiser",
                    Level = 5,
                    Type = EveLens.Common.Enumerations.PlanEntryType.Planned,
                    Priority = 3,
                    Notes = "Need for T2 cruiser"
                });
                settings.Plans.Add(plan);

                // Add first char to group
                if (i < 2)
                    group.CharacterGuids.Add(guid);
            }

            settings.CharacterGroups.Add(group);

            return settings;
        }

        #endregion

        [Fact]
        public void DecomposeSettings_AllFields_RoundTrip()
        {
            var original = BuildRealisticSettings(3);

            var (config, credentials, index, characters) = SettingsFileManager.DecomposeSettings(original);

            // Config
            config.ForkId.Should().Be("aliacollins");
            config.ForkVersion.Should().Be("1.0.0-alpha.25");
            config.Revision.Should().Be(5);
            config.SSOClientID.Should().Be("test-sso-id");
            config.EsiScopePreset.Should().Be("Custom");
            config.EsiCustomScopes.Should().HaveCount(2);
            config.CharacterGroups.Should().HaveCount(1);
            config.CharacterGroups![0].Name.Should().Be("Main Characters");
            config.CharacterGroups[0].CharacterGuids.Should().HaveCount(2);

            // Credentials
            credentials.EsiKeys.Should().HaveCount(3);
            credentials.EsiKeys[0].RefreshToken.Should().StartWith("rt-test-");
            credentials.EsiKeys[0].AuthorizedScopes.Should().HaveCount(2);

            // Index
            index.Characters.Should().HaveCount(3);
            index.MonitoredCharacterIds.Should().HaveCount(3);

            // Characters
            characters.Should().HaveCount(3);
            characters[0].Name.Should().Be("Test Pilot 1");
            characters[0].Skills.Should().HaveCount(10);
            characters[0].SkillQueue.Should().HaveCount(3);
            characters[0].Plans.Should().HaveCount(1);
            characters[0].Plans[0].Entries.Should().HaveCount(1);
        }

        [Fact]
        public void Guid_PreservedAcrossRoundTrip()
        {
            var original = BuildRealisticSettings(2);
            var expectedGuids = original.Characters.Select(c => c.Guid).ToList();

            var (_, _, _, characters) = SettingsFileManager.DecomposeSettings(original);

            characters[0].Guid.Should().Be(expectedGuids[0]);
            characters[1].Guid.Should().Be(expectedGuids[1]);
        }

        [Fact]
        public void CharacterGroups_SurviveRoundTrip()
        {
            var original = BuildRealisticSettings(3);

            var (config, _, _, _) = SettingsFileManager.DecomposeSettings(original);

            config.CharacterGroups.Should().HaveCount(1);
            var group = config.CharacterGroups![0];
            group.Name.Should().Be("Main Characters");
            group.CharacterGuids.Should().HaveCount(2);

            // Verify the Guids match the first two characters
            var expectedGuids = original.Characters.Take(2).Select(c => c.Guid).ToList();
            group.CharacterGuids.Should().BeEquivalentTo(expectedGuids);
        }

        [Fact]
        public void MonitoredCharacters_WithUISettings_SurviveRoundTrip()
        {
            var original = BuildRealisticSettings(2);

            var (_, _, index, characters) = SettingsFileManager.DecomposeSettings(original);

            // All characters should be monitored
            index.MonitoredCharacterIds.Should().HaveCount(2);

            // UISettings should be attached to character data
            foreach (var charData in characters)
            {
                charData.UISettings.Should().NotBeNull();
            }
        }

        [Fact]
        public void SaveMultiFileSync_CreatesAllExpectedFiles()
        {
            var settings = BuildRealisticSettings(3);

            SettingsFileManager.SaveMultiFileSync(settings);

            // config.json
            File.Exists(SettingsFileManager.ConfigFilePath).Should().BeTrue();

            // credentials.json
            File.Exists(SettingsFileManager.CredentialsFilePath).Should().BeTrue();

            // characters/index.json
            File.Exists(SettingsFileManager.CharacterIndexFilePath).Should().BeTrue();

            // characters/{id}.json for each character
            foreach (var character in settings.Characters)
            {
                string charPath = SettingsFileManager.GetCharacterFilePath(character.ID);
                File.Exists(charPath).Should().BeTrue($"character file for {character.ID} should exist");
            }
        }

        [Fact]
        public void SaveMultiFileSync_RemovesOrphanedCharacterFiles()
        {
            // Save with 3 characters
            var settings3 = BuildRealisticSettings(3);
            SettingsFileManager.SaveMultiFileSync(settings3);

            long thirdCharId = settings3.Characters[2].ID;
            File.Exists(SettingsFileManager.GetCharacterFilePath(thirdCharId)).Should().BeTrue();

            // Now save with only 2 characters — the third should be removed
            var settings2 = BuildRealisticSettings(2);
            SettingsFileManager.SaveMultiFileSync(settings2);

            File.Exists(SettingsFileManager.GetCharacterFilePath(thirdCharId)).Should().BeFalse(
                "orphaned character file should be removed");
        }

        [Fact]
        public async Task SaveMultiFile_ThenLoad_FullRoundTrip()
        {
            var original = BuildRealisticSettings(3);

            // Save
            await SettingsFileManager.SaveMultiFileAsync(original);

            // Load
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();

            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(3);
            loaded.ESIKeys.Should().HaveCount(3);
            loaded.Plans.Should().HaveCount(3);
            loaded.MonitoredCharacters.Should().HaveCount(3);
            loaded.ForkId.Should().Be("aliacollins");
            loaded.EsiScopePreset.Should().Be("Custom");
            loaded.EsiCustomScopes.Should().HaveCount(2);
            loaded.CharacterGroups.Should().HaveCount(1);

            // Verify character data fidelity
            var firstChar = loaded.Characters[0];
            firstChar.Name.Should().Be("Test Pilot 1");
            firstChar.Skills.Should().HaveCount(10);
            firstChar.Guid.Should().Be(original.Characters[0].Guid);

            // Verify plan ownership
            foreach (var plan in loaded.Plans)
            {
                original.Characters.Any(c => c.Guid == plan.Owner).Should().BeTrue(
                    "plan owner Guid must match a character");
            }
        }

        [Fact]
        public async Task MigrationFromSettingsJson_WritesMultiFile_RenamesOld()
        {
            var settings = BuildRealisticSettings(2);

            // Write in old single-file format (settings.json)
            string json = System.Text.Json.JsonSerializer.Serialize(settings, SettingsFileManager.DirectJsonOptions);
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, json);

            // Load should auto-migrate
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();

            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(2);

            // Multi-file format should now exist
            File.Exists(SettingsFileManager.ConfigFilePath).Should().BeTrue(
                "config.json should be created during migration");

            // Old settings.json should be renamed
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeFalse(
                "settings.json should be renamed after migration");
            File.Exists(SettingsFileManager.SettingsJsonFilePath + ".migrated").Should().BeTrue(
                "settings.json.migrated should exist after migration");
        }

        [Fact]
        public async Task SaveSync_ThenLoadAsync_IdenticalData()
        {
            var original = BuildRealisticSettings(5);

            // Use sync save (like Settings.Save uses)
            SettingsFileManager.SaveMultiFileSync(original);

            // Load async (like startup uses)
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();

            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(5);
            loaded.ESIKeys.Should().HaveCount(5);
            loaded.Plans.Should().HaveCount(5);
            loaded.Revision.Should().Be(5);

            // Guids must round-trip
            for (int i = 0; i < 5; i++)
            {
                loaded.Characters[i].Guid.Should().Be(original.Characters[i].Guid);
                loaded.Characters[i].Name.Should().Be(original.Characters[i].Name);
                loaded.Characters[i].Skills.Should().HaveCount(10);
            }
        }

        [Fact]
        public async Task EmptySettings_MultiFileRoundTrip()
        {
            var empty = new SerializableSettings();

            SettingsFileManager.SaveMultiFileSync(empty);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().BeEmpty();
            loaded.ESIKeys.Should().BeEmpty();
            loaded.Plans.Should().BeEmpty();
        }

        [Fact]
        public async Task ConfigJsonBackup_RecoversOnCorruption()
        {
            var settings = BuildRealisticSettings(2);

            // Save twice to create .bak
            SettingsFileManager.SaveMultiFileSync(settings);
            SettingsFileManager.SaveMultiFileSync(settings);

            // Corrupt config.json
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "CORRUPT!");

            // Load should recover from config.json.bak
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(2);
        }
    }
}
