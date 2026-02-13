using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using EVEMon.Common.Serialization.Settings;
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
        private readonly IDispatcher _mockDispatcher;
        private readonly string _tempDir;

        public SettingsPersistenceTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
            _mockDispatcher = Substitute.For<IDispatcher>();
            // Make Invoke execute synchronously in tests
            _mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                .Do(ci => ci.ArgAt<Action>(0).Invoke());
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

        private SmartSettingsManager CreateManager(Func<SerializableSettings> exportFunc = null)
        {
            return new SmartSettingsManager(
                _tempDir,
                _mockAggregator,
                _mockDispatcher,
                exportFunc ?? (() => new SerializableSettings()));
        }

        // --- Test 1: Save called 100 times, only tracks calls without writing ---

        [Fact]
        public void Save_Called100Times_NoActualWritesUntilTimerOrFlush()
        {
            using var manager = CreateManager();

            for (int i = 0; i < 100; i++)
            {
                manager.Save();
            }

            manager.SaveCallCount.Should().Be(100);
            manager.ActualWriteCount.Should().Be(0);
            manager.IsDirty.Should().BeTrue();
        }

        // --- Test 2: Export function is called during save ---

        [Fact]
        public void Save_ExportFuncCalledViaDispatcher_OnFlush()
        {
            int exportCallCount = 0;
            using var manager = CreateManager(() =>
            {
                exportCallCount++;
                return new SerializableSettings();
            });

            manager.Save();
            manager.Save();
            manager.Save();

            // Export not called yet (coalesced, timer hasn't fired)
            exportCallCount.Should().Be(0);
            manager.SaveCallCount.Should().Be(3);
        }

        // --- Test 3: Fork migration detection with realistic content ---

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

        // --- Test 4: Concurrent saves from multiple threads don't corrupt data ---

        [Fact]
        public void ConcurrentSaves_100Threads_NoCorruption()
        {
            using var manager = CreateManager();
            var barrier = new ManualResetEventSlim(false);
            int threadCount = 100;

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

            for (int i = 0; i < threadCount; i++)
                threads[i].Join();

            manager.SaveCallCount.Should().Be(threadCount);
            manager.IsDirty.Should().BeTrue();
        }

        // --- Test 5: Atomic write integrity ---

        [Fact]
        public async Task AtomicWrite_NoTempFileLeftBehind()
        {
            string filePath = Path.Combine(_tempDir, "atomic-test.json");

            await SmartSettingsManager.WriteFileAtomicAsync(filePath, "{\"test\": true}");

            File.Exists(filePath).Should().BeTrue();

            string tempPath = Path.Combine(_tempDir, ".atomic-test.json.tmp");
            File.Exists(tempPath).Should().BeFalse();
        }

        // --- Test 6: Disposed manager rejects new saves ---

        [Fact]
        public void DisposedManager_Save_ThrowsObjectDisposed()
        {
            var manager = CreateManager();
            manager.Dispose();

            Action act = () => manager.Save();
            act.Should().Throw<ObjectDisposedException>();
        }

        [Fact]
        public async Task DisposedManager_SaveImmediateAsync_ThrowsObjectDisposed()
        {
            var manager = CreateManager();
            manager.Dispose();

            Func<Task> act = () => manager.SaveImmediateAsync();
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }
    }
}
