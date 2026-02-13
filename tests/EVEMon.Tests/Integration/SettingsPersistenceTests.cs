using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Integration
{
    public class SettingsPersistenceTests : IDisposable
    {
        private readonly IEventAggregator _mockAggregator;
        private readonly string _tempDir;

        public SettingsPersistenceTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
            _tempDir = Path.Combine(Path.GetTempPath(), "evemon-inttest-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        public void Dispose()
        {
            try
            {
                if (Directory.Exists(_tempDir))
                    Directory.Delete(_tempDir, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        // --- Test 1: Save called 100 times, only 1-2 actual writes via coalescing ---

        [Fact]
        public void Save_Called100Times_NoActualWritesUntilTimerOrFlush()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            for (int i = 0; i < 100; i++)
            {
                manager.Save(config: new { iteration = i });
            }

            // All 100 calls tracked
            manager.SaveCallCount.Should().Be(100);

            // No writes yet -- the timer hasn't fired
            manager.ActualWriteCount.Should().Be(0);
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public void Save_Called100Times_DisposeFlushesSingleWrite()
        {
            var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            for (int i = 0; i < 100; i++)
            {
                manager.Save(config: new { iteration = i });
            }

            manager.SaveCallCount.Should().Be(100);
            manager.ActualWriteCount.Should().Be(0);

            // Dispose flushes pending saves -- only 1 write for all 100 calls
            manager.Dispose();

            manager.ActualWriteCount.Should().Be(1,
                "save coalescing should batch 100 Save() calls into 1 actual write");
        }

        // --- Test 2: SaveImmediateAsync always writes immediately ---

        [Fact]
        public async Task SaveImmediateAsync_AlwaysWritesImmediately()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            // Call Save 50 times (no writes)
            for (int i = 0; i < 50; i++)
                manager.Save(config: new { value = i });

            manager.ActualWriteCount.Should().Be(0);

            // Now SaveImmediateAsync
            await manager.SaveImmediateAsync(config: new { value = "immediate" });

            manager.ActualWriteCount.Should().Be(1);
            manager.IsDirty.Should().BeFalse();
            File.Exists(manager.ConfigFilePath).Should().BeTrue();
        }

        [Fact]
        public async Task SaveImmediateAsync_MultipleCalls_EachWrites()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            await manager.SaveImmediateAsync(config: new { v = 1 });
            await manager.SaveImmediateAsync(config: new { v = 2 });
            await manager.SaveImmediateAsync(config: new { v = 3 });

            manager.ActualWriteCount.Should().Be(3);
        }

        // --- Test 3: Round-trip write/read preserves data ---

        [Fact]
        public async Task RoundTrip_WriteRead_PreservesData()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            var config = new { name = "Test Config", count = 42, enabled = true };
            await manager.SaveImmediateAsync(config: config);

            // Read the file back
            string json = await File.ReadAllTextAsync(manager.ConfigFilePath);

            json.Should().Contain("\"name\"");
            json.Should().Contain("Test Config");
            json.Should().Contain("\"count\"");
            json.Should().Contain("42");
            json.Should().Contain("\"enabled\"");
            json.Should().Contain("true");
        }

        [Fact]
        public async Task RoundTrip_ConfigAndCredentials_BothPersisted()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            var config = new { setting = "value" };
            var credentials = new { token = "secret123" };

            await manager.SaveImmediateAsync(config: config, credentials: credentials);

            File.Exists(manager.ConfigFilePath).Should().BeTrue();
            File.Exists(manager.CredentialsFilePath).Should().BeTrue();

            string configJson = await File.ReadAllTextAsync(manager.ConfigFilePath);
            string credJson = await File.ReadAllTextAsync(manager.CredentialsFilePath);

            configJson.Should().Contain("value");
            credJson.Should().Contain("secret123");
        }

        // --- Test 4: Fork migration detection with realistic content ---

        [Fact]
        public void ForkMigration_RealisticAliaCollinsSettings_NoMigration()
        {
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""5"" forkId=""aliacollins"" forkVersion=""5.1.2"">
  <esiKeys>
    <esikey accessMask=""123"" refreshToken=""mytoken"" characterID=""12345"" />
  </esiKeys>
  <plans>
    <plan name=""Mining Plan"" />
  </plans>
</Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeFalse();
            result.DetectedForkId.Should().Be("aliacollins");
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void ForkMigration_RealisticPeterhaneveSettings_MigrationDetected()
        {
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""4986"">
  <esiKeys>
    <esikey accessMask=""456"" refreshToken=""petertoken"" characterID=""67890"" />
    <esikey accessMask=""789"" refreshToken=""petertoken2"" characterID=""11111"" />
  </esiKeys>
  <accounts>
    <account id=""1"" />
  </accounts>
</Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeTrue();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(4986);
            result.HasEsiKeys.Should().BeTrue();
        }

        [Fact]
        public void ForkMigration_OtherForkNoEsiKeys_NeedsForkIdOnly()
        {
            string content = @"<?xml version=""1.0"" encoding=""utf-8""?>
<Settings revision=""2"" forkId=""someotherfork"">
  <esiKeys></esiKeys>
</Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
            result.HasEsiKeys.Should().BeFalse();
        }

        // --- Test 5: Concurrent saves from multiple threads don't corrupt data ---

        [Fact]
        public void ConcurrentSaves_100Threads_NoCorruption()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            var barrier = new ManualResetEventSlim(false);
            int threadCount = 100;

            var threads = new Thread[threadCount];
            for (int i = 0; i < threadCount; i++)
            {
                int captured = i;
                threads[i] = new Thread(() =>
                {
                    barrier.Wait();
                    manager.Save(config: new { iteration = captured });
                });
                threads[i].Start();
            }

            barrier.Set();

            for (int i = 0; i < threadCount; i++)
                threads[i].Join();

            manager.SaveCallCount.Should().Be(threadCount);
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public async Task ConcurrentSaveImmediateAsync_NoDataLoss()
        {
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            int taskCount = 10;

            var tasks = new List<Task>();
            for (int i = 0; i < taskCount; i++)
            {
                int captured = i;
                tasks.Add(Task.Run(async () =>
                {
                    await manager.SaveImmediateAsync(config: new { iteration = captured });
                }));
            }

            await Task.WhenAll(tasks);

            // Each call should write; the semaphore serializes them
            manager.ActualWriteCount.Should().Be(taskCount);
            File.Exists(manager.ConfigFilePath).Should().BeTrue();
        }

        // --- Test 6: Atomic write integrity ---

        [Fact]
        public async Task AtomicWrite_NoTempFileLeftBehind()
        {
            string filePath = Path.Combine(_tempDir, "atomic-test.json");

            await SmartSettingsManager.WriteFileAtomicAsync(filePath, "{\"test\": true}");

            File.Exists(filePath).Should().BeTrue();

            // Temp file should not remain
            string tempPath = Path.Combine(_tempDir, ".atomic-test.json.tmp");
            File.Exists(tempPath).Should().BeFalse();
        }

        // --- Test 7: Disposed manager rejects new saves ---

        [Fact]
        public async Task DisposedManager_SaveImmediateAsync_ThrowsObjectDisposed()
        {
            var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            manager.Dispose();

            Func<Task> act = () => manager.SaveImmediateAsync(config: new { x = 1 });
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }
    }
}
