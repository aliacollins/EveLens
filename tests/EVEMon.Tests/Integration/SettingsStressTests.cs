using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Helpers;
using EVEMon.Common.Serialization.Eve;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Services;
using EVEMon.Common.SettingsObjects;
using EVEMon.Core;
using EVEMon.Core.Events;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Integration
{
    /// <summary>
    /// Stress tests for the settings save pipeline with 100 characters.
    /// Simulates the full lifecycle: bulk add → save → restart → crash recovery.
    ///
    /// These tests verify:
    /// 1. The save pipeline handles 100 characters without crashing.
    /// 2. SmartSettingsManager coalesces rapid saves during bulk character import.
    /// 3. The UI thread (dispatcher) remains responsive during saves.
    /// 4. Shutdown flushes dirty state to disk.
    /// 5. Crash mid-save recovers from .bak files.
    /// 6. SettingsFileManager writes 100+ character files atomically.
    /// 7. Round-trip: save 100 characters → reload → identical data.
    /// </summary>
    [Collection("AppServices")]
    public class SettingsStressTests : IDisposable
    {
        private readonly IEventAggregator _mockAggregator;
        private readonly IDispatcher _mockDispatcher;
        private readonly string _tempDir;
        private readonly IApplicationPaths _originalPaths;
        private readonly ITraceService _originalTrace;

        // Tracks dispatcher calls for verification
        private int _postCallCount;
        private int _invokeCallCount;

        private static readonly JsonSerializerOptions s_jsonReadOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public SettingsStressTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
            _mockDispatcher = Substitute.For<IDispatcher>();

            _mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                .Do(ci =>
                {
                    Interlocked.Increment(ref _invokeCallCount);
                    ci.ArgAt<Action>(0).Invoke();
                });

            _mockDispatcher.When(d => d.Post(Arg.Any<Action>()))
                .Do(ci =>
                {
                    Interlocked.Increment(ref _postCallCount);
                    ci.ArgAt<Action>(0).Invoke();
                });

            _tempDir = Path.Combine(Path.GetTempPath(), "evemon-stress-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);

            _originalPaths = ServiceLocator.ApplicationPaths;
            _originalTrace = ServiceLocator.TraceService;

            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(_tempDir);
            AppServices.SetApplicationPaths(mockPaths);
            AppServices.SyncToServiceLocator();

            // Ensure directories exist for the save pipeline
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

        /// <summary>
        /// Builds a realistic SerializableSettings with N characters, each with skills,
        /// skill queues, plans, ESI keys, and monitored character entries.
        /// </summary>
        private static SerializableSettings BuildSettingsWith(int characterCount)
        {
            var settings = new SerializableSettings
            {
                ForkId = "aliacollins",
                ForkVersion = "5.2.0",
                Revision = 5
            };

            for (int i = 0; i < characterCount; i++)
            {
                long charId = 2100000001L + i;
                var guid = Guid.NewGuid();

                var character = new SerializableCCPCharacter
                {
                    Guid = guid,
                    ID = charId,
                    Name = $"Stress Pilot {i + 1}",
                    Race = "Caldari",
                    BloodLine = "Deteis",
                    Gender = "Female",
                    CorporationName = $"Corp {i % 10}",
                    CorporationID = 98000001L + (i % 10),
                    AllianceName = $"Alliance {i % 3}",
                    AllianceID = 99000001L + (i % 3),
                    Balance = 1000000m * (i + 1),
                    SecurityStatus = -5.0 + (i * 0.1),
                    FreeSkillPoints = i * 10000,
                    FreeRespecs = (short)(i % 4),
                    ShipName = $"Ship_{i}",
                    ShipTypeName = "Raven Navy Issue",
                    CloneState = i % 3 == 0 ? "Alpha" : "Omega",
                    Label = i % 5 == 0 ? $"Main {i / 5}" : null,
                    Attributes = new SerializableCharacterAttributes
                    {
                        Intelligence = 20 + (i % 7),
                        Memory = 20 + (i % 5),
                        Perception = 20 + (i % 6),
                        Willpower = 20 + (i % 4),
                        Charisma = 17 + (i % 8)
                    }
                };

                // Add 50 skills per character
                for (int s = 0; s < 50; s++)
                {
                    character.Skills.Add(new SerializableCharacterSkill
                    {
                        ID = 3300 + s,
                        Name = $"Skill_{s}",
                        Level = (s % 5) + 1,
                        ActiveLevel = (s % 5) + 1,
                        Skillpoints = (long)(256000 * ((s % 5) + 1)),
                        IsKnown = true,
                        OwnsBook = s % 2 == 0
                    });
                }

                // Add 10 skill queue entries per character
                for (int q = 0; q < 10; q++)
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

                settings.Characters.Add(character);

                // Add ESI key for each character
                settings.ESIKeys.Add(new SerializableESIKey
                {
                    ID = charId,
                    RefreshToken = $"rt-stress-{charId}",
                    AccessMask = 4294967295UL,
                    Monitored = true
                });

                // Add a plan with 5 entries per character
                var plan = new SerializablePlan
                {
                    Name = $"Plan for Pilot {i + 1}",
                    Description = $"Auto-generated plan #{i}",
                    Owner = guid
                };
                for (int p = 0; p < 5; p++)
                {
                    plan.Entries.Add(new SerializablePlanEntry
                    {
                        ID = 3300 + p,
                        SkillName = $"Skill_{p}",
                        Level = (p % 5) + 1,
                        Priority = p + 1
                    });
                }
                settings.Plans.Add(plan);

                // Mark as monitored
                settings.MonitoredCharacters.Add(new MonitoredCharacterSettings
                {
                    CharacterGuid = guid,
                    Name = $"Stress Pilot {i + 1}"
                });
            }

            return settings;
        }

        private SmartSettingsManager CreateManager(Func<SerializableSettings>? exportFunc = null)
        {
            return new SmartSettingsManager(
                _tempDir,
                _mockAggregator,
                _mockDispatcher,
                exportFunc ?? (() => new SerializableSettings()));
        }

        /// <summary>
        /// Count JSON files in the characters directory.
        /// </summary>
        private int CountCharacterFiles()
        {
            string charDir = Path.Combine(_tempDir, "characters");
            if (!Directory.Exists(charDir))
                return 0;
            // Count only numbered JSON files, exclude index.json
            return Directory.GetFiles(charDir, "*.json")
                .Count(f => long.TryParse(Path.GetFileNameWithoutExtension(f), out _));
        }

        #endregion

        // =====================================================================
        // SCENARIO 1: 100 characters are added, full save pipeline fires
        // =====================================================================

        #region Adding 100 Characters — Save Pipeline Behavior

        [Fact]
        public async Task Save100Characters_AllFilesCreated()
        {
            // Arrange — build settings with 100 characters
            var settings = BuildSettingsWith(100);

            // Act — save through SettingsFileManager (the file I/O layer)
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Assert — verify settings.json exists (single-file format)
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();

            // Verify round-trip
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(100);
            loaded.MonitoredCharacters.Should().HaveCount(100);
        }

        [Fact]
        public async Task Save100Characters_ESIKeysPreserved()
        {
            var settings = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded!.ESIKeys.Should().HaveCount(100);

            // Each key should have a valid refresh token
            foreach (var key in loaded.ESIKeys)
            {
                key.RefreshToken.Should().StartWith("rt-stress-");
                key.AccessMask.Should().Be(4294967295UL);
                key.Monitored.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Save100Characters_EachCharacterHasCorrectData()
        {
            var settings = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();

            // Spot-check several characters
            for (int i = 0; i < 100; i += 17)  // check every 17th character
            {
                long charId = 2100000001L + i;
                var character = loaded!.Characters.FirstOrDefault(c => c.ID == charId);

                character.Should().NotBeNull($"character {charId} should be loadable");
                character!.Name.Should().Be($"Stress Pilot {i + 1}");
                character.Skills.Should().HaveCount(50,
                    $"character {charId} should have 50 skills");

                if (character is SerializableCCPCharacter ccpChar)
                {
                    ccpChar.SkillQueue.Should().HaveCount(10,
                        $"character {charId} should have 10 queue entries");
                }

                var plans = loaded.Plans.Where(p => p.Owner == character.Guid).ToList();
                plans.Should().HaveCount(1,
                    $"character {charId} should have 1 plan");
                plans[0].Entries.Should().HaveCount(5);
            }
        }

        [Fact]
        public async Task Save100Characters_SSOPreserved()
        {
            var settings = BuildSettingsWith(100);
            settings.SSOClientID = "custom-stress-client";
            settings.SSOClientSecret = "custom-stress-secret";

            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded!.SSOClientID.Should().Be("custom-stress-client");
            loaded.SSOClientSecret.Should().Be("custom-stress-secret");
        }

        #endregion

        // =====================================================================
        // SCENARIO 2: SmartSettingsManager coalescing with 100 characters
        // =====================================================================

        #region Save Coalescing and UI Thread Behavior

        [Fact]
        public void Coalescing_100RapidSaves_OnlyOneDirtyFlag()
        {
            // Simulate 100 settings-changed events (one per character add)
            // SmartSettingsManager should coalesce all into a single write.
            var settings = BuildSettingsWith(100);
            using var manager = CreateManager(() => settings);

            // Each character add triggers Settings.Save() → SmartSettingsManager.Save()
            for (int i = 0; i < 100; i++)
            {
                manager.Save();
            }

            manager.SaveCallCount.Should().Be(100);
            manager.ActualWriteCount.Should().Be(0,
                "no writes until timer fires — all 100 saves are coalesced");
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public async Task Coalescing_100Saves_ThenImmediate_ProducesOneWrite()
        {
            var settings = BuildSettingsWith(100);
            using var manager = CreateManager(() => settings);

            // 100 rapid Save() calls
            for (int i = 0; i < 100; i++)
                manager.Save();

            // Then one SaveImmediateAsync (e.g., user clicks Save)
            await manager.SaveImmediateAsync();

            manager.ActualWriteCount.Should().Be(1,
                "100 coalesced saves + 1 immediate = only 1 actual write");

            // Verify settings.json was written
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task Coalescing_DispatcherPostCalled_ForExportOnUIThread()
        {
            var settings = BuildSettingsWith(100);
            _postCallCount = 0;

            using var manager = CreateManager(() => settings);
            await manager.SaveImmediateAsync();

            // Post should be called exactly once per save (to marshal Export to UI thread)
            _postCallCount.Should().Be(1,
                "Export() is dispatched once per actual write, not per Save() call");
        }

        [Fact]
        public async Task Coalescing_WriteLockSerializesAccess()
        {
            var settings = BuildSettingsWith(100);
            using var manager = CreateManager(() => settings);

            // Perform multiple immediate saves — each should serialize through WriteLock
            await manager.SaveImmediateAsync();
            await manager.SaveImmediateAsync();
            await manager.SaveImmediateAsync();

            manager.ActualWriteCount.Should().Be(3);

            // WriteLock should still be available (not deadlocked)
            manager.WriteLock.CurrentCount.Should().Be(1,
                "write lock should be released after each save");
        }

        [Fact]
        public async Task Coalescing_EventPublishedPerWrite_NotPerSaveCall()
        {
            var settings = BuildSettingsWith(10); // smaller for speed
            using var manager = CreateManager(() => settings);

            manager.Save();
            manager.Save();
            manager.Save();
            await manager.SaveImmediateAsync();

            // SettingsSavedEvent published once per actual write
            _mockAggregator.Received(1).Publish(Arg.Any<SettingsSavedEvent>());
        }

        #endregion

        // =====================================================================
        // SCENARIO 3: User closes EVEMon (graceful shutdown)
        // =====================================================================

        #region Graceful Shutdown — Dispose Flushes Dirty State

        [Fact]
        public void Shutdown_WithDirtyState_FlushesAllCharacters()
        {
            var settings = BuildSettingsWith(100);
            var manager = CreateManager(() => settings);

            // Simulate 100 character adds (rapid saves)
            for (int i = 0; i < 100; i++)
                manager.Save();

            manager.IsDirty.Should().BeTrue();
            manager.ActualWriteCount.Should().Be(0);

            // Dispose simulates graceful shutdown — should flush
            manager.Dispose();

            // After dispose, settings.json should be on disk
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue(
                "Dispose should flush settings to disk");
        }

        [Fact]
        public void Shutdown_DisposeFlushesThroughInvoke_NotPost()
        {
            var settings = BuildSettingsWith(10);
            _invokeCallCount = 0;

            var manager = CreateManager(() => settings);
            manager.Save();
            manager.Dispose();

            // Dispose uses Invoke (synchronous) to marshal Export to UI thread
            // because it needs to complete before the method returns
            _invokeCallCount.Should().BeGreaterThan(0,
                "Dispose should use Invoke (blocking) for the flush");
        }

        [Fact]
        public void Shutdown_DoubleDispose_DoesNotThrow()
        {
            var settings = BuildSettingsWith(100);
            var manager = CreateManager(() => settings);
            manager.Save();
            manager.Dispose();

            Action act = () => manager.Dispose();
            act.Should().NotThrow("double-dispose is safe");
        }

        [Fact]
        public void Shutdown_NotDirty_DoesNotFlush()
        {
            int exportCount = 0;
            var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCount);
                return BuildSettingsWith(100);
            });

            // No Save() calls — not dirty
            manager.Dispose();

            exportCount.Should().Be(0, "no export when not dirty");
        }

        #endregion

        // =====================================================================
        // SCENARIO 5: Restart — load what was saved
        // =====================================================================

        #region Restart Round-Trip — Save then Reload

        [Fact]
        public async Task Restart_RoundTrip_100Characters_AllDataPreserved()
        {
            // === Phase 1: "Running EVEMon" — save 100 characters ===
            var originalSettings = BuildSettingsWith(100);
            originalSettings.SSOClientID = "my-precious-sso-id";
            originalSettings.SSOClientSecret = "my-precious-sso-secret";

            await SettingsFileManager.SaveFromSerializableSettingsAsync(originalSettings);

            // === Phase 2: "Restart EVEMon" — load from JSON ===
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();

            loaded.Should().NotBeNull("settings should be loadable after save");
            loaded!.Characters.Should().HaveCount(100);
            loaded.ESIKeys.Should().HaveCount(100);
            loaded.Plans.Should().HaveCount(100);
            loaded.MonitoredCharacters.Should().HaveCount(100);
            loaded.SSOClientID.Should().Be("my-precious-sso-id");
            loaded.SSOClientSecret.Should().Be("my-precious-sso-secret");
            loaded.ForkId.Should().Be("aliacollins");
        }

        [Fact]
        public async Task Restart_RoundTrip_CharacterSkillsPreserved()
        {
            var original = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(original);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();

            // Verify each character still has 50 skills
            foreach (var character in loaded!.Characters)
            {
                character.Skills.Should().HaveCount(50,
                    $"character {character.ID} should retain all 50 skills after restart");
            }
        }

        [Fact]
        public async Task Restart_RoundTrip_PlanEntriesPreserved()
        {
            var original = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(original);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();

            loaded!.Plans.Should().HaveCount(100);
            foreach (var plan in loaded.Plans)
            {
                plan.Entries.Should().HaveCount(5,
                    $"plan '{plan.Name}' should retain 5 entries after restart");
            }
        }

        [Fact]
        public async Task Restart_RoundTrip_ESIKeyTokensPreserved()
        {
            var original = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(original);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();

            foreach (var key in loaded!.ESIKeys)
            {
                key.RefreshToken.Should().StartWith("rt-stress-",
                    $"ESI key for {key.ID} should preserve refresh token");
                key.Monitored.Should().BeTrue();
            }
        }

        [Fact]
        public async Task Restart_RoundTrip_SkillQueuePreserved()
        {
            var original = BuildSettingsWith(100);

            await SettingsFileManager.SaveFromSerializableSettingsAsync(original);

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();

            // Spot-check specific character
            long targetId = 2100000001L;
            var ccpChar = loaded!.Characters.OfType<SerializableCCPCharacter>()
                .FirstOrDefault(c => c.ID == targetId);
            ccpChar.Should().NotBeNull();
            ccpChar!.SkillQueue.Should().HaveCount(10);
            ccpChar.SkillQueue[0].ID.Should().Be(3300);
        }

        #endregion

        // =====================================================================
        // SCENARIO 6: EVEMon crashes mid-save
        // =====================================================================

        #region Crash Recovery — .bak Files Save the Day

        [Fact]
        public async Task Crash_SettingsJsonCorrupted_RecoveredFromBackup()
        {
            // Save twice so .bak exists from first save
            var settings1 = BuildSettingsWith(10);
            settings1.SSOClientID = "good-sso-id";
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings1);

            var settings2 = BuildSettingsWith(10);
            settings2.SSOClientID = "updated-sso-id";
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings2);

            // Simulate crash: corrupt settings.json mid-write
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "\0\0\0CORRUPT");

            // "Restart" — load should recover from .bak
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().Be("good-sso-id",
                "should recover from .bak which has the first save");
        }

        [Fact]
        public async Task Crash_SettingsJsonCorrupted_ESIKeysRecovered()
        {
            var settings1 = BuildSettingsWith(5);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings1);

            var settings2 = BuildSettingsWith(5);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings2);

            // Corrupt settings.json
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "BROKEN");

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.ESIKeys.Should().HaveCount(5,
                "should recover 5 ESI keys from backup");
        }

        [Fact]
        public async Task Crash_SettingsJsonCorrupted_AllCharactersRecovered()
        {
            var settings = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Save again so .bak exists
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Corrupt settings.json
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "CORRUPT!!!");

            // Should recover from .bak
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull("should recover from .bak");
            loaded!.Characters.Should().HaveCount(100);

            foreach (var character in loaded.Characters)
            {
                character.Skills.Should().HaveCount(50);
            }
        }

        [Fact]
        public async Task Crash_SettingsJsonCorrupted_PlansRecovered()
        {
            var settings = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Save again to create .bak
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Corrupt settings.json
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "{{{{");

            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Plans.Should().HaveCount(100,
                "plans should recover from .bak");
        }

        [Fact]
        public async Task Crash_SettingsJsonDeleted_RecoveredFromBackup()
        {
            // Simulate total crash where primary file is deleted but .bak survives
            var settings = BuildSettingsWith(50);
            settings.ForkId = "aliacollins";
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Save again to create .bak file
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Delete primary file
            File.Delete(SettingsFileManager.SettingsJsonFilePath);

            // Recovery from .bak — settings.json is gone so load checks .bak
            // Note: the atomic write creates .bak, which LoadToSerializableSettingsAsync checks
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(50);
            loaded.ESIKeys.Should().HaveCount(50);
        }

        [Fact]
        public async Task Crash_BothPrimaryAndBackupCorrupt_ReturnsDefaults()
        {
            // Write corrupt data to both primary and backup
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "NOT JSON");
            File.WriteAllText(SettingsFileManager.ConfigFilePath + ".bak", "ALSO NOT JSON");

            // Should gracefully return defaults, not throw
            var config = await SettingsFileManager.LoadConfigAsync();
            config.Should().NotBeNull();
            config.ForkId.Should().Be("aliacollins", "should return default config");
        }

        [Fact]
        public async Task Crash_NoBackupExists_PrimaryCorrupt_ReturnsDefaults()
        {
            // Corrupt the primary and delete any backup that might exist
            File.WriteAllText(SettingsFileManager.ConfigFilePath, "CORRUPT");
            string bakPath = SettingsFileManager.ConfigFilePath + ".bak";
            if (File.Exists(bakPath))
                File.Delete(bakPath);

            var config = await SettingsFileManager.LoadConfigAsync();
            config.Should().NotBeNull();
            config.Version.Should().Be(1); // default
        }

        #endregion

        // =====================================================================
        // SCENARIO 7: Crash during SmartSettingsManager save
        // =====================================================================

        #region ProcessExit Handler

        [Fact]
        public void ProcessExit_DirtyManager_FlushesViaExportFunc()
        {
            int exportCalls = 0;
            var settings = BuildSettingsWith(10);
            var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCalls);
                return settings;
            });

            manager.Save();
            manager.IsDirty.Should().BeTrue();

            // Simulate what happens on process exit: the ProcessExit handler fires
            // We can't actually fire ProcessExit, but we can verify the dispose path
            // which uses the same flush mechanism
            manager.Dispose();

            exportCalls.Should().BeGreaterThan(0);
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();
        }

        [Fact]
        public void ProcessExit_CleanManager_SkipsExport()
        {
            int exportCalls = 0;
            var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCalls);
                return BuildSettingsWith(10);
            });

            // Not dirty — ProcessExit/Dispose should skip
            manager.Dispose();

            exportCalls.Should().Be(0);
        }

        #endregion

        // =====================================================================
        // SCENARIO 8: Concurrent operations during save
        // =====================================================================

        #region Concurrent Save and Clear Race Conditions

        [Fact]
        public async Task ConcurrentSave_WriteLock_SerializesAccess()
        {
            var settings = BuildSettingsWith(100);
            using var manager = CreateManager(() => settings);

            // Fire 5 immediate saves concurrently
            var tasks = Enumerable.Range(0, 5)
                .Select(_ => manager.SaveImmediateAsync())
                .ToArray();

            await Task.WhenAll(tasks);

            manager.ActualWriteCount.Should().Be(5);
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task ClearAllJsonFiles_WithWriteLock_WaitsForSave()
        {
            var settings = BuildSettingsWith(50);
            using var manager = CreateManager(() => settings);

            // Save first
            await manager.SaveImmediateAsync();
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();

            // Clear with write lock — should not race with saves
            SettingsFileManager.ClearAllJsonFiles(manager.WriteLock);

            // After clear, files should be gone
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeFalse();

            // WriteLock should be released
            manager.WriteLock.CurrentCount.Should().Be(1);
        }

        [Fact]
        public void ConcurrentSaveCalls_100Threads_NoStateCorruption()
        {
            var settings = BuildSettingsWith(10);
            using var manager = CreateManager(() => settings);
            int threadCount = 100;
            var barrier = new ManualResetEventSlim(false);

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                threads[i] = new Thread(() =>
                {
                    barrier.Wait();
                    manager.Save();
                });
                threads[i].Start();
            }

            barrier.Set();
            foreach (var t in threads) t.Join();

            manager.SaveCallCount.Should().Be(threadCount);
            manager.IsDirty.Should().BeTrue();
        }

        #endregion

        // =====================================================================
        // SCENARIO 9: Timing and throughput behavior
        // =====================================================================

        #region Save Pipeline Timing

        [Fact]
        public async Task Save100Characters_CompletesInReasonableTime()
        {
            var settings = BuildSettingsWith(100);

            var sw = Stopwatch.StartNew();
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
            sw.Stop();

            // Single settings.json file
            // Should complete in under 30 seconds even on slow disks
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
                "saving 100 characters should not take longer than 30 seconds");

            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task Load100Characters_CompletesInReasonableTime()
        {
            var settings = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            var sw = Stopwatch.StartNew();
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            sw.Stop();

            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(100);

            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(30),
                "loading 100 characters should not take longer than 30 seconds");
        }

        [Fact]
        public async Task BackupRecovery_100Characters_CompletesInReasonableTime()
        {
            var settings = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // Corrupt the primary settings.json
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "CORRUPT");

            var sw = Stopwatch.StartNew();

            // Load should fall back to .bak
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            sw.Stop();

            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(100, "all 100 characters should recover from .bak");
            sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(60),
                "recovery from backup should complete in under 60 seconds");
        }

        #endregion

        // =====================================================================
        // SCENARIO 10: EventAggregator stress under 100 characters
        // =====================================================================

        #region Event Delivery at Scale

        [Fact]
        public void EventAggregator_100CharacterUpdates_AllDelivered()
        {
            var aggregator = new EventAggregator();
            int received = 0;
            aggregator.Subscribe<CharacterUpdatedEvent>(e => received++);

            for (int i = 0; i < 100; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i + 1, $"Pilot {i + 1}"));

            received.Should().Be(100);
        }

        [Fact]
        public void EventAggregator_100SettingsSaved_AllDelivered()
        {
            var aggregator = new EventAggregator();
            int received = 0;
            aggregator.Subscribe<SettingsSavedEvent>(e => received++);

            for (int i = 0; i < 100; i++)
                aggregator.Publish(new SettingsSavedEvent());

            received.Should().Be(100);
        }

        [Fact]
        public void EventAggregator_MultipleSubscribers_AllReceive100Events()
        {
            var aggregator = new EventAggregator();
            int sub1Count = 0, sub2Count = 0, sub3Count = 0;

            aggregator.Subscribe<CharacterUpdatedEvent>(e => sub1Count++);
            aggregator.Subscribe<CharacterUpdatedEvent>(e => sub2Count++);
            aggregator.Subscribe<CharacterUpdatedEvent>(e => sub3Count++);

            for (int i = 0; i < 100; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i + 1, $"Pilot {i + 1}"));

            sub1Count.Should().Be(100);
            sub2Count.Should().Be(100);
            sub3Count.Should().Be(100);
        }

        [Fact]
        public void EventAggregator_SubscriberDisposedMidStream_StopsReceiving()
        {
            var aggregator = new EventAggregator();
            int activeCount = 0;
            int disposedCount = 0;

            aggregator.Subscribe<CharacterUpdatedEvent>(e => activeCount++);
            var disposable = aggregator.Subscribe<CharacterUpdatedEvent>(e => disposedCount++);

            // First 50 events — both subscribers receive
            for (int i = 0; i < 50; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i + 1, $"Pilot {i + 1}"));

            activeCount.Should().Be(50);
            disposedCount.Should().Be(50);

            // Dispose the second subscriber
            disposable.Dispose();

            // Next 50 events — only first subscriber receives
            for (int i = 50; i < 100; i++)
                aggregator.Publish(new CharacterUpdatedEvent(i + 1, $"Pilot {i + 1}"));

            activeCount.Should().Be(100);
            disposedCount.Should().Be(50, "disposed subscriber should stop at 50");
        }

        #endregion

        // =====================================================================
        // SCENARIO 11: Full lifecycle simulation
        // =====================================================================

        #region Full Lifecycle: Add → Save → Crash → Recover → Verify

        [Fact]
        public async Task FullLifecycle_Add100_Save_CrashMidWrite_Recover_VerifyAll()
        {
            // === Step 1: User adds 100 characters ===
            var settings = BuildSettingsWith(100);
            settings.SSOClientID = "lifecycle-sso-id";
            settings.SSOClientSecret = "lifecycle-sso-secret";

            // === Step 2: First save (creates settings.json, no .bak yet) ===
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();

            // === Step 3: Second save (creates .bak via File.Replace) ===
            settings.Characters[0].Name = "Updated Pilot 1";
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // === Step 4: Simulate crash — corrupt settings.json ===
            File.WriteAllText(SettingsFileManager.SettingsJsonFilePath, "CRASH!");

            // === Step 5: "Restart" — load with recovery from .bak ===
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.SSOClientID.Should().NotBeNullOrEmpty("SSO credentials should recover");
            loaded.ESIKeys.Should().HaveCount(100);
            loaded.Characters.Should().HaveCount(100);

            // === Step 6: Verify all characters are accessible ===
            foreach (var character in loaded.Characters)
            {
                character.Skills.Should().HaveCount(50);
            }
        }

        [Fact]
        public async Task FullLifecycle_SmartSettingsManager_DirtyShutdown_ThenRestart()
        {
            // === Phase 1: "EVEMon running" — add characters, save gets coalesced ===
            var settings = BuildSettingsWith(100);

            var manager = CreateManager(() => settings);

            // Simulate 100 character-add events
            for (int i = 0; i < 100; i++)
                manager.Save();

            // Timer hasn't fired yet — no actual writes
            manager.ActualWriteCount.Should().Be(0);

            // === Phase 2: User closes EVEMon — Dispose flushes ===
            manager.Dispose();

            // Settings file should now exist
            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();

            // === Phase 3: "Restart" — load everything back ===
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().HaveCount(100);
            loaded.ESIKeys.Should().HaveCount(100);
            loaded.Plans.Should().HaveCount(100);

            // Verify data integrity
            for (int i = 0; i < 100; i++)
            {
                loaded.Characters[i].Skills.Should().HaveCount(50);
                loaded.ESIKeys[i].Monitored.Should().BeTrue();
            }
        }

        #endregion

        // =====================================================================
        // SCENARIO 12: Edge cases with large character sets
        // =====================================================================

        #region Edge Cases

        [Fact]
        public async Task SaveFewerCharacters_OverwritesPrevious()
        {
            // Save 100 characters
            var settings100 = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings100);

            var loaded100 = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded100!.Characters.Should().HaveCount(100);

            // Now save only 50 characters — overwrites settings.json
            var settings50 = BuildSettingsWith(50);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings50);

            var loaded50 = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded50!.Characters.Should().HaveCount(50,
                "save with fewer characters should overwrite the previous");
        }

        [Fact]
        public async Task EmptySettings_SaveAndLoad_NoCharacters()
        {
            var empty = new SerializableSettings();
            await SettingsFileManager.SaveFromSerializableSettingsAsync(empty);

            File.Exists(SettingsFileManager.SettingsJsonFilePath).Should().BeTrue();
            var loaded = await SettingsFileManager.LoadToSerializableSettingsAsync();
            loaded.Should().NotBeNull();
            loaded!.Characters.Should().BeEmpty();
        }

        [Fact]
        public async Task Save100_ThenSave100Again_BackupExists()
        {
            var settings = BuildSettingsWith(100);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);
            await SettingsFileManager.SaveFromSerializableSettingsAsync(settings);

            // .bak file should exist for settings.json
            File.Exists(SettingsFileManager.SettingsJsonFilePath + ".bak").Should().BeTrue(
                "settings.json.bak should exist after second save");
        }

        #endregion
    }
}
