using System;
using System.Collections.Generic;
using System.Linq;
using EVEMon.Common.Services;
using EVEMon.Core.Interfaces;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace EVEMon.Tests.Integration
{
    public class RateLimitSimulationTests : IDisposable
    {
        private readonly IDispatcher _dispatcher;
        private readonly IEsiClient _esiClient;
        private Action _scheduledCallback;
        private SmartQueryScheduler _scheduler;

        public RateLimitSimulationTests()
        {
            _dispatcher = Substitute.For<IDispatcher>();
            _esiClient = Substitute.For<IEsiClient>();
            _esiClient.MaxConcurrentRequests.Returns(20);
            _esiClient.ActiveRequests.Returns(0L);

            _dispatcher.When(d => d.Schedule(Arg.Any<TimeSpan>(), Arg.Any<Action>()))
                .Do(ci => _scheduledCallback = ci.ArgAt<Action>(1));
        }

        public void Dispose()
        {
            _scheduler?.Dispose();
        }

        private SmartQueryScheduler CreateSchedulerWithZeroDelays()
        {
            var random = Substitute.For<Random>();
            random.Next(Arg.Any<int>()).Returns(0);
            _scheduler = new SmartQueryScheduler(_dispatcher, _esiClient, random);
            return _scheduler;
        }

        private static IScheduledQueryable CreateQueryable(long characterId, int consecutiveNotModified = 0)
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

        private static int GetProcessTickCount(IScheduledQueryable queryable)
        {
            return queryable.ReceivedCalls()
                .Count(c => c.GetMethodInfo().Name == "ProcessTick");
        }

        // --- Test 1: Background pauses when ActiveRequests > 80% of max ---

        [Fact]
        public void RateLimit_70Chars_BackgroundPausesWhenActiveRequestsHigh()
        {
            var scheduler = CreateSchedulerWithZeroDelays();

            var visible = CreateQueryable(1);
            scheduler.Register(visible);
            scheduler.SetVisibleCharacter(1);

            var backgroundChars = new List<IScheduledQueryable>();
            for (int i = 2; i <= 70; i++)
            {
                var q = CreateQueryable(i);
                scheduler.Register(q);
                backgroundChars.Add(q);
            }

            // Set active requests to 17/20 = 85% > 80% threshold
            _esiClient.ActiveRequests.Returns(17L);

            SimulateTicks(10);

            // Visible character still processed every tick
            GetProcessTickCount(visible).Should().Be(10);

            // No background character should be processed
            foreach (var bg in backgroundChars)
            {
                GetProcessTickCount(bg).Should().Be(0,
                    "background characters should be paused during rate limiting");
            }

            scheduler.IsRateLimitPaused.Should().BeTrue();
        }

        // --- Test 2: Adaptive polling reduces call frequency after repeated 304s ---

        [Fact]
        public void AdaptivePolling_ReducesCallFrequencyAfterRepeated304s()
        {
            var scheduler = CreateSchedulerWithZeroDelays();

            // Use 4 chars (1 visible + 3 background) so round-robin wraps quickly
            // All return 3 consecutive 304s (NotModified)
            var visible = CreateQueryable(1, consecutiveNotModified: 3);
            var bg1 = CreateQueryable(2, consecutiveNotModified: 3);
            var bg2 = CreateQueryable(3, consecutiveNotModified: 3);
            var bg3 = CreateQueryable(4, consecutiveNotModified: 3);

            scheduler.Register(visible);
            scheduler.Register(bg1);
            scheduler.Register(bg2);
            scheduler.Register(bg3);
            scheduler.SetVisibleCharacter(1);

            // Run 20 ticks
            SimulateTicks(20);

            // Visible character with adaptive polling:
            // Tick 1: process (mult 1->2, countdown=1)
            // Tick 2: skip (countdown 1->0)
            // Tick 3: process (mult 2->4, countdown=3)
            // Tick 4-6: skip
            // Tick 7: process (4 capped, countdown=3)
            // Tick 8-10: skip
            // Tick 11: process, 15, 19 = 6 calls in 20 ticks
            int visibleCalls = GetProcessTickCount(visible);
            visibleCalls.Should().Be(6, "visible char should back off to every 4th tick");

            // Without adaptation, each background char would get ~20/3 = ~6-7 calls.
            // With adaptation, after first process each gets mult 2, then 4.
            // Total background calls should be significantly less than 20.
            int totalBgCalls = GetProcessTickCount(bg1) + GetProcessTickCount(bg2) + GetProcessTickCount(bg3);

            // Without adaptation: 1 bg per tick * 20 ticks = 20 bg calls.
            // With adaptation: after first round-robin cycle (3 ticks), each has mult=2.
            // Subsequent attempts may skip due to countdown.
            totalBgCalls.Should().BeLessThan(20,
                "adaptive polling should reduce total background API calls");
        }

        // --- Test 3: Visible character still processed during rate limit pause ---

        [Fact]
        public void RateLimit_VisibleCharacterStillProcessedDuringPause()
        {
            var scheduler = CreateSchedulerWithZeroDelays();

            var visible = CreateQueryable(1);
            var background = CreateQueryable(2);
            scheduler.Register(visible);
            scheduler.Register(background);
            scheduler.SetVisibleCharacter(1);

            // Above rate limit threshold
            _esiClient.ActiveRequests.Returns(17L);

            SimulateTicks(5);

            GetProcessTickCount(visible).Should().Be(5,
                "visible character must still be processed during rate limit pause");
            GetProcessTickCount(background).Should().Be(0,
                "background should be paused during rate limit");
        }

        // --- Test 4: Rate limit pause lifts when ActiveRequests drops ---

        [Fact]
        public void RateLimit_PauseLiftsWhenActiveRequestsDropsBelowThreshold()
        {
            var scheduler = CreateSchedulerWithZeroDelays();

            var visible = CreateQueryable(1);
            var background = CreateQueryable(2);
            scheduler.Register(visible);
            scheduler.Register(background);
            scheduler.SetVisibleCharacter(1);

            // Start above threshold
            _esiClient.ActiveRequests.Returns(17L);
            SimulateTicks(3);

            GetProcessTickCount(background).Should().Be(0,
                "background paused while above threshold");

            // Drop below threshold: 15/20 = 75% < 80%
            _esiClient.ActiveRequests.Returns(15L);
            scheduler.IsRateLimitPaused.Should().BeFalse();

            SimulateTicks(3);

            GetProcessTickCount(background).Should().BeGreaterThan(0,
                "background should resume after rate limit lifts");
        }

        // --- Test 5: Rate limit boundary - exactly at threshold ---

        [Fact]
        public void RateLimit_ExactlyAtThreshold_IsPaused()
        {
            var scheduler = CreateSchedulerWithZeroDelays();

            // 16/20 = 0.80 -- the check is > 0.8, so exactly 80% is NOT paused
            _esiClient.ActiveRequests.Returns(16L);
            scheduler.IsRateLimitPaused.Should().BeFalse("0.80 is not > 0.8");

            // 17/20 = 0.85 > 0.8 -- paused
            _esiClient.ActiveRequests.Returns(17L);
            scheduler.IsRateLimitPaused.Should().BeTrue("0.85 > 0.8");
        }

        // --- Test 6: Rate limit with zero max concurrent ---

        [Fact]
        public void RateLimit_ZeroMaxConcurrent_NeverPauses()
        {
            _esiClient.MaxConcurrentRequests.Returns(0);
            var scheduler = CreateSchedulerWithZeroDelays();

            _esiClient.ActiveRequests.Returns(100L);

            scheduler.IsRateLimitPaused.Should().BeFalse(
                "should not pause when MaxConcurrentRequests is 0");
        }
    }
}
