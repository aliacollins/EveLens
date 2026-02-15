using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using EVEMon.Common.Helpers;
using EVEMon.Common.Services;
using EVEMon.Core;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Helpers
{
    /// <summary>
    /// Tests for SettingsFileManager: atomic writes, directory management,
    /// config/credentials serialization, and migration detection.
    /// Uses temp directories for full isolation.
    /// </summary>
    [Collection("AppServices")]
    public class SettingsFileManagerTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly IApplicationPaths _originalPaths;
        private readonly ITraceService _originalTrace;

        public SettingsFileManagerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "evemon-sfm-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            // Save original values
            _originalPaths = ServiceLocator.ApplicationPaths;
            _originalTrace = ServiceLocator.TraceService;

            // Set up AppServices to use our temp directory
            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(_tempDir);
            AppServices.SetApplicationPaths(mockPaths);
            AppServices.SyncToServiceLocator();
        }

        public void Dispose()
        {
            // Restore originals
            AppServices.Reset();
            if (_originalPaths != null)
            {
                ServiceLocator.ApplicationPaths = _originalPaths;
            }
            if (_originalTrace != null)
            {
                ServiceLocator.TraceService = _originalTrace;
            }

            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors
            }
        }

        #region Directory Management

        [Fact]
        public void EnsureDirectoriesExist_CreatesDataDirectory()
        {
            string subDir = Path.Combine(_tempDir, "subtest-" + Guid.NewGuid().ToString("N"));
            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(subDir);
            AppServices.SetApplicationPaths(mockPaths);
            AppServices.SyncToServiceLocator();

            SettingsFileManager.EnsureDirectoriesExist();

            Directory.Exists(subDir).Should().BeTrue();
            Directory.Exists(Path.Combine(subDir, "characters")).Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoriesExist_CreatesCharactersSubdirectory()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            Directory.Exists(SettingsFileManager.CharactersDirectory).Should().BeTrue();
        }

        [Fact]
        public void EnsureDirectoriesExist_Idempotent_DoesNotThrow()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            Action act = () => SettingsFileManager.EnsureDirectoriesExist();
            act.Should().NotThrow();
        }

        #endregion

        #region Detection (JSON vs XML)

        [Fact]
        public void JsonSettingsExist_NoConfigFile_ReturnsFalse()
        {
            SettingsFileManager.JsonSettingsExist().Should().BeFalse();
        }

        [Fact]
        public void JsonSettingsExist_WithConfigFile_ReturnsTrue()
        {
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "{}");

            SettingsFileManager.JsonSettingsExist().Should().BeTrue();
        }

        [Fact]
        public void LegacySettingsExist_NoXmlFile_ReturnsFalse()
        {
            SettingsFileManager.LegacySettingsExist().Should().BeFalse();
        }

        [Fact]
        public void LegacySettingsExist_WithXmlFile_ReturnsTrue()
        {
            File.WriteAllText(SettingsFileManager.LegacySettingsFilePath, "<Settings/>");

            SettingsFileManager.LegacySettingsExist().Should().BeTrue();
        }

        [Fact]
        public void NeedsMigration_XmlExistsNoJson_ReturnsTrue()
        {
            File.WriteAllText(SettingsFileManager.LegacySettingsFilePath, "<Settings/>");

            SettingsFileManager.NeedsMigration().Should().BeTrue();
        }

        [Fact]
        public void NeedsMigration_BothExist_ReturnsFalse()
        {
            File.WriteAllText(SettingsFileManager.LegacySettingsFilePath, "<Settings/>");
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "{}");

            SettingsFileManager.NeedsMigration().Should().BeFalse();
        }

        [Fact]
        public void NeedsMigration_NeitherExist_ReturnsFalse()
        {
            SettingsFileManager.NeedsMigration().Should().BeFalse();
        }

        #endregion

        #region Config Load/Save (Atomic Write)

        [Fact]
        public async Task SaveConfigAsync_CreatesConfigFile()
        {
            var config = new JsonConfig
            {
                Version = 1,
                ForkId = "aliacollins"
            };

            await SettingsFileManager.SaveConfigAsync(config);

            File.Exists(SettingsFileManager.ConfigFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task SaveConfigAsync_ThenLoadConfigAsync_RoundTrips()
        {
            var config = new JsonConfig
            {
                Version = 1,
                ForkId = "testfork",
                ForkVersion = "5.2.0"
            };

            await SettingsFileManager.SaveConfigAsync(config);
            var loaded = await SettingsFileManager.LoadConfigAsync();

            loaded.Should().NotBeNull();
            loaded.Version.Should().Be(1);
            loaded.ForkId.Should().Be("testfork");
            loaded.ForkVersion.Should().Be("5.2.0");
        }

        [Fact]
        public async Task SaveConfigAsync_NoTempFileRemains()
        {
            var config = new JsonConfig { Version = 1 };

            await SettingsFileManager.SaveConfigAsync(config);

            // Temp file pattern is ".config.json.tmp"
            string tempPath = Path.Combine(_tempDir, ".config.json.tmp");
            File.Exists(tempPath).Should().BeFalse();
        }

        [Fact]
        public async Task SaveConfigAsync_OverwritesExisting()
        {
            var config1 = new JsonConfig { ForkId = "first" };
            await SettingsFileManager.SaveConfigAsync(config1);

            var config2 = new JsonConfig { ForkId = "second" };
            await SettingsFileManager.SaveConfigAsync(config2);

            var loaded = await SettingsFileManager.LoadConfigAsync();
            loaded.ForkId.Should().Be("second");
        }

        #endregion

        #region LoadConfigAsync - Corrupted File Detection

        [Fact]
        public async Task LoadConfigAsync_MissingFile_ReturnsDefaults()
        {
            var config = await SettingsFileManager.LoadConfigAsync();

            config.Should().NotBeNull();
            config.Version.Should().Be(1);
            config.ForkId.Should().Be("aliacollins");
        }

        [Fact]
        public async Task LoadConfigAsync_CorruptedJson_ReturnsDefaults()
        {
            // Write invalid JSON
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "{ this is not valid json }}}");

            var config = await SettingsFileManager.LoadConfigAsync();

            // Should return defaults, not throw
            config.Should().NotBeNull();
        }

        [Fact]
        public async Task LoadConfigAsync_EmptyFile_ReturnsDefaults()
        {
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "");

            var config = await SettingsFileManager.LoadConfigAsync();

            config.Should().NotBeNull();
        }

        #endregion

        #region Credentials Load/Save

        [Fact]
        public async Task SaveCredentialsAsync_ThenLoadCredentialsAsync_RoundTrips()
        {
            var creds = new JsonCredentials
            {
                Version = 1,
                EsiKeys = { new JsonEsiKey { CharacterId = 12345, RefreshToken = "token123", Monitored = true } }
            };

            await SettingsFileManager.SaveCredentialsAsync(creds);
            var loaded = await SettingsFileManager.LoadCredentialsAsync();

            loaded.Should().NotBeNull();
            loaded.EsiKeys.Should().HaveCount(1);
            loaded.EsiKeys[0].CharacterId.Should().Be(12345);
            loaded.EsiKeys[0].RefreshToken.Should().Be("token123");
            loaded.EsiKeys[0].Monitored.Should().BeTrue();
        }

        [Fact]
        public async Task LoadCredentialsAsync_MissingFile_ReturnsEmpty()
        {
            var creds = await SettingsFileManager.LoadCredentialsAsync();

            creds.Should().NotBeNull();
            creds.EsiKeys.Should().BeEmpty();
        }

        [Fact]
        public async Task SaveCredentialsAsync_SetsLastSaved()
        {
            var before = DateTime.UtcNow;
            var creds = new JsonCredentials();

            await SettingsFileManager.SaveCredentialsAsync(creds);

            creds.LastSaved.Should().BeOnOrAfter(before);
            creds.LastSaved.Should().BeOnOrBefore(DateTime.UtcNow);
        }

        #endregion

        #region Character Data

        [Fact]
        public async Task SaveCharacterAsync_ThenLoadCharacterAsync_RoundTrips()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            var character = new JsonCharacterData
            {
                CharacterId = 98765,
                Name = "Test Pilot",
                CorporationName = "Test Corp",
                Intelligence = 20,
                Memory = 21,
                Charisma = 19,
                Perception = 22,
                Willpower = 23
            };

            await SettingsFileManager.SaveCharacterAsync(character);
            var loaded = await SettingsFileManager.LoadCharacterAsync(98765);

            loaded.Should().NotBeNull();
            loaded!.CharacterId.Should().Be(98765);
            loaded.Name.Should().Be("Test Pilot");
            loaded.CorporationName.Should().Be("Test Corp");
            loaded.Intelligence.Should().Be(20);
        }

        [Fact]
        public async Task LoadCharacterAsync_NonexistentId_ReturnsNull()
        {
            var result = await SettingsFileManager.LoadCharacterAsync(99999);
            result.Should().BeNull();
        }

        [Fact]
        public void DeleteCharacter_RemovesFile()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            string filePath = SettingsFileManager.GetCharacterFilePath(11111);
            File.WriteAllText(filePath, "{}");
            File.Exists(filePath).Should().BeTrue();

            SettingsFileManager.DeleteCharacter(11111);

            File.Exists(filePath).Should().BeFalse();
        }

        [Fact]
        public void DeleteCharacter_NonexistentId_DoesNotThrow()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            Action act = () => SettingsFileManager.DeleteCharacter(99999);
            act.Should().NotThrow();
        }

        [Fact]
        public void GetCharacterFilePath_ReturnsExpectedFormat()
        {
            var path = SettingsFileManager.GetCharacterFilePath(12345);

            path.Should().EndWith("12345.json");
            path.Should().Contain("characters");
        }

        #endregion

        #region ClearAllJsonFiles

        [Fact]
        public async Task ClearAllJsonFiles_RemovesConfigAndCredentials()
        {
            // Create files first
            await SettingsFileManager.SaveConfigAsync(new JsonConfig());
            await SettingsFileManager.SaveCredentialsAsync(new JsonCredentials());

            File.Exists(SettingsFileManager.ConfigFilePath).Should().BeTrue();
            File.Exists(SettingsFileManager.CredentialsFilePath).Should().BeTrue();

            SettingsFileManager.ClearAllJsonFiles();

            File.Exists(SettingsFileManager.ConfigFilePath).Should().BeFalse();
            File.Exists(SettingsFileManager.CredentialsFilePath).Should().BeFalse();
        }

        [Fact]
        public void ClearAllJsonFiles_NoFiles_DoesNotThrow()
        {
            Action act = () => SettingsFileManager.ClearAllJsonFiles();
            act.Should().NotThrow();
        }

        #endregion

        #region Character Index

        [Fact]
        public async Task SaveCharacterIndexAsync_ThenLoad_RoundTrips()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            var index = new JsonCharacterIndex
            {
                Version = 1,
                Characters =
                {
                    new JsonCharacterIndexEntry
                    {
                        CharacterId = 111,
                        Name = "Pilot One",
                        CorporationName = "Corp A"
                    },
                    new JsonCharacterIndexEntry
                    {
                        CharacterId = 222,
                        Name = "Pilot Two",
                        CorporationName = "Corp B"
                    }
                },
                MonitoredCharacterIds = { 111 }
            };

            await SettingsFileManager.SaveCharacterIndexAsync(index);
            var loaded = await SettingsFileManager.LoadCharacterIndexAsync();

            loaded.Should().NotBeNull();
            loaded.Characters.Should().HaveCount(2);
            loaded.Characters[0].Name.Should().Be("Pilot One");
            loaded.MonitoredCharacterIds.Should().Contain(111);
        }

        [Fact]
        public async Task LoadCharacterIndexAsync_MissingFile_ReturnsEmpty()
        {
            var index = await SettingsFileManager.LoadCharacterIndexAsync();

            index.Should().NotBeNull();
            index.Characters.Should().BeEmpty();
        }

        #endregion

        #region GetSavedCharacterIds

        [Fact]
        public void GetSavedCharacterIds_EmptyDirectory_ReturnsEmpty()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            var ids = SettingsFileManager.GetSavedCharacterIds();
            ids.Should().BeEmpty();
        }

        [Fact]
        public void GetSavedCharacterIds_WithCharacterFiles_ReturnsIds()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            File.WriteAllText(SettingsFileManager.GetCharacterFilePath(111), "{}");
            File.WriteAllText(SettingsFileManager.GetCharacterFilePath(222), "{}");

            var ids = SettingsFileManager.GetSavedCharacterIds();
            ids.Should().Contain(111);
            ids.Should().Contain(222);
        }

        [Fact]
        public void GetSavedCharacterIds_ExcludesIndexFile()
        {
            SettingsFileManager.EnsureDirectoriesExist();

            // The index.json file should not be returned as a character ID
            File.WriteAllText(SettingsFileManager.CharacterIndexFilePath, "{}");
            File.WriteAllText(SettingsFileManager.GetCharacterFilePath(333), "{}");

            var ids = SettingsFileManager.GetSavedCharacterIds();
            ids.Should().Contain(333);
            ids.Should().NotContain(0); // "index" cannot be parsed to a long
        }

        [Fact]
        public void GetSavedCharacterIds_NoCharactersDirectory_ReturnsEmpty()
        {
            // Don't call EnsureDirectoriesExist - characters dir doesn't exist
            var ids = SettingsFileManager.GetSavedCharacterIds();
            ids.Should().BeEmpty();
        }

        #endregion

        #region Backup Detection

        [Fact]
        public void IsJsonBackupFile_NullPath_ReturnsFalse()
        {
            SettingsFileManager.IsJsonBackupFile(null!).Should().BeFalse();
        }

        [Fact]
        public void IsJsonBackupFile_EmptyPath_ReturnsFalse()
        {
            SettingsFileManager.IsJsonBackupFile("").Should().BeFalse();
        }

        [Fact]
        public void IsJsonBackupFile_XmlExtension_ReturnsFalse()
        {
            SettingsFileManager.IsJsonBackupFile("settings.xml").Should().BeFalse();
        }

        [Fact]
        public void IsJsonBackupFile_ValidBackupContent_ReturnsTrue()
        {
            string filePath = Path.Combine(_tempDir, "backup.json");
            File.WriteAllText(filePath, "{\"ForkId\": \"test\", \"Characters\": []}");

            SettingsFileManager.IsJsonBackupFile(filePath).Should().BeTrue();
        }

        [Fact]
        public void IsJsonBackupFile_RegularJson_ReturnsFalse()
        {
            string filePath = Path.Combine(_tempDir, "other.json");
            File.WriteAllText(filePath, "{\"key\": \"value\"}");

            SettingsFileManager.IsJsonBackupFile(filePath).Should().BeFalse();
        }

        #endregion
    }
}
