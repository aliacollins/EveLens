using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Services
{
    public class SmartQuerySchedulerTests : IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEsiClient _esiClient;
        private Action _scheduledCallback;
        private SmartQueryScheduler _scheduler;

        public SmartQuerySchedulerTests()
        {
            _dispatcher = Substitute.For<IDispatcher>();
            _esiClient = Substitute.For<IEsiClient>();
            _esiClient.MaxConcurrentRequests.Returns(20);
            _esiClient.ActiveRequests.Returns(0L);

            // Capture the scheduled callback so we can invoke it to simulate ticks
            _dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(ci => _scheduledCallback = ci.ArgAt<Action>(1));
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }

        private SmartQueryScheduler CreateScheduler()
        {
            var random = new Random(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);
            return _scheduler;
        }

        private SmartQueryScheduler CreateSchedulerWithZeroDelays()
        {
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);
            return _scheduler;
        }

        private IScheduledQueryable CreateQueryable(long characterId, int consecutiveNotModified = 0)
        {
            var queryable = Substitute.For<IScheduledQueryable>();
            queryable.CharacterID.Returns(characterId);
            queryable.IsStartupComplete.Returns(true);
            queryable.ConsecutiveNotModifiedCount.Returns(consecutiveNotModified);
            return queryable;
        }

        private void SimulateTick()
        {
            _scheduledCallback?.Invoke();
        }

        private void SimulateTicks(int count)
        {
            for (int i = 0; i < count; i++)
                SimulateTick();
        }

        private int GetProcessTickCount(IScheduledQueryable queryable)
        {
            return queryable.ReceivedCalls()
                .Count(c => c.GetMethodInfo().Name == "ProcessTick");
        }

        // --- Test 1: Registration ---

        [Fact]
        public void Register_70Queryables_RegisteredCountIs70()
        {
            var scheduler = CreateScheduler();

            for (int i = 0; i < 70; i++)
                scheduler.Register(CreateQueryable(1000 + i));

            scheduler.RegisteredCount.Should().Be(70);
        }

        // --- Test 2: Unregistration ---

        [Fact]
        public void Unregister_Queryable_DecreasesCount()
        {
            var scheduler = CreateScheduler();
            var q1 = CreateQueryable(1001);
            var q2 = CreateQueryable(1002);
            scheduler.Register(q1);
            scheduler.Register(q2);
            scheduler.RegisteredCount.Should().Be(2);

            scheduler.Unregister(q1);

            scheduler.RegisteredCount.Should().Be(1);
        }

        // --- Test 3: Staggered startup ---

        [Fact]
        public void StaggeredStartup_QueryableNotProcessedBeforeDelayElapsed()
        {
            // Use max random delay so the last queryable gets 69*75 + 250 = 5425ms
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(250);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);
            var scheduler = _scheduler;

            var queryables = new List<IScheduledQueryable>();
            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(2000 + i);
                queryables.Add(q);
                scheduler.Register(q);
            }

            // Set the last one as visible
            scheduler.SetVisibleCharacter(2069);

            // First tick: 5000ms elapsed, but queryable 69 needs 5425ms
            SimulateTick();
            queryables[69].DidNotReceive().ProcessTick();

            // Second tick: 10000ms elapsed, now 5425ms delay is cleared
            SimulateTick();
            queryables[69].Received(1).ProcessTick();
        }

        // --- Test 4: Adaptive polling doubles after 3 consecutive NotModified ---

        [Fact]
        public void AdaptivePolling_IntervalsDoubleAfter3ConsecutiveNotModified()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryable = CreateQueryable(3001, consecutiveNotModified: 3);
            scheduler.Register(queryable);
            scheduler.SetVisibleCharacter(3001);

            // Tick 1: multiplier=1, processes immediately. UpdateAdaptive: 3>=3, doubles to 2.
            // Countdown set to 1 (newMultiplier - 1).
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(1);

            // Tick 2: countdown=1, decrement to 0 -> skip.
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(1, "should skip due to adaptive doubling");

            // Tick 3: countdown=0, process. Multiplier 2->4, countdown set to 3.
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(2, "should process on 3rd tick (every 2nd)");
        }

        // --- Test 5: Adaptive polling caps at 4x base ---

        [Fact]
        public void AdaptivePolling_IntervalCapsAt4xBase()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryable = CreateQueryable(4001, consecutiveNotModified: 10);
            scheduler.Register(queryable);
            scheduler.SetVisibleCharacter(4001);

            // Run many ticks. With capping at 4x, it should process every 4th tick at worst.
            // Tick 1: process (mult 1->2, countdown=1)
            // Tick 2: skip (countdown 1->0)
            // Tick 3: process (mult 2->4, countdown=3)
            // Tick 4-6: skip (countdown 3->2->1->0)
            // Tick 7: process (mult 4, 4*2=8 capped to 4, no change, countdown=3)
            // Tick 8-10: skip
            // Tick 11: process
            // Pattern from tick 3: process every 4 ticks
            SimulateTicks(20);

            int callCount = GetProcessTickCount(queryable);
            // Expected: tick 1, 3, 7, 11, 15, 19 = 6 calls in 20 ticks
            callCount.Should().Be(6, "with 4x cap should process every 4th tick after ramp-up");
        }

        // --- Test 6: Adaptive polling resets when NotModified drops to 0 ---

        [Fact]
        public void AdaptivePolling_IntervalResetsWhenNotModifiedDropsToZero()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryable = CreateQueryable(5001, consecutiveNotModified: 3);
            scheduler.Register(queryable);
            scheduler.SetVisibleCharacter(5001);

            // Tick 1: process, multiplier doubles 1->2, countdown=1
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(1);

            // Change to NotModified=0 (fresh data returned)
            queryable.ConsecutiveNotModifiedCount.Returns(0);

            // Tick 2: skip (countdown=1 -> 0)
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(1);

            // Tick 3: process. UpdateAdaptive resets multiplier to 1, countdown to 0.
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(2);

            // Tick 4: process immediately (multiplier is back to 1)
            SimulateTick();
            GetProcessTickCount(queryable).Should().Be(3, "should process every tick after reset");
        }

        // --- Test 7: Visible character processed every tick ---

        [Fact]
        public void VisibleCharacter_ProcessedEveryTick()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryable = CreateQueryable(6001);
            scheduler.Register(queryable);
            scheduler.SetVisibleCharacter(6001);

            SimulateTicks(5);

            queryable.Received(5).ProcessTick();
        }

        // --- Test 8: Background characters processed round-robin ---

        [Fact]
        public void BackgroundCharacters_ProcessedRoundRobin()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var visible = CreateQueryable(7001);
            var bg1 = CreateQueryable(7002);
            var bg2 = CreateQueryable(7003);
            var bg3 = CreateQueryable(7004);

            scheduler.Register(visible);
            scheduler.Register(bg1);
            scheduler.Register(bg2);
            scheduler.Register(bg3);
            scheduler.SetVisibleCharacter(7001);

            // 3 ticks should cycle through all 3 background characters
            SimulateTicks(3);

            bg1.Received(1).ProcessTick();
            bg2.Received(1).ProcessTick();
            bg3.Received(1).ProcessTick();
            visible.Received(3).ProcessTick();
        }

        // --- Test 9: Rate limit pause pauses background ---

        [Fact]
        public void RateLimitPause_PausesBackgroundWhenAboveThreshold()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var visible = CreateQueryable(8001);
            var background = CreateQueryable(8002);
            scheduler.Register(visible);
            scheduler.Register(background);
            scheduler.SetVisibleCharacter(8001);

            // 17/20 = 0.85 > 0.8 threshold
            _esiClient.ActiveRequests.Returns(17L);

            SimulateTicks(3);

            background.DidNotReceive().ProcessTick();
            scheduler.IsRateLimitPaused.Should().BeTrue();
        }

        // --- Test 10: Rate limit pause still processes visible character ---

        [Fact]
        public void RateLimitPause_StillProcessesVisibleCharacter()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var visible = CreateQueryable(9001);
            var background = CreateQueryable(9002);
            scheduler.Register(visible);
            scheduler.Register(background);
            scheduler.SetVisibleCharacter(9001);

            _esiClient.ActiveRequests.Returns(17L);

            SimulateTicks(3);

            visible.Received(3).ProcessTick();
            background.DidNotReceive().ProcessTick();
        }

        // --- Test 11: 70-char registration performance ---

        [Fact]
        public void Register70Characters_NoPerformanceIssues()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryables = new List<IScheduledQueryable>();

            for (int i = 0; i < 70; i++)
            {
                var q = CreateQueryable(10000 + i);
                queryables.Add(q);
                scheduler.Register(q);
            }

            scheduler.SetVisibleCharacter(10000);

            SimulateTicks(70);

            // Visible character should be processed every tick
            queryables[0].Received(70).ProcessTick();

            // Each background character should be processed at least once
            for (int i = 1; i < 70; i++)
            {
                GetProcessTickCount(queryables[i]).Should().BeGreaterThan(0,
                    $"background character {10000 + i} should be processed at least once in 70 ticks");
            }

            scheduler.RegisteredCount.Should().Be(70);
        }

        // --- Test 12: Dispose clears and stops ---

        [Fact]
        public void Dispose_ClearsRegistrationsAndStopsProcessing()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var queryable = CreateQueryable(11001);
            scheduler.Register(queryable);
            scheduler.SetVisibleCharacter(11001);

            SimulateTick();
            queryable.Received(1).ProcessTick();

            scheduler.Dispose();

            scheduler.RegisteredCount.Should().Be(0);

            queryable.ClearReceivedCalls();
            SimulateTick();
            queryable.DidNotReceive().ProcessTick();
        }

        // --- Test 13: Constructor schedules first tick ---

        [Fact]
        public void Constructor_SchedulesFirstTick()
        {
            var scheduler = CreateScheduler();

            _dispatcher.Received(1).Schedule(
                Arg.Is<TimeSpan>(t => t.TotalMilliseconds == 5000),
                Arg.Any<Action>());
        }

        // --- Test 14: ApiCallsInWindow tracks calls ---

        [Fact]
        public void ApiCallsInWindow_TracksProcessTickCalls()
        {
            var scheduler = CreateSchedulerWithZeroDelays();
            var q1 = CreateQueryable(12001);
            var q2 = CreateQueryable(12002);
            scheduler.Register(q1);
            scheduler.Register(q2);
            scheduler.SetVisibleCharacter(12001);

            SimulateTick();

            // One visible + one background
            scheduler.ApiCallsInWindow.Should().Be(2);
        }
    }
}
