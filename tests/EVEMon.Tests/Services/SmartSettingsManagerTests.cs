using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly string _tempDir;

        public SmartSettingsManagerTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
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

        #region Constructor Tests

        [Fact]
        public void Constructor_NullDataDirectory_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new SmartSettingsManager(null, _mockAggregator);
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("dataDirectory");
        }

        [Fact]
        public void Constructor_NullEventAggregator_ThrowsArgumentNullException()
        {
            // Act & Assert
            Action act = () => new SmartSettingsManager(_tempDir, null);
            act.Should().Throw<ArgumentNullException>()
                .And.ParamName.Should().Be("eventAggregator");
        }

        [Fact]
        public void Constructor_CreatesDataDirectoryIfNotExists()
        {
            // Arrange
            string newDir = Path.Combine(_tempDir, "subdir-" + Guid.NewGuid().ToString("N"));
            Directory.Exists(newDir).Should().BeFalse();

            // Act
            using var manager = new SmartSettingsManager(newDir, _mockAggregator);

            // Assert
            Directory.Exists(newDir).Should().BeTrue();
        }

        #endregion

        #region Save Coalescing Tests

        [Fact]
        public void Save_Called21Times_TracksAllCalls()
        {
            // Arrange
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            var config = new { setting = "value" };

            // Act - call Save() 21 times like the original 21-saves-per-cycle problem
            for (int i = 0; i < 21; i++)
            {
                manager.Save(config: config);
            }

            // Assert - all 21 calls are tracked
            manager.SaveCallCount.Should().Be(21);
            // No actual write yet because the timer hasn't fired
            manager.ActualWriteCount.Should().Be(0);
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public async Task SaveImmediateAsync_BypassesCoalescing()
        {
            // Arrange
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            var config = new { setting = "immediate" };

            // Act
            await manager.SaveImmediateAsync(config: config);

            // Assert - write happens immediately
            manager.ActualWriteCount.Should().Be(1);
            manager.IsDirty.Should().BeFalse();
            File.Exists(manager.ConfigFilePath).Should().BeTrue();
        }

        [Fact]
        public void SaveCallCount_TracksInvocations()
        {
            // Arrange
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);

            // Act
            manager.Save(config: new { a = 1 });
            manager.Save(config: new { a = 2 });
            manager.Save(config: new { a = 3 });

            // Assert
            manager.SaveCallCount.Should().Be(3);
        }

        #endregion

        #region Fork Migration Detection Tests

        [Fact]
        public void DetectForkMigration_OurForkId_NoMigration()
        {
            // Arrange
            string content = @"<Settings revision=""5"" forkId=""aliacollins"" forkVersion=""5.1.2"">";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeFalse();
            result.DetectedForkId.Should().Be("aliacollins");
        }

        [Fact]
        public void DetectForkMigration_OtherForkWithEsiKeys_MigrationDetected()
        {
            // Arrange
            string content = @"<Settings revision=""10"" forkId=""otherfork"">
                <esiKeys>
                    <esikey refreshToken=""sometoken123"" />
                </esiKeys>
            </Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue();
            result.HasEsiKeys.Should().BeTrue();
            result.DetectedForkId.Should().Be("otherfork");
        }

        [Fact]
        public void DetectForkMigration_MissingForkId_HighRevisionWithEsiKeys_PeterhaneveUser()
        {
            // Arrange - revision 4986 is typical of peterhaneve's fork
            string content = @"<Settings revision=""4986"">
                <esiKeys>
                    <esikey refreshToken=""petertoken456"" />
                </esiKeys>
            </Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeTrue();
            result.HasEsiKeys.Should().BeTrue();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(4986);
        }

        [Fact]
        public void DetectForkMigration_MissingForkId_LowRevision_OurExistingUser()
        {
            // Arrange - revision 5 is typical of our fork (pre-forkId era)
            string content = @"<Settings revision=""5"">";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
            result.MigrationDetected.Should().BeFalse();
            result.NeedsForkIdAdded.Should().BeTrue();
            result.DetectedForkId.Should().BeNull();
            result.DetectedRevision.Should().Be(5);
        }

        [Fact]
        public void DetectForkMigration_OtherForkWithoutEsiKeys_NoMigration_NeedsForkId()
        {
            // Arrange - different fork but no ESI keys to clear
            string content = @"<Settings revision=""10"" forkId=""anotherfork"">
                <esiKeys></esiKeys>
            </Settings>";

            // Act
            var result = SmartSettingsManager.DetectForkMigration(content);

            // Assert
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
            // Arrange
            string filePath = Path.Combine(_tempDir, "test-atomic.json");
            string content = @"{""key"": ""value""}";

            // Act
            await SmartSettingsManager.WriteFileAtomicAsync(filePath, content);

            // Assert
            File.Exists(filePath).Should().BeTrue();
            File.ReadAllText(filePath).Should().Be(content);

            // Temp file should not remain
            string tempPath = Path.Combine(_tempDir, ".test-atomic.json.tmp");
            File.Exists(tempPath).Should().BeFalse();
        }

        [Fact]
        public async Task AtomicWrite_OverwritesExistingFile()
        {
            // Arrange
            string filePath = Path.Combine(_tempDir, "test-overwrite.json");
            await File.WriteAllTextAsync(filePath, "old content");

            // Act
            await SmartSettingsManager.WriteFileAtomicAsync(filePath, "new content");

            // Assert
            File.ReadAllText(filePath).Should().Be("new content");
        }

        #endregion

        #region Dispose Tests

        [Fact]
        public async Task Dispose_FlushesPendingSaves()
        {
            // Arrange
            var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            var config = new { setting = "pending" };
            manager.Save(config: config);

            manager.IsDirty.Should().BeTrue();
            manager.ActualWriteCount.Should().Be(0);

            // Act
            manager.Dispose();

            // Assert - pending save was flushed
            manager.ActualWriteCount.Should().Be(1);
            File.Exists(manager.ConfigFilePath).Should().BeTrue();
        }

        [Fact]
        public void Dispose_SaveAfterDispose_ThrowsObjectDisposedException()
        {
            // Arrange
            var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            manager.Dispose();

            // Act & Assert
            Action act = () => manager.Save(config: new { a = 1 });
            act.Should().Throw<ObjectDisposedException>();
        }

        #endregion

        #region Thread Safety Tests

        [Fact]
        public void ConcurrentSaveCalls_DoNotCorruptState()
        {
            // Arrange
            using var manager = new SmartSettingsManager(_tempDir, _mockAggregator);
            int concurrentCalls = 100;
            var barrier = new ManualResetEventSlim(false);

            // Act - fire many Save() calls from different threads
            var threads = new Thread[concurrentCalls];
            for (int i = 0; i < concurrentCalls; i++)
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

            for (int i = 0; i < concurrentCalls; i++)
            {
                threads[i].Join();
            }

            // Assert - all calls were tracked, no corruption
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
