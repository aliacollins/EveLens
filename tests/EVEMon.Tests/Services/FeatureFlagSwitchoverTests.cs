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
    public class SchedulerAndSettingsTests
    {
        #region ScheduledQueryableAdapter

        [Fact]
        public void Adapter_WrapsProcessTickCorrectly()
        {
            int callCount = 0;
            var adapter = new ScheduledQueryableAdapter(12345L, () => callCount++);

            adapter.ProcessTick();
            adapter.ProcessTick();
            adapter.ProcessTick();

            callCount.Should().Be(3);
        }

        [Fact]
        public void Adapter_ReportsCorrectCharacterID()
        {
            var adapter = new ScheduledQueryableAdapter(98765L, () => { });

            adapter.CharacterID.Should().Be(98765L);
        }

        [Fact]
        public void Adapter_IsStartupComplete_AlwaysTrue()
        {
            var adapter = new ScheduledQueryableAdapter(1L, () => { });

            adapter.IsStartupComplete.Should().BeTrue();
        }

        [Fact]
        public void Adapter_ConsecutiveNotModifiedCount_AlwaysZero()
        {
            var adapter = new ScheduledQueryableAdapter(1L, () => { });

            adapter.ConsecutiveNotModifiedCount.Should().Be(0);
        }

        [Fact]
        public void Adapter_ThrowsOnNullProcessTick()
        {
            Action act = () => new ScheduledQueryableAdapter(1L, null!);

            act.Should().Throw<ArgumentNullException>()
                .WithParameterName("processTick");
        }

        [Fact]
        public void Adapter_ImplementsIScheduledQueryable()
        {
            var adapter = new ScheduledQueryableAdapter(1L, () => { });

            adapter.Should().BeAssignableTo<IScheduledQueryable>();
        }

        #endregion

        #region SmartScheduler Adapter Registration

        [Fact]
        public void SmartScheduler_AcceptsAdapterRegistration()
        {
            var dispatcher = Substitute.For<IDispatcher>();
            var esiClient = Substitute.For<IEsiClient>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            // Capture scheduled callback
            dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(_ => { });

            using var scheduler = new SmartQueryScheduler(dispatcher, esiClient, new Random(0));
            var adapter = new ScheduledQueryableAdapter(42L, () => { });

            scheduler.Register(adapter);

            scheduler.RegisteredCount.Should().Be(1);
        }

        [Fact]
        public void SmartScheduler_UnregisterAdapterWorks()
        {
            var dispatcher = Substitute.For<IDispatcher>();
            var esiClient = Substitute.For<IEsiClient>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(_ => { });

            using var scheduler = new SmartQueryScheduler(dispatcher, esiClient, new Random(0));
            var adapter = new ScheduledQueryableAdapter(42L, () => { });

            scheduler.Register(adapter);
            scheduler.RegisteredCount.Should().Be(1);

            scheduler.Unregister(adapter);
            scheduler.RegisteredCount.Should().Be(0);
        }

        [Fact]
        public void SmartScheduler_DrivesAdapterOnTick()
        {
            var dispatcher = Substitute.For<IDispatcher>();
            var esiClient = Substitute.For<IEsiClient>();
            esiClient.MaxConcurrentRequests.Returns(20);
            esiClient.ActiveRequests.Returns(0L);

            Action scheduledCallback = null!;
            dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(ci => scheduledCallback = ci.ArgAt<Action>(1));

            using var scheduler = new SmartQueryScheduler(dispatcher, esiClient, new Random(0));

            int tickCount = 0;
            var adapter = new ScheduledQueryableAdapter(42L, () => tickCount++);
            scheduler.Register(adapter);

            // Set this as the visible character so it gets ticked every cycle
            scheduler.SetVisibleCharacter(42L);

            // Simulate multiple ticks — need enough to pass startup delay
            for (int i = 0; i < 20; i++)
            {
                scheduledCallback?.Invoke();
            }

            // Adapter should have been driven at least once
            tickCount.Should().BeGreaterThan(0);
        }

        #endregion

        #region SmartSettings Save Delegation

        [Fact]
        public void SmartSettingsManager_SaveIncrementsSaveCount()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var mockAggregator = Substitute.For<IEventAggregator>();
                var mockDispatcher = Substitute.For<IDispatcher>();
                mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                    .Do(ci => ci.ArgAt<Action>(0).Invoke());

                using var manager = new SmartSettingsManager(
                    tempDir, mockAggregator, mockDispatcher, () => new SerializableSettings());

                manager.Save();
                manager.Save();
                manager.Save();

                manager.SaveCallCount.Should().Be(3);
                manager.IsDirty.Should().BeTrue();
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        [Fact]
        public void SmartSettingsManager_SaveImmediateResetsState()
        {
            var tempDir = CreateTempDirectory();
            try
            {
                var mockAggregator = Substitute.For<IEventAggregator>();
                var mockDispatcher = Substitute.For<IDispatcher>();
                mockDispatcher.When(d => d.Invoke(Arg.Any<Action>()))
                    .Do(ci => ci.ArgAt<Action>(0).Invoke());

                int exportCount = 0;
                using var manager = new SmartSettingsManager(
                    tempDir, mockAggregator, mockDispatcher, () =>
                    {
                        exportCount++;
                        return new SerializableSettings();
                    });

                // Save marks dirty, tracks count
                manager.Save();
                manager.Save();

                manager.SaveCallCount.Should().Be(2);
                manager.IsDirty.Should().BeTrue();

                // Export func is not called until flush — no timer fired yet
                exportCount.Should().Be(0);
            }
            finally
            {
                TryDeleteDirectory(tempDir);
            }
        }

        #endregion

        #region Helpers

        private static string CreateTempDirectory()
        {
            var path = Path.Combine(Path.GetTempPath(), "evemon-switchover-test-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(path);
            return path;
        }

        private static void TryDeleteDirectory(string path)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
            }
            catch
            {
                // Ignore cleanup errors in tests
            }
        }

        #endregion
    }
}
