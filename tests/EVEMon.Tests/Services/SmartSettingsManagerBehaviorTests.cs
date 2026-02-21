// EVEMon NexT — Character Intelligence for EVE Online
// Copyright © 2006-2021 EVEMon Development Team, © 2025-2026 Alia Collins
// Built with Claude Code (Anthropic)
// Licensed under GPL v2 — see LICENSE for details

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
    /// <summary>
    /// Behavior tests for SmartSettingsManager focusing on coalescing, immediate save,
    /// error handling during save, and dispose flushing.
    /// Complements SmartSettingsManagerTests which covers constructor validation,
    /// fork migration detection, atomic writes, and thread safety.
    /// </summary>
    [Collection("AppServices")]
    public class SmartSettingsManagerBehaviorTests : IDisposable
    {
        private readonly IEventAggregator _mockAggregator;
        private readonly IDispatcher _mockDispatcher;
        private readonly string _tempDir;

        public SmartSettingsManagerBehaviorTests()
        {
            _mockAggregator = Substitute.For<IEventAggregator>();
            _mockDispatcher = Substitute.For<IDispatcher>();

            // Make Invoke execute synchronously in tests
            _mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                .Do(ci => ci.ArgAt<Action>(0).Invoke());

            // Make Post execute synchronously in tests
            _mockDispatcher.When(d => d.Post(Arg.Any<Action>()))
                .Do(ci => ci.ArgAt<Action>(0).Invoke());

            _tempDir = CreateTempDirectory();

            // Set up AppServices to use our temp directory so SettingsFileManager
            // can resolve DataDirectory (used by SaveFromSerializableSettingsAsync)
            var mockPaths = Substitute.For<IApplicationPaths>();
            mockPaths.DataDirectory.Returns(_tempDir);
            AppServices.SetApplicationPaths(mockPaths);
            AppServices.SyncToServiceLocator();
        }

        public void Dispose()
        {
            AppServices.Reset();

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
            var path = Path.Combine(Path.GetTempPath(), "evemon-behavior-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private SmartSettingsManager CreateManager(Func<SerializableSettings>? exportFunc = null)
        {
            return new SmartSettingsManager(
                _tempDir,
                _mockAggregator,
                _mockDispatcher,
                exportFunc ?? (() => new SerializableSettings()));
        }

        #region RapidSaves_CoalescedToSingleWrite

        [Fact]
        public void RapidSaves_OnlyMarkDirtyWithoutWriting()
        {
            // Rapid calls to Save() should only set the dirty flag.
            // Actual writes happen on the timer callback, not synchronously.
            using var manager = CreateManager();

            for (int i = 0; i < 50; i++)
            {
                manager.Save();
            }

            // All 50 calls tracked
            manager.SaveCallCount.Should().Be(50);
            // But no actual writes performed (timer hasn't fired)
            manager.ActualWriteCount.Should().Be(0);
            // Dirty flag set
            manager.IsDirty.Should().BeTrue();
        }

        [Fact]
        public async Task RapidSaves_FollowedBySaveImmediate_ProducesOneWrite()
        {
            // After many rapid Save() calls, a single SaveImmediateAsync should
            // produce exactly one write and clear the dirty flag.
            using var manager = CreateManager();

            manager.Save();
            manager.Save();
            manager.Save();

            manager.IsDirty.Should().BeTrue();

            await manager.SaveImmediateAsync();

            // Dirty flag cleared by SaveImmediateAsync
            manager.IsDirty.Should().BeFalse();
            // Exactly one actual write
            manager.ActualWriteCount.Should().Be(1);
        }

        #endregion

        #region ForceSave_WritesImmediately

        [Fact]
        public async Task SaveImmediateAsync_BypassesCoalescing_WritesImmediately()
        {
            int exportCallCount = 0;
            using var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCallCount);
                return new SerializableSettings();
            });

            await manager.SaveImmediateAsync();

            manager.ActualWriteCount.Should().Be(1);
            exportCallCount.Should().Be(1);
        }

        [Fact]
        public async Task SaveImmediateAsync_ClearsDirtyFlag()
        {
            using var manager = CreateManager();

            manager.Save();
            manager.IsDirty.Should().BeTrue();

            await manager.SaveImmediateAsync();

            manager.IsDirty.Should().BeFalse();
        }

        [Fact]
        public async Task SaveImmediateAsync_PublishesSettingsSavedEvent()
        {
            using var manager = CreateManager();

            await manager.SaveImmediateAsync();

            _mockAggregator.Received(1).Publish(Arg.Any<SettingsSavedEvent>());
        }

        [Fact]
        public async Task SaveImmediateAsync_PostsToDispatcherForExport()
        {
            using var manager = CreateManager();

            await manager.SaveImmediateAsync();

            // Post is used for the export func marshaling (non-blocking)
            _mockDispatcher.Received(1).Post(Arg.Any<Action>());
        }

        [Fact]
        public async Task SaveImmediateAsync_AfterDispose_ThrowsObjectDisposedException()
        {
            var manager = CreateManager();
            manager.Dispose();

            Func<Task> act = () => manager.SaveImmediateAsync();
            await act.Should().ThrowAsync<ObjectDisposedException>();
        }

        #endregion

        #region ExceptionDuringSave_LoggedNotSwallowed

        [Fact]
        public async Task SaveImmediateAsync_NullExportResult_DoesNotWrite()
        {
            // When the export function returns null, the save pipeline should
            // skip the write step entirely.
            using var manager = CreateManager(() => null!);

            await manager.SaveImmediateAsync();

            // No actual write because settings was null
            manager.ActualWriteCount.Should().Be(0);
            _mockAggregator.DidNotReceive().Publish(Arg.Any<SettingsSavedEvent>());
        }

        [Fact]
        public async Task SaveImmediateAsync_ExportThrows_PropagatesException()
        {
            // When the export function throws, SaveImmediateAsync should propagate
            // the exception (it's not swallowed).
            using var manager = CreateManager(() =>
                throw new InvalidOperationException("Export failed"));

            Func<Task> act = () => manager.SaveImmediateAsync();
            await act.Should().ThrowAsync<InvalidOperationException>()
                .WithMessage("Export failed");
        }

        #endregion

        #region Dispose_FlushesRemainingWrites

        [Fact]
        public void Dispose_WhenDirty_AttemptsFlush()
        {
            int exportCallCount = 0;
            var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCallCount);
                return new SerializableSettings();
            });

            manager.Save();
            manager.IsDirty.Should().BeTrue();

            manager.Dispose();

            // Dispose uses Invoke (not Post) for the flush since it's synchronous
            _mockDispatcher.Received().Invoke(Arg.Any<Action>());
        }

        [Fact]
        public void Dispose_WhenNotDirty_DoesNotCallExport()
        {
            int exportCallCount = 0;
            var manager = CreateManager(() =>
            {
                Interlocked.Increment(ref exportCallCount);
                return new SerializableSettings();
            });

            // Not dirty - no Save() called
            manager.Dispose();

            // No export should be called during dispose if not dirty
            exportCallCount.Should().Be(0);
        }

        [Fact]
        public void Dispose_CalledTwice_DoesNotThrow()
        {
            var manager = CreateManager();

            manager.Dispose();

            Action act = () => manager.Dispose();
            act.Should().NotThrow();
        }

        [Fact]
        public void Dispose_WhenDirty_DispatcherInvokeFailure_DoesNotThrow()
        {
            // If the dispatcher is already shut down during Dispose, it should not throw
            var failingDispatcher = Substitute.For<IDispatcher>();
            failingDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                .Do(_ => throw new ObjectDisposedException("Dispatcher"));

            var manager = new SmartSettingsManager(
                _tempDir,
                _mockAggregator,
                failingDispatcher,
                () => new SerializableSettings());

            manager.Save();

            Action act = () => manager.Dispose();
            act.Should().NotThrow("Dispose must not throw even if dispatcher is shut down");
        }

        #endregion

        #region SaveCoalesceIntervalMs Constant

        [Fact]
        public void SaveCoalesceIntervalMs_IsTenSeconds()
        {
            SmartSettingsManager.SaveCoalesceIntervalMs.Should().Be(10_000);
        }

        #endregion
    }
}
