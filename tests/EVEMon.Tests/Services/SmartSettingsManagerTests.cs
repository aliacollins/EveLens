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

namespace EVEMon.Tests.Services
{
    public class SmartSettingsManagerTests : IDisposable
    {
        private readonly IEventAggregator _mockAggregator;
        private readonly IDispatcher _mockDispatcher;
        private readonly string _tempDir;

        public SmartSettingsManagerTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
            _mockDispatcher = Substitute.For<IDispatcher>();
            // Make Invoke execute synchronously in tests
            _mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                .Do(ci => ci.ArgAt<Action>(0).Invoke());
            _tempDir = CreateTempDirectory();
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

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "evemon-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private SmartSettingsManager CreateManager(Func<SerializableSettings> exportFunc = null)
        {
            return new SmartSettingsManager(
                _tempDir,
                _mockAggregator,
                _mockDispatcher,
                exportFunc ?? (() => new SerializableSettings()));
        }

        #region Constructor Tests

        [Fact]
        public void Constructor_NullDataDirectory_ThrowsArgumentNullException()
        {
            Action act = () => new SmartSettingsManager(null, _mockAggregator, _mockDispatcher, () => new SerializableSettings());
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("dataDirectory");
        }

        [Fact]
        public void Constructor_NullEventAggregator_ThrowsArgumentNullException()
        {
            Action act = () => new SmartSettingsManager(_tempDir, null, _mockDispatcher, () => new SerializableSettings());
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("eventAggregator");
        }

        [Fact]
        public void Constructor_NullDispatcher_ThrowsArgumentNullException()
        {
            Action act = () => new SmartSettingsManager(_tempDir, _mockAggregator, null, () => new SerializableSettings());
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("dispatcher");
        }

        [Fact]
        public void Constructor_NullExportFunc_ThrowsArgumentNullException()
        {
            Action act = () => new SmartSettingsManager(_tempDir, _mockAggregator, _mockDispatcher, null);
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("exportFunc");
        }

        [Fact]
        public void Constructor_CreatesDataDirectoryIfNotExists()
        {
            string newDir = Path.Combine(_tempDir, "subdir-" + Guid.NewGuid().ToString("N"));
            Directory.Exists(newDir).Should().BeFalse();

            using var manager = new SmartSettingsManager(
                newDir, _mockAggregator, _mockDispatcher, () => new SerializableSettings());

            Directory.Exists(newDir).Should().BeTrue();
        }

        #endregion

        #region Save Coalescing Tests

        [Fact]
        public void Save_Called21Times_TracksAllCalls()
        {
            using var manager = CreateManager();

            for (int i = 0; i < 21; i++)
            {
                manager.Save();
            }

            manager.SaveCallCount.Should().Be(21);
            manager.ActualWriteCount.Should().Be(0);
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public void SaveCallCount_TracksInvocations()
        {
            using var manager = CreateManager();

            manager.Save();
            manager.Save();
            manager.Save();

            manager.SaveCallCount.Should().Be(3);
        }

        #endregion

        #region Fork Migration Detection Tests

        [Fact]
        public void DetectForkMigration_OurForkId_NoMigration()
        {
            string content = @"<Settings revision=""5"" forkId=""aliacollins"" forkVersion=""5.1.2"">";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeFalse();
            result.DetectedForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void DetectForkMigration_OtherForkWithEsiKeys_MigrationDetected()
        {
            string content = @"<Settings revision=""10"" forkId=""otherfork"">
                <esiKeys>
                    <esikey refreshToken=""sometoken123"" />
                </esiKeys>
            </Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeTrue();
            result.HasEsiKeys.Should().BeTrue();
            result.DetectedForkId.Should().Be("otherfork");
        }

        [Fact]
        public void DetectForkMigration_MissingForkId_HighRevisionWithEsiKeys_PeterhaneveUser()
        {
            string content = @"<Settings revision=""4986"">
                <esiKeys>
                    <esikey refreshToken=""petertoken456"" />
                </esiKeys>
            </Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeTrue();
            result.HasEsiKeys.Should().BeTrue();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(4986);
        }

        [Fact]
        public void DetectForkMigration_MissingForkId_LowRevision_OurExistingUser()
        {
            string content = @"<Settings revision=""5"">";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(5);
        }

        [Fact]
        public void DetectForkMigration_OtherForkWithoutEsiKeys_NoMigration_NeedsForkId()
        {
            string content = @"<Settings revision=""10"" forkId=""anotherfork"">
                <esiKeys></esiKeys>
            </Settings>";

            var result = SmartSettingsManager.DetectForkMigration(content);

            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
            result.HasEsiKeys.Should().BeFalse();
            result.DetectedForkId.Should().Be("anotherfork");
        }

        #endregion

        #region Atomic Write Tests

        [Fact]
        public async Task AtomicWrite_CreatesTargetFile_NoTempFileRemains()
        {
            string filePath = Path.Combine(_tempDir, "test-atomic.json");
            string content = @"{""key"": ""value""}";

            await SmartSettingsManager.WriteFileAtomicAsync(filePath, content);

            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);

            string tempPath = Path.Combine(_tempDir, ".test-atomic.json.tmp");
            File.Exists(tempPath).Should().BeFalse();
        }

        [Fact]
        public async Task AtomicWrite_OverwritesExistingFile()
        {
            string filePath = Path.Combine(_tempDir, "test-overwrite.json");
            await File.WriteAllTextAsync(filePath, "old content");

            await SmartSettingsManager.WriteFileAtomicAsync(filePath, "new content");

            File.ReadAllText(filePath).Should().Be("new content");
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public void Dispose_SaveAfterDispose_ThrowsObjectDisposedException()
        {
            var manager = CreateManager();
            manager.Dispose();

            Action act = () => manager.Save();
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void ConcurrentSaveCalls_DoNotCorruptState()
        {
            using var manager = CreateManager();
            int concurrentCalls = 100;
            var barrier = new ManualResetEventSlim(false);

            var threads = new Thread[concurrentCalls];
            for (int i = 0; i < concurrentCalls; i++)
            {
                threads[i] = new Thread(() =>
                {
                    barrier.Wait();
                    manager.Save();
                });
                threads[i].Start();
            }

            barrier.Set();

            for (int i = 0; i < concurrentCalls; i++)
            {
                threads[i].Join();
            }

            manager.SaveCallCount.Should().Be(concurrentCalls);
            manager.IsDirty.Should().BeTrue();
        }

        #endregion

        #region ParseRevisionNumber Tests

        [Fact]
        public void ParseRevisionNumber_ValidRevision_ReturnsNumber()
        {
            string content = @"<Settings revision=""4986"">";
            SmartSettingsManager.ParseRevisionNumber(content).Should().Be(4986);
        }

        [Fact]
        public void ParseRevisionNumber_MissingRevision_ReturnsNegativeOne()
        {
            string content = @"<Settings>";
            SmartSettingsManager.ParseRevisionNumber(content).Should().Be(-1);
        }

        [Fact]
        public void ParseRevisionNumber_RevisionZero_ReturnsZero()
        {
            string content = @"<Settings revision=""0"">";
            SmartSettingsManager.ParseRevisionNumber(content).Should().Be(0);
        }

        #endregion
    }
}
